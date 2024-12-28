using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class CancelQuotationForCurvePoints : QuotationScenarioBase
{
    private static string _curveCode = "SOFTWELL-RUB-ADMIN-380";

    public CancelQuotationForCurvePoints(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    public override string Name => nameof(CancelQuotationForCurvePoints);

    protected override string QuotationSecurityId => "RUB1WD=";

    public override string? Description => $"Отменить котировку, дождаться сообщения об отмене";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        SubscribeToRefreshes(context);
        await WaitForSnapshotFullRefreshAsync(context, ct);

        var valuesRequest = Helpers.CreateQuote(QuotationSecurityId, 7.37m, 8.74m);

        valuesRequest.AddGroup(new MarketDataRequest.NoPartyIDsGroup
        {
            PartyID = new PartyID(_curveCode)
        });

        context.Client.SendMessage(valuesRequest);

        Logger.LogInformation("Отправили котировку с какими-то значениями, чтобы было, что удалять");

        var request = Helpers.CreateQuoteCancel(QuotationSecurityId);

        request.AddGroup(new MarketDataRequest.NoPartyIDsGroup
        {
            PartyID = new PartyID(_curveCode)
        });
        context.Client.SendMessage(request);

        Logger.LogInformation("Отправили отмену котировки, ожидаем сообщение об отмене..");

        var isBidDeleted = false;
        var isOfferDeleted = false;
        var isOurPartyId = false;

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.IsOfType<MarketDataIncrementalRefresh>(MsgType.MARKET_DATA_INCREMENTAL_REFRESH, out var mdir))
            {
                foreach (var g in EnumerateMDEntriesGroups(mdir))
                {
                    if (g.MDUpdateAction.getValue() == MDUpdateAction.DELETE
                        && g.SecurityID.getValue() == QuotationSecurityId)
                    {
                        var type = g.MDEntryType.getValue();

                        if (type == MDEntryType.BID)
                        {
                            isBidDeleted = true;
                        }
                        if (type == MDEntryType.OFFER)
                        {
                            isOfferDeleted = true;
                        }

                        var pg = new MarketDataSnapshotFullRefresh.NoMDEntriesGroup.NoPartyIDsGroup();
                        g.GetGroup(1, pg);

                        if (pg.PartyID.getValue() == _curveCode)
                        {
                            isOurPartyId = true;
                        }
                    }
                }
            }
            else if (msg.IsOfType<BusinessMessageReject>(MsgType.BUSINESS_MESSAGE_REJECT, out var bmr)
                && bmr.RefMsgType.getValue() == MsgType.QUOTE_CANCEL
                && bmr.IsSetBusinessRejectRefID()
                && bmr.BusinessRejectRefID.getValue() == request.QuoteMsgID.getValue())
            {
                throw new Exception($"Отмена котировки отклонена с причиной {bmr.BusinessRejectReason.getValue()}: {bmr.Text.getValue()}");
            }

            if (isBidDeleted && isOfferDeleted && isOurPartyId)
            {
                Logger.LogInformation("Получили отмену котировки по кривой");
                return;
            }
        }
    }
}