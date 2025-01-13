using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickFix.Fields;
using QuickFix.FIX50SP2;
using SoftWell.RtFix.ConsoleHost.Scenarios.Infrastructure;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public abstract class QuotationScenarioBase : ScenarioBase
{
    private readonly IOptions<List<OperationOptions>> _options;

    protected QuotationScenarioBase(ScenarioSettings settings, IOptions<List<OperationOptions>> options, ILoggerFactory loggerFactory) : base(settings, loggerFactory)
    {
        _options = options;
    }

    protected virtual OperationOptions ScenarioOptions => _options.Value.Where(x => x.Name == "CommonSettings").FirstOrDefault() ?? throw new ArgumentNullException();

    protected void SubscribeToRefreshes(ScenarioContext context)
    {
        SubscribeToRefreshes(context, ScenarioOptions.QuotationSecurityId);
    }

    protected void SubscribeToRefreshes(ScenarioContext context, string quotationSecurityId)
    {
        var quotationsRequest = Helpers.CreateQuotationRequest(new[] { quotationSecurityId }, null);
        context.Client.SendMessage(quotationsRequest);
        Logger.LogInformation("Отправили подписку на котировку {QuotationSecurityId}..", quotationSecurityId);
    }

    protected async Task<MarketDataSnapshotFullRefresh> WaitForSnapshotFullRefreshAsync(ScenarioContext context, CancellationToken ct)
    {
        while (true)
        {
            var mes = await WaitForMessageAsync(context, MsgType.MARKET_DATA_SNAPSHOT_FULL_REFRESH, ct);
            var mdr = (MarketDataSnapshotFullRefresh)mes;
            if (mdr.SecurityID.getValue() == ScenarioOptions.QuotationSecurityId)
            {
                Logger.LogInformation("Получили MARKET_DATA_SNAPSHOT_FULL_REFRESH для {securityId}..", ScenarioOptions.QuotationSecurityId);
                return mdr;
            }
        }
    }

    protected static IEnumerable<MarketDataIncrementalRefresh.NoMDEntriesGroup> EnumerateMDEntriesGroups(MarketDataIncrementalRefresh mdir)
    {
        for (var i = 1; i <= mdir.NoMDEntries.getValue(); i++)
        {
            var g = new MarketDataIncrementalRefresh.NoMDEntriesGroup();
            mdir.GetGroup(i, g);

            yield return g;
        }
    }
}
