using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class CancelMassQuote : QuotationScenarioBase
{
    private readonly IOptions<List<OperationOptions>> _options;

    public CancelMassQuote(
        ScenarioSettings settings,
        IOptions<List<OperationOptions>> options,
        ILoggerFactory loggerFactory) : base(settings, options, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    protected override OperationOptions ScenarioOptions => _options.Value.Where(x => x.Name == nameof(CancelMassQuote)).FirstOrDefault() ?? throw new ArgumentNullException();

    public override string Name => nameof(CancelMassQuote);

    public override string? Description => $"Отменить MassQuote котировку, дождаться сообщения об отмене";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var message = Helpers.MassQuoteRequest(ScenarioOptions.QuotationSecurityId, ScenarioOptions.PartyId);

        var cancelBand = Helpers.CreateQuoteCancel(ScenarioOptions.QuotationSecurityId);
        //Указываем, что отменяем торгуемые цены, чтобы отменять банды.
        cancelBand.QuoteType = new QuoteType(QuoteType.TRADEABLE);
        cancelBand.AddGroup(new QuoteCancel.NoPartyIDsGroup
        {
            PartyID = new PartyID(ScenarioOptions.PartyId)
        });

        context.Client.SendMessage(message);

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.IsOfType<MassQuoteAcknowledgement>(MsgType.MASSQUOTEACKNOWLEDGEMENT, out var massQuote))
            {
                if (massQuote.QuoteID.getValue() == message.QuoteID.getValue() && massQuote.QuoteStatus.getValue() == QuoteStatus.ACCEPTED)
                {
                    Logger.LogInformation("Отправили банд для того, чтобы его отменить");
                    context.Client.SendMessage(cancelBand);

                }
                if (massQuote.QuoteID.getValue() == cancelBand.QuoteMsgID.getValue() && massQuote.QuoteStatus.getValue() == QuoteStatus.CANCELED)
                {
                    Logger.LogInformation("Банд отменился успешно");
                    return;
                }
            }
            else if (msg.IsOfType<BusinessMessageReject>(MsgType.BUSINESS_MESSAGE_REJECT, out var reject))
            {
                throw new Exception($"Something went wrong {reject.Text.getValue()}");
            }
        }
    }
}