using QuickFix.Fields;
using QuickFix.FIX50SP2;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public static class Helpers
{
    private const string _hihiClubSecuritySourceId = "177";

    public static MarketDataRequest CreateQuotationRequest(
        IEnumerable<string> securitiesIds,
        IEnumerable<string>? partysIds)
    {
        ArgumentNullException.ThrowIfNull(securitiesIds);

        var sIds = securitiesIds.ToArray();

        if (sIds.Length == 0) throw new ArgumentException("Список инструментов не должен быть пустым", nameof(securitiesIds));

        var pIds = partysIds?.ToArray() ?? Array.Empty<string>();

        var msg = new MarketDataRequest
        {
            MDReqID = new MDReqID(Guid.NewGuid().ToString()),
            MarketDepth = new MarketDepth(1),
            NoMDEntryTypes = new NoMDEntryTypes(2),
            SubscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES),
            MDUpdateType = new MDUpdateType(MDUpdateType.INCREMENTAL_REFRESH),
            NoRelatedSym = new NoRelatedSym(sIds.Length)
        };

        msg.AddGroup(new MarketDataRequest.NoMDEntryTypesGroup
        {
            MDEntryType = new MDEntryType(MDEntryType.BID)
        });

        msg.AddGroup(new MarketDataRequest.NoMDEntryTypesGroup
        {
            MDEntryType = new MDEntryType(MDEntryType.OFFER)
        });

        foreach (var sId in sIds)
        {
            msg.AddGroup(new MarketDataRequest.NoRelatedSymGroup
            {
                Symbol = new Symbol("[N/A]"),
                SecurityID = new SecurityID(sId),
                SecurityIDSource = new SecurityIDSource(_hihiClubSecuritySourceId)
            });
        }

        if (pIds.Length > 0)
        {
            msg.NoPartyIDs = new NoPartyIDs(pIds.Length);

            foreach (var pId in pIds)
            {
                msg.AddGroup(new MarketDataRequest.NoPartyIDsGroup
                {
                    PartyID = new PartyID(pId),
                    PartyIDSource = new PartyIDSource(PartyIDSource.PROPRIETARY_CUSTOM_CODE)
                });
            }
        }

        return msg;
    }

    public static TradeCaptureReportRequest CreateDealsReportRequest(DateTime minDate, DateTime? maxDate)
    {
        var msg = new TradeCaptureReportRequest
        {
            TradeRequestID = new TradeRequestID(Guid.NewGuid().ToString()),
            SubscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT),
            TradeRequestType = new TradeRequestType(TradeRequestType.ALL_TRADES),
            NoDates = new NoDates(maxDate.HasValue ? 2 : 1)
        };

        msg.AddGroup(new TradeCaptureReportRequest.NoDatesGroup
        {
            TradeDate = new TradeDate(minDate.ToString("yyyyMMdd"))
        });

        if (maxDate.HasValue)
        {
            msg.AddGroup(new TradeCaptureReportRequest.NoDatesGroup
            {
                TradeDate = new TradeDate(maxDate.Value.ToString("yyyyMMdd"))
            });
        }

        return msg;
    }
}