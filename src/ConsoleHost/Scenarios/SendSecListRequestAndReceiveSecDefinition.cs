using Microsoft.Extensions.Logging;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class SendSecListRequestAndReceiveSecDefinition : ScenarioBase
{
    // private readonly string _fra = "FRA-EUR-3M-6M";

    private readonly string _repo = "REPO-GCOLLATERAL-1M";

    // private readonly string _fx = "FX-AUD-CHF-SPOT";

    // private readonly string _fx100 = "FX-AMD-RUB-SPOT";

    // private readonly string _fx2pips = "FX-USD-JPY-SPOT";

    // private readonly string _fx20pips = "FX-JPY-RUB-TOD";

    // private readonly string _fx15pips = "FX-RUB-KGS-SPOT";

    // private readonly string _fxtom = "FX-CHF-RUB-TOM";

    // private readonly string _fxswap = "FXS-EUR-USD-3M";

    // private readonly string _fxswap2 = "FXS-EUR-USD-1W";

    // private readonly string _fxswaptn = "FXS-USD-RUB-TN";

    // private readonly string _fxswapon = "FXS-USD-RUB-ON";

    // private readonly string _fxswaptom = "FXS-USD-RUB-4Y";

    // private readonly string _fxswaptom2 = "FXS-USD-RUB-18M";

    // private readonly string _swap = "EURUSD-LIBOR-3M-BASIS-1Y";

    // private readonly string _swap2 = "EURUSD-LIBOR-3M-BASIS-5Y";

    // private readonly string _irs = "RUB-MOSPRIME-NFEA-3M-10Y";

    // private readonly string _irs2 = "RUB-MOSPRIME-NFEA-3M-3Y";

    // private readonly string _ois = "RUB-RUONIA-OIS-COMPOUND-1W";

    // private readonly string _dp = "DP-RUB-1W";

    // private readonly string _dpon = "DP-RUB-ON";

    // private readonly string _dpspot = "DP-USD-1M";

    // private readonly string _bond = "ОФЗ 26207 Bond Tod";

    // private readonly string _bondcode = "SU26207RMFS9";

    // private readonly string _prec = "PREC-XAG-USD-TOM";

    // private readonly string _prec2 = "PREC-XAU-USD-SPOT";

    public SendSecListRequestAndReceiveSecDefinition(
        ScenarioSettings settings,
        ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
    }

    public override string Name => nameof(SendSecListRequestAndReceiveSecDefinition);

    public override string? Description => "Отправить запрос на данные по инструменту, получить подтверждение запроса, получить данные";

    protected override async Task RunAsyncInner(ScenarioContext context, CancellationToken ct)
    {
        var request = Helpers.CreateSecListSymbolRequest(_repo);

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
        Logger.LogInformation(@"Получили сообщение об инструменте: {fpml}",
            sl.SecurityReqID.getValue());//TODO: find xml field
    }
}