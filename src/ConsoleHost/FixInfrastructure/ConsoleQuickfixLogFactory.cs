using Microsoft.Extensions.Logging;
using QuickFix;

namespace SoftWell.RtFix.ConsoleHost.FixInfrastructure;

public class ConsoleQuickfixLogFactory : ILogFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public ConsoleQuickfixLogFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public ILog Create(SessionID sessionID)
    {
        return new Logger(_loggerFactory.CreateLogger($"quickfix:{sessionID}"));
    }

    private class Logger : ILog
    {
        private readonly ILogger _logger;

        public Logger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Clear()
        {
        }

        public void Dispose()
        {
        }

        public void OnEvent(string s)
        {
            Log("event", s);
        }

        public void OnIncoming(string msg)
        {
            Log("incoming", msg);
        }

        public void OnOutgoing(string msg)
        {
            Log("outgoing", msg);
        }

        private void Log(string category, string message)
        {
            _logger.LogInformation("{category}: {message}", category, message.ToFixReadable());
        }
    }
}
