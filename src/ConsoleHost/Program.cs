using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickFix;
using SoftWell.RtFix.ConsoleHost.Scenarios;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

var existsLocal = File.Exists("SessionSettings.local.cfg");

var settingsPath = existsLocal ? "SessionSettings.local.cfg" : "SessionSettings.cfg";

var scenarioSettings = new ScenarioSettings(
    new SessionSettings(settingsPath));

var builder = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddLogging(o => o.AddConsole());
        services.AddSingleton(scenarioSettings);
        // services.AddSingleton<IScenario, SendQuotationsRequestReceiveSnapshots>();
        // services.AddSingleton<IScenario, SendQuotationsRequestReceiveRefreshed>();
        // services.AddSingleton<IScenario, ReceiveDeal>();
        services.AddSingleton<IScenario, SendDealsRequestAndReceiveDeal>();

    });

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, __) =>
{
    logger.LogInformation("Приложение останавливается..");
    cts.Cancel();
};

try
{
    logger.LogInformation("Приложение запущено");

    var scenarios = host.Services.GetRequiredService<IEnumerable<IScenario>>().ToList();

    logger.LogInformation("Количество тестовых сценариев: {count}", scenarios.Count);

    var errorCount = 0;

    foreach (var s in scenarios)
    {
        try
        {
            await s.RunAsync(cts.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            errorCount++;
        }
    }

    logger.LogInformation("Успешно выполнено сценариев: {successCount}/{totalCount}", scenarios.Count - errorCount, scenarios.Count);

    if (errorCount > 0) return 1;
}
finally
{
    logger.LogInformation("Приложение остановлено");
}

return 0;