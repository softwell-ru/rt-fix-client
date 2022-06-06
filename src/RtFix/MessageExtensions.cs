using System.Diagnostics.CodeAnalysis;
using QuickFix.Fields;

namespace SoftWell.RtFix;

public static class Extensions
{
    public static string ToLog(this QuickFix.Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return message.ToString().ToFixReadable();
    }

    public static string ToFixReadable(this string str)
    {
        ArgumentNullException.ThrowIfNull(str);

        return str.Replace('\u0001', '|');
    }

    public static bool IsOfType<TMessage>(
        this QuickFix.Message message,
        string msgType,
        [NotNullWhen(true)] out TMessage? typedMessage)
            where TMessage : QuickFix.Message
    {
        if (!message.IsOfType(msgType))
        {
            typedMessage = null;
            return false;
        }

        typedMessage = (TMessage)message;
        return true;
    }

    public static bool IsOfType(
        this QuickFix.Message message,
        string msgType)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(msgType);

        return message.Header.GetString(Tags.MsgType) == msgType;
    }
}
