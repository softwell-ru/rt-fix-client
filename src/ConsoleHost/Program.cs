using Microsoft.Extensions.Configuration;
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
    .ConfigureHostConfiguration(
        builder => builder
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile("appsettings.local.json", true))
    .ConfigureServices((host, services) =>
    {
        services.AddLogging(
            lb => lb.AddSimpleConsole(
                opts =>
                {
                    opts.IncludeScopes = true;
                    opts.TimestampFormat = "HH:mm:ss ";
                })
            .SetMinimumLevel(LogLevel.Debug));

        services.AddOptions<SendQuotationsBatchRequestReceiveRefreshedIndefinitelyOptions>()
            .Bind(host.Configuration.GetSection(nameof(SendQuotationsBatchRequestReceiveRefreshedIndefinitely)));

        services.AddSingleton(scenarioSettings);
        AddScenario<SendQuotationsRequestReceiveSnapshots>(services);
        AddScenario<SendQuotationsRequestReceiveRefreshed>(services);
        AddScenario<ReceiveDeal>(services);
        AddScenario<SendDealsRequestAndReceiveDeals>(services);
        AddScenario<SendQuotation>(services);
        AddScenario<CancelQuotation>(services);
        AddScenario<ReceiveChats>(services);
        AddScenario<SendChatsRequestAndReceiveChats>(services);
        AddScenario<SendSecListRequestAndReceiveSecList>(services);
        AddScenario<SendSecListRequestAndReceiveSecDefinition>(services);
        AddScenario<SendQuotationsBatchRequestReceiveRefreshedIndefinitely>(services);
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