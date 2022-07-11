using Microsoft.Extensions.Logging;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendQuotation : ScenarioBase
{
    private readonly TimeSpan _waitErrorTimeout = TimeSpan.FromSeconds(10);

    public SendQuotation(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    public override string Name => nameof(SendQuotation);

    public override string? Description => $"Отправить котировку, за {_waitErrorTimeout} не дождаться сообщения об ошибке";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var request = Helpers.CreateQuote("FX-USD-RUB-TOM", 61.5m, 62.5m);

        context.Client.SendMessage(request);

        Logger.LogInformation("Отправили котировку, ожидаем, что за {timeout} не получим ошибку", _waitErrorTimeout);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_waitErrorTimeout);

            await foreach (var msg in context.Client.ReadAllMessagesAsync(cts.Token))
            {
                if (msg.IsOfType<BusinessMessageReject>(MsgType.BUSINESS_MESSAGE_REJECT, out var bmr)
                    && bmr.RefMsgType.getValue() == MsgType.QUOTE
                    && bmr.IsSetBusinessRejectRefID()
                    && bmr.BusinessRejectRefID.getValue() == request.QuoteID.getValue())
                {
                    throw new Exception($"Котировка отклонена с причиной {bmr.BusinessRejectReason.getValue()}: {bmr.Text.getValue()}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("За {timeout} не получили ошибку", _waitErrorTimeout);
        }
    }
}