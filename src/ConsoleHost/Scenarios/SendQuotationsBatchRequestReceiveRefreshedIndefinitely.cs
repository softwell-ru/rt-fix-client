using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendQuotationsBatchRequestReceiveRefreshedIndefinitely : QuotationScenarioBase
{
    private readonly SendQuotationsBatchRequestReceiveRefreshedIndefinitelyOptions _options;

    private long _totalRefreshMessagesReceived = 0;

    private long _totalRefreshPricesReceived = 0;

    public SendQuotationsBatchRequestReceiveRefreshedIndefinitely(
        ScenarioSettings settings,
        IOptions<SendQuotationsBatchRequestReceiveRefreshedIndefinitelyOptions> options,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Value is null) throw new ArgumentException("Scenario options should be present in configuration");
        if (options.Value.SecurityIds?.Any() != true) throw new ArgumentException("SecurityIds should be present in scenario options");

        _options = options.Value;
    }

    public override string Name => nameof(SendQuotationsBatchRequestReceiveRefreshedIndefinitely);

    public override string? Description => "Отправить запрос на батч котировов, замерять скорость обновлений";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var request = Helpers.CreateQuotationRequest(_options.SecurityIds, _options.PartyIds);
        context.Client.SendMessage(request);

        Logger.LogInformation("Отправили запрос на котировки");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Task? t = null;

        try
        {
            await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
            {
                if (msg.IsOfType<MarketDataIncrementalRefresh>(MsgType.MARKET_DATA_INCREMENTAL_REFRESH, out var mdir))
                {
                    Interlocked.Increment(ref _totalRefreshMessagesReceived);
                    Interlocked.Add(ref _totalRefreshPricesReceived, mdir.NoMDEntries.getValue());

                    t ??= LogRefreshMessagesRateAsync(cts.Token);
                }
                else if (msg.IsOfType<MarketDataRequestReject>(MsgType.MARKET_DATA_REQUEST_REJECT, out var mdrr))
                {
                    var reason = mdrr.MDReqRejReason.getValue();
                    if (reason == MDReqRejReason.UNKNOWN_SYMBOL)
                    {
                        Logger.LogWarning("На запрос на получение котировок сервер ответил следующими предупреждениями: {warnings}", mdrr.Text.getValue());
                    }
                    else
                    {
                        throw new Exception($"Запрос на получение котировок был отклонен с причиной {reason}: {mdrr.Text.getValue()}");
                    }
                }
                else if (
                    msg.IsOfType<BusinessMessageReject>(MsgType.BUSINESS_MESSAGE_REJECT, out var bmr)
                    && bmr.RefMsgType.getValue() == MsgType.MARKET_DATA_REQUEST
                    && bmr.IsSetBusinessRejectRefID()
                    && bmr.BusinessRejectRefID.getValue() == request.MDReqID.getValue())
                {
                    throw new Exception($"Запрос отклонен с причиной {bmr.BusinessRejectReason.getValue()}: {bmr.Text.getValue()}");
                }
            }
        }
        finally
        {
            cts.Cancel();
            if (t != null)
            {
                await t;
            }
        }
    }

    private async Task LogRefreshMessagesRateAsync(CancellationToken ct)
    {
        await Task.Yield();
        var sw = Stopwatch.StartNew();

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(_options.Interval, ct);

            var secs = sw.Elapsed.TotalSeconds;

            // мало ли, кто-то еще сидит на 32-разрядной ОС..
            var messagesCount = Interlocked.Read(ref _totalRefreshMessagesReceived);
            var pricessCount = Interlocked.Read(ref _totalRefreshPricesReceived);

            Logger.LogInformation(
                @"{time}: 
    Скорость получения сообщений: {messages}/сек
    Скорость получения цен:       {prices}/сек",
                DateTime.Now,
                messagesCount / secs,
                pricessCount / secs);
        }
    }
}