using Microsoft.Extensions.Logging;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendDealsRequestAndReceiveDeal : ScenarioBase
{
    public SendDealsRequestAndReceiveDeal(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    protected override string Name => nameof(SendDealsRequestAndReceiveDeal);

    protected override string? Description => "Отправить запрос на сделки за последние 10 дней, получить подтверждение запроса, получить сделки, если есть";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        await WaitForLogonAsync(context, ct);

        var request = Helpers.CreateDealsReportRequest(DateTime.Now.AddDays(-10), null);

        context.Client.SendMessage(request);

        var count = 0;

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.Message.Header.GetString(Tags.MsgType) == MsgType.TRADE_CAPTURE_REPORT_REQUEST_ACK)
            {
                var m = (TradeCaptureReportRequestAck)msg.Message;

                if (m.TradeRequestID.getValue() != request.TradeRequestID.getValue()) continue;

                var status = m.TradeRequestStatus.getValue();

                if (status == TradeRequestStatus.REJECTED)
                {
                    throw new Exception("Пришел отказ на получение сделок с причиной: " + m.TradeRequestResult.getValue());
                }

                if (status == TradeRequestStatus.COMPLETED)
                {
                    Logger.LogInformation("Отправка сделок сервером завершена. Получено сделок: {count}", count);
                    return;
                }

                throw new Exception($"Пришло подтверждение на получение сделок с неизвестным статусом: {status} и результатом {m.TradeRequestResult.getValue()}");
            }
            else if (msg.Message.Header.GetString(Tags.MsgType) == MsgType.TRADE_CAPTURE_REPORT)
            {
                var m = (TradeCaptureReport)msg.Message;

                if (m.TradeRequestID.getValue() != request.TradeRequestID.getValue()) continue;

                LogTradeCaptureReport(m);
                count++;
            }
        }
    }

    private void LogTradeCaptureReport(TradeCaptureReport mds)
    {
        Logger.LogInformation(@"Получили сообщение о сделке: 
{fpml}",
            mds.SecurityXML.getValue());
    }
}