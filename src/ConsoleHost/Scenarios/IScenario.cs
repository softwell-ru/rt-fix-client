namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public interface IScenario
{
    Task RunAsync(CancellationToken ct = default);
}