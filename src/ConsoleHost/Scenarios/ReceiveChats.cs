using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class ReceiveChats : ScenarioBase
{
    public ReceiveChats(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    public override string Name => nameof(ReceiveDeal);

    public override string? Description => "Получить сообщение о начале чата, сообщение в чате, сообщение о завершении чата в фоновом режиме";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        Logger.LogInformation("Ожидаем хотя бы одно сообщение о начале чата, завершении чата и сообщении в чате");

        var startReceived = false;
        var messageReceived = false;
        var endReceived = false;

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.IsOfType("US")) // chat start
            {
                //  ответ на запрос, а не фоновое сообщение
                if (msg.IsSetField(11004)) continue;

                Logger.LogInformation(
                    @"Получили фоновое сообщение о начале чата {subject}, id={id}",
                    Helpers.GetAndDecodeBase64Text(msg),
                    msg.GetField(new StringField(11002)));
                startReceived = true;
            }
            else if (msg.IsOfType("UE")) // chat end
            {
                //  ответ на запрос, а не фоновое сообщение
                if (msg.IsSetField(11004)) continue;

                Logger.LogInformation(@"Получили фоновое сообщение о завершении чата {id}", msg.GetField(new StringField(11002)));
                endReceived = true;
            }
            else if (msg.IsOfType("UM")) // chat message
            {
                //  ответ на запрос, а не фоновое сообщение
                if (msg.IsSetField(11004)) continue;

                Logger.LogInformation(
                    @"Получили фоновое сообщение из чата {id}: {message}",
                    msg.GetField(new StringField(11002)),
                    Helpers.GetAndDecodeBase64Text(msg));

                messageReceived = true;
            }

            if (startReceived && endReceived && messageReceived) return;
        }
    }
}