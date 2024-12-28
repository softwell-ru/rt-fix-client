using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendSecListRequestAndReceiveSecList : ScenarioBase
{
    public SendSecListRequestAndReceiveSecList(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    public override string Name => nameof(SendSecListRequestAndReceiveSecList);

    public override string? Description => "Отправить запрос на списки инструментов, получить подтверждение запроса, получить списки";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var request = Helpers.CreateSecListRequest();

        context.Client.SendMessage(request);

        Logger.LogInformation("Отправили запрос на получение списка инструментов, ожидаем результат..");

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
        Logger.LogInformation(@"Получили сообщение о списке инструментов в количестве: {number}",
            sl.NoRelatedSym.getValue());
    }
}