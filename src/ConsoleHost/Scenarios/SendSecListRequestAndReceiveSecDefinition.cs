using Microsoft.Extensions.Logging;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendSecListRequestAndReceiveSecDefinition : ScenarioBase
{
    private readonly string _securityId = "FX-USD-RUB-TOM";

    public SendSecListRequestAndReceiveSecDefinition(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    public override string Name => nameof(SendSecListRequestAndReceiveSecDefinition);

    public override string? Description => "Отправить запрос на данные по инструменту, получить подтверждение запроса, получить данные";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var request = Helpers.CreateSecListSymbolRequest(_securityId);

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
        Logger.LogInformation(@"Получили сообщение об инструменте: {securityId}",
            sl.GetGroup(1, group).GetField(field).getValue());
    }
}