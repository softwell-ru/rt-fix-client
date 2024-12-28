using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendMassQuoteWithoutParty : QuotationScenarioBase
{
    private readonly IOptions<List<OperationOptions>> _options;

    public SendMassQuoteWithoutParty(
        ScenarioSettings settings,
        IOptions<List<OperationOptions>> options,
        ILoggerFactory loggerFactory) : base(settings, options, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    protected override OperationOptions ScenarioOptions => _options.Value.Where(x => x.Name == nameof(SendMassQuoteWithoutParty)).FirstOrDefault() ?? throw new ArgumentNullException();

    public override string Name => nameof(SendMassQuoteWithoutParty);

    public override string? Description => $"Отправить MassQuote без кода режима торгов";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var request = Helpers.MassQuoteRequest(ScenarioOptions.QuotationSecurityId, null);

        context.Client.SendMessage(request);

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.IsOfType<MassQuoteAcknowledgement>(MsgType.MASSQUOTEACKNOWLEDGEMENT, out var massQuote))
            {
                if (massQuote.QuoteID.getValue() == request.QuoteID.getValue() && massQuote.QuoteStatus.getValue() == QuoteStatus.ACCEPTED)
                {
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