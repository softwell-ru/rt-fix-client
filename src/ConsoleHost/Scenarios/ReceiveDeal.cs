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

    public override string Name => nameof(ReceiveDeal);

    public override string? Description => "Получить одну сделку в фоновом режиме";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        Logger.LogInformation("Ожидаем хотя бы одну сделку");

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.IsOfType<TradeCaptureReport>(MsgType.TRADE_CAPTURE_REPORT, out var tcr))
            {
                //  ответ на запрос, а не фоновая сделка
                if (tcr.IsSetTradeRequestID()) continue;

                LogTradeCaptureReport(tcr);
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