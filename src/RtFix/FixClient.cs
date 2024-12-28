using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;

namespace SoftWell.RtFix;

public class FixClient : IApplication, IAsyncDisposable
{
    private readonly SessionSettings _sessionSettings;

    private readonly Channel<Message> _channel;

    private readonly ILogger _logger;

    private Session _session = null!;

    public FixClient(
        SessionSettings sessionSettings,
        ILogger<FixClient> logger)
    {
        _sessionSettings = sessionSettings ?? throw new ArgumentNullException(nameof(sessionSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _channel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = true
        });
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {
        _logger.LogTrace("FROMADMIN: {session}, {message}", sessionID, message.ToLog());
        _channel.Writer.TryWrite(message);
    }

    public void FromApp(Message message, SessionID sessionID)
    {
        _logger.LogTrace("FROMAPP: {session}, {message}", sessionID, message.ToLog());
        _channel.Writer.TryWrite(message);
    }

    public void OnCreate(SessionID sessionID)
    {
        _session = Session.LookupSession(sessionID) ?? throw new ArgumentException("Unknown session id", nameof(sessionID));
        _logger.LogTrace("Создана сессия: {session}", sessionID);
    }

    public void OnLogon(SessionID sessionID)
    {
        _logger.LogTrace("LOGON: {session}", sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        _logger.LogTrace("LOGOUT: {session}", sessionID);
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

        _logger.LogTrace("TOADMIN: {session}, {message}", sessionID, message.ToLog());
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

        _logger.LogTrace("TOAPP: {session}, {message}", sessionID, message.ToLog());
    }

    public void SendMessage(Message m)
    {
        if (_session == null) throw new InvalidOperationException("Отсутствует сессия");

        _session.Send(m);
    }

    public void Logout()
    {
        if (_session == null) throw new InvalidOperationException("Отсутствует сессия");

        _session.Logout();
    }

    public IAsyncEnumerable<Message> ReadAllMessagesAsync(CancellationToken ct = default)
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
