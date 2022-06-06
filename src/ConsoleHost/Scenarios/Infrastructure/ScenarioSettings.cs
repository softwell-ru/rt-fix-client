using QuickFix;

namespace SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

public class ScenarioSettings
{
    public ScenarioSettings(SessionSettings sessionSettings)
    {
        SessionSettings = sessionSettings ?? throw new ArgumentNullException(nameof(sessionSettings));
    }

    public SessionSettings SessionSettings { get; }
}