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

    public override string Name => nameof(SendQuotationsRequestReceiveSnapshots);

    public override string? Description => "Отправить запрос на котировки, получить текущие значения";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var count = 0;
        var totalCount = 0;

        var request = Helpers.CreateQuotationRequest(new[] { "USD/RUB" }, null);
        context.Client.SendMessage(request);

        Logger.LogInformation("Отправили запрос на котировки, ожидаем текущее значение котировок..");

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.Message.IsOfType<MarketDataSnapshotFullRefresh>(MsgType.MARKET_DATA_SNAPSHOT_FULL_REFRESH, out var mdfr))
            {
                if (count == 0)
                {
                    totalCount = mdfr.TotNumReports.getValue();
                }
                count++;

                LogMarketDataSnapshotFullRefresh(mdfr);

                if (count == totalCount)
                {
                    Logger.LogInformation("Все котировки получены: {count}", count);
                    return;
                }

                Logger.LogInformation("Осталось получить котировок: {count}/{totalCount}", totalCount - count, totalCount);
            }
            else if (msg.Message.IsOfType<MarketDataRequestReject>(MsgType.MARKET_DATA_REQUEST_REJECT, out var mdrr))
            {
                throw new Exception("Запрос на получение котировок был отклонен с причиной " + mdrr.MDReqRejReason.getValue());
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