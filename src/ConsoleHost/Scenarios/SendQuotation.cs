using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendQuotation : QuotationScenarioBase
{
    private readonly IOptions<List<OperationOptions>> _options;

    public SendQuotation(
        ScenarioSettings settings,
        IOptions<List<OperationOptions>> options,
        ILoggerFactory loggerFactory) : base(settings, options, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    protected override OperationOptions ScenarioOptions => _options.Value.Where(x => x.Name == nameof(SendQuotation)).FirstOrDefault() ?? throw new ArgumentNullException();

    public override string Name => nameof(SendQuotation);

    public override string? Description => $"Отправить котировку, дождаться сообщения с измененными ценами";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        SubscribeToRefreshes(context);
        await WaitForSnapshotFullRefreshAsync(context, ct);

        const decimal bid = 61.35m;
        const decimal offer = 64.37m;
        var request = Helpers.CreateQuote(ScenarioOptions.QuotationSecurityId, bid, offer);

        context.Client.SendMessage(request);

        Logger.LogInformation("Отправили котировку, ожидаем обновления..");

        var isBidSet = false;
        var isOfferSet = false;

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.IsOfType<MarketDataIncrementalRefresh>(MsgType.MARKET_DATA_INCREMENTAL_REFRESH, out var mdr))
            {
                foreach (var g in EnumerateMDEntriesGroups(mdr))
                {
                    if (g.MDUpdateAction.getValue() == MDUpdateAction.NEW
                            && g.SecurityID.getValue() == ScenarioOptions.QuotationSecurityId)
                    {
                        var type = g.MDEntryType.getValue();

                        if (type == MDEntryType.BID && g.MDEntryPx.getValue() == bid)
                        {
                            isBidSet = true;
                        }
                        if (type == MDEntryType.OFFER && g.MDEntryPx.getValue() == offer)
                        {
                            isOfferSet = true;
                        }
                    }
                }
            }
            else if (msg.IsOfType<BusinessMessageReject>(MsgType.BUSINESS_MESSAGE_REJECT, out var bmr)
                && bmr.RefMsgType.getValue() == MsgType.QUOTE
                && bmr.IsSetBusinessRejectRefID()
                && bmr.BusinessRejectRefID.getValue() == request.QuoteID.getValue())
            {
                throw new Exception($"Котировка отклонена с причиной {bmr.BusinessRejectReason.getValue()}: {bmr.Text.getValue()}");
            }

            if (isBidSet && isOfferSet)
            {
                Logger.LogInformation("Получили обновление котировки с нужными ценами");
                return;
            }
        }
    }
}