namespace SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

public class ScenarioContext
{
    public ScenarioContext(FixClient client)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public FixClient Client { get; }
}