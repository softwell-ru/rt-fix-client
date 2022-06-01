using Microsoft.Extensions.Logging;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendQuotationsRequestReceiveSnapshots : ScenarioBase
{
    public SendQuotationsRequestReceiveSnapshots(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    protected override string Name => nameof(SendQuotationsRequestReceiveSnapshots);

    protected override string? Description => "Отправить запрос на котировки, получить текущие значения";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        await WaitForLogonAsync(context, ct);

        var count = 0;
        var totalCount = 0;

        var request = Helpers.CreateQuotationRequest(new[] { "USD/RUB" }, null);
        context.Client.SendMessage(request);

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.Message.Header.GetString(Tags.MsgType) == MsgType.MARKET_DATA_SNAPSHOT_FULL_REFRESH)
            {
                var m = (MarketDataSnapshotFullRefresh)msg.Message;

                if (count == 0)
                {
                    totalCount = m.TotNumReports.getValue();
                }
                count++;

                LogMarketDataSnapshotFullRefresh(m);

                if (count == totalCount)
                {
                    Logger.LogInformation("Все котировки получены: {count}", count);
                    return;
                }

                Logger.LogInformation("Осталось получить котировок: {count}/{totalCount}", totalCount - count, totalCount);
            }
            else if (msg.Message.Header.GetString(Tags.MsgType) == MsgType.MARKET_DATA_REQUEST_REJECT)
            {
                var m = (MarketDataRequestReject)msg.Message;
                throw new Exception("Запрос на получение котировок был отклонен с причиной " + m.MDReqRejReason.getValue());
            }
        }
    }

    private void LogMarketDataSnapshotFullRefresh(MarketDataSnapshotFullRefresh mds)
    {
        var prices = new List<string>();

        for (var i = 1; i <= mds.NoMDEntries.getValue(); i++)
        {
            var g = new MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
            mds.GetGroup(i, g);

            var pg = new MarketDataSnapshotFullRefresh.NoMDEntriesGroup.NoPartyIDsGroup();

            g.GetGroup(1, pg);

            prices.Add($"{g.MDEntryType.getValue()}: {g.MDEntryPx.getValue()}, PartyId={pg.PartyID.getValue()}");
        }

        Logger.LogInformation(@"Получили котировку по инструменту {securityId}: 
{prices}",
            mds.SecurityID.getValue(),
            prices);
    }
}