using Microsoft.Extensions.Logging;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class ReceiveDeal : ScenarioBase
{
    public ReceiveDeal(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    protected override string Name => "Получить сделку";

    protected override string? Description => "Получить одну сделку в фоновом режиме";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        await WaitForLogonAsync(context, ct);

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.Message.Header.GetString(Tags.MsgType) == MsgType.TRADE_CAPTURE_REPORT)
            {
                var m = (TradeCaptureReport)msg.Message;

                //  ответ на запрос, а не фоновая сделка
                if (m.IsSetTradeRequestID()) continue;

                LogTradeCaptureReport(m);
                return;
            }
        }
    }

    private void LogTradeCaptureReport(TradeCaptureReport mds)
    {
        Logger.LogInformation(@"Получили фоновое сообщение о сделке: 
{fpml}",
            mds.SecurityXML.getValue());
    }
}