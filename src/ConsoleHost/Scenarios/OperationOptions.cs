namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class OperationOptions
{
    public string Name { get; set; } = null!;

    public string? PartyId { get; set; }

    public string[]? PartyIds { get; set; }

    public string QuotationSecurityId { get; set; } = null!;

    public string? CurveCode { get; set; }

    public string SecurityId { get; set; } = null!;

    public string[] SecurityIds { get; set; } = null!;

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);
}