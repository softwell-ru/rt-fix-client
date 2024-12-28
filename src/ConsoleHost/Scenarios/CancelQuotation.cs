using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class CancelQuotation : QuotationScenarioBase
{
    public CancelQuotation(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    public override string Name => nameof(CancelQuotation);

    public override string? Description => $"Отменить котировку, дождаться сообщения об отмене";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        SubscribeToRefreshes(context);
        await WaitForSnapshotFullRefreshAsync(context, ct);

        var valuesRequest = Helpers.CreateQuote(QuotationSecurityId, 62.37m, 63.74m);
        context.Client.SendMessage(valuesRequest);

        Logger.LogInformation("Отправили котировку с какими-то значениями, чтобы было, что удалять");

        var request = Helpers.CreateQuoteCancel(QuotationSecurityId);
        context.Client.SendMessage(request);

        Logger.LogInformation("Отправили отмену котировки, ожидаем сообщение об отмене..");

        var isBidDeleted = false;
        var isOfferDeleted = false;

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

            if (isBidDeleted && isOfferDeleted)
            {
                Logger.LogInformation("Получили отмену котировки");
                return;
            }
        }
    }
}