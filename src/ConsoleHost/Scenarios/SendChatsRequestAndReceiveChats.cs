using Microsoft.Extensions.Logging;
using QuickFix.Fields;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendChatsRequestAndReceiveChats : ScenarioBase
{
    public SendChatsRequestAndReceiveChats(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    public override string Name => nameof(SendChatsRequestAndReceiveChats);

    public override string? Description => "Отправить запрос на чаты за вчера и сегодня, получить подтверждение запроса, получить сообщения из чатов";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var request = Helpers.CreateChatsRequest(DateTime.Now.AddDays(-1), null);

        context.Client.SendMessage(request);

        Logger.LogInformation("Отправили запрос на получение чатов, ожидаем результат..");

        var count = 0;

        var id = request.GetString(11004);

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.IsOfType("US")) // chat start
            {
                if (!msg.IsSetField(11004) || msg.GetString(11004) != id) continue;

                Logger.LogInformation(
                    @"Получили сообщение о начала чата {subject}, id={id}",
                    msg.GetField(new Subject()),
                    msg.GetField(new StringField(11002)));
                count++;
            }
            else if (msg.IsOfType("UE")) // chat end
            {
                if (!msg.IsSetField(11004) || msg.GetString(11004) != id) continue;

                Logger.LogInformation(@"Получили фоновое сообщение о завершении чата {id}", msg.GetField(new StringField(11002)));
                count++;
            }
            else if (msg.IsOfType("UM")) // chat message
            {
                if (!msg.IsSetField(11004) || msg.GetString(11004) != id) continue;

                Logger.LogInformation(
                    @"Получили фоновое сообщение из чата {id}: {message}",
                    msg.GetField(new StringField(11002)),
                    msg.GetField(new Text()));

                count++;
            }
            else if (msg.IsOfType("UW")) // chats request ack
            {
                if (!msg.IsSetField(11004) || msg.GetString(11004) != id) continue;

                var status = msg.GetInt(11007); // status

                if (status == 1)
                {
                    throw new Exception("Пришел отказ на получение чатов с причиной: " + msg.GetInt(11006));
                }

                if (status == 0)
                {
                    if (count == 0)
                    {
                        Logger.LogWarning("Отправка сообщений сервером завершена, но сообщений от сервера не пришло");
                    }
                    else
                    {
                        Logger.LogInformation("Отправка сообщений сервером завершена. Получено сообщений: {count}", count);
                    }

                    return;
                }

                throw new Exception($"Пришло подтверждение на получение сообщений с неизвестным статусом: {status} и результатом {msg.GetInt(11006)}");
            }
        }
    }
}