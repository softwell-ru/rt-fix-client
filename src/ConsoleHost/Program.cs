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

void AddScenario<TScenario>(IServiceCollection services) where TScenario : class, IScenario
{
    if (args.Length > 0 && !args.Contains(typeof(TScenario).Name, StringComparer.OrdinalIgnoreCase)) return;

    services.AddSingleton<IScenario, TScenario>();
}

var builder = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddLogging(
            o => o.AddConsole()
                .SetMinimumLevel(LogLevel.Debug));

        services.AddSingleton(scenarioSettings);
        AddScenario<SendQuotationsRequestReceiveSnapshots>(services);
        AddScenario<SendQuotationsRequestReceiveRefreshed>(services);
        AddScenario<ReceiveDeal>(services);
        AddScenario<SendDealsRequestAndReceiveDeals>(services);
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

    logger.LogInformation("Тестовые сценарии: {scenarioNames}", string.Join(", ", scenarios.Select(x => x.Name)));

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

    if (!cts.IsCancellationRequested)
    {
        logger.LogInformation("Успешно выполнено сценариев: {successCount}/{totalCount}", scenarios.Count - errorCount, scenarios.Count);
    }

    if (errorCount > 0) return 1;
}
finally
{
    logger.LogInformation("Приложение остановлено");
}

return 0;