using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendQuotationsRequestReceiveSnapshots : QuotationScenarioBase
{
    private readonly OperationOptions _options;

    public SendQuotationsRequestReceiveSnapshots(
        ScenarioSettings settings,
        ConfigManager configManager,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(configManager);

        _options = configManager.GetOperationSettings("CommonSettings")
            ?? throw new InvalidOperationException("CommonSettings settings are not configured.");

        if (_options.SecurityId is null) throw new ArgumentException("SecurityId should be present in configuration");
    }

    public override string Name => nameof(SendQuotationsRequestReceiveSnapshots);

    public override string? Description => "Отправить запрос на котировки, получить текущие значения";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var count = 0;
        var totalCount = 0;

        var request = Helpers.CreateQuotationRequest(new[] { _options.SecurityId }, null);
        context.Client.SendMessage(request);

        Logger.LogInformation("Отправили запрос на котировки, ожидаем текущее значение котировок..");

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.IsOfType<MarketDataSnapshotFullRefresh>(MsgType.MARKET_DATA_SNAPSHOT_FULL_REFRESH, out var mdfr))
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

        var unitOfMeasure = mds.IsSetUnitOfMeasure() ? mds.UnitOfMeasure.getValue() : null;

        Logger.LogInformation(@"Получили котировку по инструменту {securityId}: 
{prices}, unit of measure: {unitOfMeasure}",
            mds.SecurityID.getValue(),
            prices,
            unitOfMeasure);
    }
}