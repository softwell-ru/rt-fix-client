using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class CancelMassQuote : QuotationScenarioBase
{
    private readonly OperationOptions _options;

    public CancelMassQuote(
        ScenarioSettings settings,
        ConfigManager configManager,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(configManager);

        _options = configManager.GetOperationSettings("CommonSettings")
            ?? throw new InvalidOperationException("CommonSettings settings are not configured.");

        if (_options.QuotationSecurityId is null) throw new ArgumentNullException("QuotationSecurityId should be present in configuration");
    }

    public override string Name => nameof(CancelMassQuote);

    public override string? Description => $"Отменить MassQuote котировку, дождаться сообщения об отмене";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var message = Helpers.MassQuoteRequest(_options.QuotationSecurityId, _options.PartyId);

        var cancelBand = Helpers.CreateQuoteCancel(_options.QuotationSecurityId);
        //Указываем, что отменяем торгуемые цены, чтобы отменять банды.
        cancelBand.QuoteType = new QuoteType(QuoteType.TRADEABLE);
        cancelBand.AddGroup(new QuoteCancel.NoPartyIDsGroup
        {
            PartyID = new PartyID(_options.PartyId)
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