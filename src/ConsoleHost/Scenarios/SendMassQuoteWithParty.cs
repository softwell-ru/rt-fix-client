using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendMassQuoteWithParty : QuotationScenarioBase
{
    private readonly OperationOptions _options;

    public SendMassQuoteWithParty(
        ScenarioSettings settings,
        ConfigManager configManager,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(configManager);

        _options = configManager.GetOperationSettings("CommonSettings")
                   ?? throw new InvalidOperationException("Common settings are not configured.");

        if (_options.QuotationSecurityId is null) throw new ArgumentException("QuotationSecurityId should be present in configuration");
    }

    public override string Name => nameof(SendMassQuoteWithParty);

    public override string? Description => $"Отправить MassQuote с режимом торгов";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var request = Helpers.MassQuoteRequest(_options.QuotationSecurityId, _options.PartyId);

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