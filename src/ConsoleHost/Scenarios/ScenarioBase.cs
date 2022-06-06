using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public abstract class ScenarioBase : IScenario
{
    private readonly ILoggerFactory _loggerFactory;

    protected ScenarioBase(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        Logger = _loggerFactory.CreateLogger(GetType());
    }

    public ScenarioSettings Settings { get; }

    public abstract string Name { get; }

    public virtual string? Description { get; }

    protected ILogger Logger { get; }

    public async Task RunAsync(CancellationToken ct = default)
    {
        using var _ = Logger.BeginScope("Сценарий {scenarioName}", Name);
        var sw = Stopwatch.StartNew();

        if (Description is not null)
        {
            Logger.LogInformation("{scenarioDescription}", Description);
        }

        try
        {
            await using var client = new FixClient(Settings.SessionSettings, _loggerFactory.CreateLogger<FixClient>());

            var storeFactory = new FileStoreFactory(Settings.SessionSettings);
            var logFactory = new FileLogFactory(Settings.SessionSettings);

            using var initiator = new QuickFix.Transport.SocketInitiator(client, storeFactory, Settings.SessionSettings, logFactory);
            using var ctr = ct.Register(() => initiator.Stop());

            var context = new ScenarioContext(client);

            initiator.Start();

            await WaitForLogonAsync(context, ct);

            Logger.LogInformation("Подключились к серверу");

            await RunAsyncInner(context, ct);

            context.Client.Logout();

            await WaitForLogoutAsync(context, ct);

            initiator.Stop();
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested) return;

            Logger.LogError(ex, "Ошибка во время выполнения сценария");
            throw;
        }
        finally
        {
            sw.Stop();
            Logger.LogInformation("Сценарий выполнялся {time}", sw.Elapsed);
        }
    }

    protected abstract Task RunAsyncInner(ScenarioContext context, CancellationToken ct);

    protected static Task WaitForLogonAsync(ScenarioContext context, CancellationToken ct)
    {
        return WaitForMessageAsync(context, MsgType.LOGON, ct);
    }

    protected static Task WaitForLogoutAsync(ScenarioContext context, CancellationToken ct)
    {
        return WaitForMessageAsync(context, MsgType.LOGOUT, ct);
    }

    protected static async Task<MessageWrapper> WaitForMessageAsync(
        ScenarioContext context,
        string msgType,
        CancellationToken ct)
    {
        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.Message.IsOfType(msgType))
            {
                return msg;
            }
        }

        ct.ThrowIfCancellationRequested();

        throw new TimeoutException();
    }
}