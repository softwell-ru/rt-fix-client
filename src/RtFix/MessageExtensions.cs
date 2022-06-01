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

        return str.Replace('\u0001', '☺');
    }
}
