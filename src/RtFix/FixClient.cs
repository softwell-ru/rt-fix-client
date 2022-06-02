using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;

namespace SoftWell.RtFix;

public class FixClient : IApplication, IAsyncDisposable
{
    private readonly SessionSettings _sessionSettings;

    private readonly Channel<MessageWrapper> _channel;

    private readonly ILogger _logger;

    private Session _session = null!;

    public FixClient(
        SessionSettings sessionSettings,
        ILogger<FixClient> logger)
    {
        _sessionSettings = sessionSettings ?? throw new ArgumentNullException(nameof(sessionSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _channel = Channel.CreateUnbounded<MessageWrapper>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = true
        });
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {
        if (message.Header.GetString(Tags.MsgType) == MsgType.LOGOUT
            && message.IsSetField(Tags.Text)
            && message.GetString(Tags.Text).StartsWith("MsgSeqNum too low"))
        {
            var seqno = message.Header.GetInt(Tags.MsgSeqNum);
            _logger.LogDebug("Сбрасываем seqno для {session}: {seqno}", sessionID, seqno);
            _session.NextSenderMsgSeqNum = seqno;
            _session.NextTargetMsgSeqNum = seqno;
            return;
        }

        _logger.LogDebug("FROMADMIN: {session}, {message}", sessionID, message.ToLog());
        _channel.Writer.TryWrite(new MessageWrapper(message, sessionID, DateTime.Now));
    }

    public void FromApp(Message message, SessionID sessionID)
    {
        _logger.LogDebug("FROMAPP: {session}, {message}", sessionID, message.ToLog());
        _channel.Writer.TryWrite(new MessageWrapper(message, sessionID, DateTime.Now));
    }

    public void OnCreate(SessionID sessionID)
    {
        _session = Session.LookupSession(sessionID);
        _logger.LogDebug("Создана сессия: {session}", sessionID);
    }

    public void OnLogon(SessionID sessionID)
    {
        _logger.LogDebug("LOGON: {session}", sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        _logger.LogDebug("LOGOUT: {session}", sessionID);
    }

    public void ToAdmin(Message message, SessionID sessionID)
    {
        if (message.Header.GetString(Tags.MsgType) == MsgType.LOGON)
        {
            var settings = _sessionSettings.Get(sessionID);

            if (settings.Has("Username"))
            {
                message.SetField(new Username(settings.GetString("Username")));
            }

            if (settings.Has("Password"))
            {
                message.SetField(new Password(settings.GetString("Password")));
            }
        }

        _logger.LogDebug("TOADMIN: {session}, {message}", sessionID, message.ToLog());
    }

    public void ToApp(Message message, SessionID sessionID)
    {
        try
        {
            var possDupFlag = false;
            if (message.Header.IsSetField(Tags.PossDupFlag))
            {
                possDupFlag = QuickFix.Fields.Converters.BoolConverter.Convert(
                    message.Header.GetString(Tags.PossDupFlag));
            }
            if (possDupFlag)
                throw new DoNotSend();
        }
        catch (FieldNotFoundException)
        { }

        _logger.LogDebug("TOAPP: {session}, {message}", sessionID, message.ToLog());
    }

    public void SendMessage(Message m)
    {
        if (_session == null) throw new InvalidOperationException("Отсутствует сессия");

        _session.Send(m);
    }

    public IAsyncEnumerable<MessageWrapper> ReadAllMessagesAsync(CancellationToken ct = default)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        await _channel.Reader.Completion;
        GC.SuppressFinalize(this);
    }
}
