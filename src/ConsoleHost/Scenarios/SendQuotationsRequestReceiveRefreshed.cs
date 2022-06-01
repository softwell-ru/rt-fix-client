using Microsoft.Extensions.Logging;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendQuotationsRequestReceiveRefreshed : ScenarioBase
{
    public SendQuotationsRequestReceiveRefreshed(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    protected override string Name => nameof(SendQuotationsRequestReceiveRefreshed);

    protected override string? Description => "Отправить запрос на котировки, получить обновление";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        await WaitForLogonAsync(context, ct);

        var request = Helpers.CreateQuotationRequest(new[] { "001500002000TSLA" }, null);
        context.Client.SendMessage(request);

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.Message.Header.GetString(Tags.MsgType) == MsgType.MARKET_DATA_INCREMENTAL_REFRESH)
            {
                var m = (MarketDataIncrementalRefresh)msg.Message;

                LogMarketDataIncrementalRefresh(m);

                return;
            }
            else if (msg.Message.Header.GetString(Tags.MsgType) == MsgType.MARKET_DATA_REQUEST_REJECT)
            {
                var m = (MarketDataRequestReject)msg.Message;
                throw new Exception("Запрос на получение котировок был отклонен с причиной " + m.MDReqRejReason.getValue());
            }
        }
    }

    private void LogMarketDataIncrementalRefresh(MarketDataIncrementalRefresh mdr)
    {
        var msgs = new List<string>();

        for (var i = 1; i < mdr.NoMDEntries.getValue(); i++)
        {
            var g = new MarketDataIncrementalRefresh.NoMDEntriesGroup();
            mdr.GetGroup(i, g);

            var pg = new MarketDataIncrementalRefresh.NoMDEntriesGroup.NoPartyIDsGroup();

            g.GetGroup(1, pg);

            msgs.Add($"инструмент {g.SecurityID.getValue()} {g.MDEntryType.getValue}: {g.MDEntryPx.getValue()}, PartyId={pg.PartyID.getValue()}");
        }

        Logger.LogInformation(@"Получили обновление котировок: 
{prices}",
            msgs);
    }
}