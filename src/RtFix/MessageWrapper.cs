using QuickFix;

namespace SoftWell.RtFix;

public class MessageWrapper
{
    public MessageWrapper(Message message, SessionID sessionId, DateTime receiveTime)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        ReceiveTime = receiveTime;
    }

    public Message Message { get; }

    public SessionID SessionId { get; }

    public DateTime ReceiveTime { get; }
}
