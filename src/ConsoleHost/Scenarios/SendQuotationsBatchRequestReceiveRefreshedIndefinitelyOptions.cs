namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendQuotationsBatchRequestReceiveRefreshedIndefinitelyOptions
{
    public string[] SecurityIds { get; set; } = null!;

    public string[]? PartyIds { get; set; }

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);
}
