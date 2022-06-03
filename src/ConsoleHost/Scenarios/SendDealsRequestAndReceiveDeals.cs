using Microsoft.Extensions.Logging;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendDealsRequestAndReceiveDeals : ScenarioBase
{
    public SendDealsRequestAndReceiveDeals(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    public override string Name => nameof(SendDealsRequestAndReceiveDeals);

    public override string? Description => "Отправить запрос на сделки за вчера и сегодня, получить подтверждение запроса, получить сделки";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var request = Helpers.CreateDealsReportRequest(DateTime.Now.AddDays(-1), null);

        context.Client.SendMessage(request);

        Logger.LogInformation("Отправили запрос на получение сделок, ожидаем результат..");

        var count = 0;

        await foreach (var msg in context.Client.ReadAllMessagesAsync(ct))
        {
            if (msg.Message.IsOfType<TradeCaptureReportRequestAck>(MsgType.TRADE_CAPTURE_REPORT_REQUEST_ACK, out var tcra))
            {
                if (tcra.TradeRequestID.getValue() != request.TradeRequestID.getValue()) continue;

                var status = tcra.TradeRequestStatus.getValue();

                if (status == TradeRequestStatus.REJECTED)
                {
                    throw new Exception("Пришел отказ на получение сделок с причиной: " + tcra.TradeRequestResult.getValue());
                }

                if (status == TradeRequestStatus.COMPLETED)
                {
                    if (count == 0)
                    {
                        Logger.LogWarning("Отправка сделок сервером завершена, но сделок от сервера не пришло");
                    }
                    else
                    {
                        Logger.LogInformation("Отправка сделок сервером завершена. Получено сделок: {count}", count);
                    }

                    return;
                }

                throw new Exception($"Пришло подтверждение на получение сделок с неизвестным статусом: {status} и результатом {tcra.TradeRequestResult.getValue()}");
            }
            else if (msg.Message.IsOfType<TradeCaptureReport>(MsgType.TRADE_CAPTURE_REPORT, out var tcr))
            {
                if (tcr.TradeRequestID.getValue() != request.TradeRequestID.getValue()) continue;

                count++;
                LogTradeCaptureReport(tcr);
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