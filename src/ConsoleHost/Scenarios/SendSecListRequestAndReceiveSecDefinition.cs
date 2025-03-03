using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendSecListRequestAndReceiveSecDefinition : ScenarioBase
{
    private readonly IOptions<List<OperationOptions>> _options;

    public SendSecListRequestAndReceiveSecDefinition(
        ScenarioSettings settings,
        IOptions<List<OperationOptions>> options,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    protected OperationOptions ScenarioOptions => _options.Value.Where(x => x.Name == nameof(SendSecListRequestAndReceiveSecDefinition)).FirstOrDefault() ?? throw new ArgumentNullException();

    public override string Name => nameof(SendSecListRequestAndReceiveSecDefinition);

    public override string? Description => "Отправить запрос на данные по инструменту, получить подтверждение запроса, получить данные";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var request = Helpers.CreateSecListSymbolRequest(ScenarioOptions.SecurityId);

        context.Client.SendMessage(request);

        Logger.LogInformation("Отправили запрос на получение данных по инструменту, ожидаем результат..");

        var count = 0;

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.IsOfType<SecurityList>(MsgType.SECURITY_LIST, out var sl))
            {
                if (sl.SecurityReqID.getValue() != request.SecurityReqID.getValue()) continue;

                count++;
                LogSecurityResponse(sl);
            }
        }
    }

    private void LogSecurityResponse(SecurityList sl)
    {
        var group = new SecurityList.NoRelatedSymGroup();
        var field = new Symbol();
        sl.GetGroup(1, group);
        Logger.LogInformation(@"Получили сообщение об инструменте: {securityId}",
            group.GetField(field).getValue());
    }
}