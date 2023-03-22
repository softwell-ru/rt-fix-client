using System.Text;
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

        msg.AddGroup(new MarketDataRequest.NoMDEntryTypesGroup
        {
            MDEntryType = new MDEntryType(MDEntryType.TRADE)
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

    public static SecurityListRequest CreateSecListRequest()
    {
        var msg = new SecurityListRequest
        {
            SecurityReqID = new SecurityReqID(Guid.NewGuid().ToString()),
            SecurityListRequestType = new SecurityListRequestType(SecurityListRequestType.ALL_SECURITIES)
        };

        return msg;
    }

    public static SecurityListRequest CreateSecListSymbolRequest(string symbol)
    {
        var msg = new SecurityListRequest
        {
            SecurityReqID = new SecurityReqID(Guid.NewGuid().ToString()),
            SecurityListRequestType = new SecurityListRequestType(SecurityListRequestType.SYMBOL),
            Symbol = new Symbol(symbol)
        };

        return msg;
    }

    public static Quote CreateQuote(
        string securityId,
        decimal? bid,
        decimal? offer)
    {
        var res = new Quote
        {
            QuoteID = new QuoteID(Guid.NewGuid().ToString()),
            Symbol = new Symbol("[N/A]"),
            SecurityID = new SecurityID(securityId),
            SecurityIDSource = new SecurityIDSource(_hihiClubSecuritySourceId),
            QuoteType = new QuoteType(QuoteType.INDICATIVE),
            TransactTime = new TransactTime(DateTime.UtcNow)
        };

        if (bid.HasValue)
        {
            res.BidPx = new BidPx(bid.Value);
        }

        if (offer.HasValue)
        {
            res.OfferPx = new OfferPx(offer.Value);
        }

        return res;
    }

    public static QuoteCancel CreateQuoteCancel(
        string securityId)
    {
        var res = new QuoteCancel
        {
            QuoteMsgID = new QuoteMsgID(Guid.NewGuid().ToString())
        };

        res.AddGroup(new QuoteCancel.NoQuoteEntriesGroup
        {
            Symbol = new Symbol("[N/A]"),
            SecurityID = new SecurityID(securityId),
            SecurityIDSource = new SecurityIDSource(_hihiClubSecuritySourceId)
        });

        return res;
    }

    public static QuickFix.Message CreateChatsRequest(DateTime minDate, DateTime? maxDate)
    {
        var msg = new QuickFix.Message();

        msg.Header.SetField(new BeginString("FIXT.1.1"));
        msg.Header.SetField(new MsgType("UR"));

        msg.SetField(new StringField(11004, Guid.NewGuid().ToString())); // ChatsRequestID
        // msg.SetField(new StringField(11002, "some-id")); // ChatID

        var startGr = new QuickFix.Group(11005, Tags.TransactTime);
        startGr.SetField(new TransactTime(minDate.ToUniversalTime()));
        msg.AddGroup(startGr);

        if (maxDate.HasValue)
        {
            var endGr = new QuickFix.Group(11005, Tags.TransactTime);
            endGr.SetField(new TransactTime(maxDate.Value.ToUniversalTime()));
            msg.AddGroup(endGr);
        }

        return msg;
    }

    public static string? GetAndDecodeBase64Text(QuickFix.Message message)
    {
        var field = new StringField(11008);
        if (!message.IsSetField(field)) return null;

        var base64 = message.GetField(field).getValue();

        if (string.IsNullOrWhiteSpace(base64)) return null;

        var bytes = Convert.FromBase64String(base64);

        var res = Encoding.UTF8.GetString(bytes);

        return res;
    }
}