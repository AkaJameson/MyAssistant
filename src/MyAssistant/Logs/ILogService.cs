using System;

namespace MyAssistant.Logs
{
    public interface ILogService
    {
        void Information(string message, params object[] args);
        void Warning(string message, params object[] args);
        void Error(string message, Exception exception = null, params object[] args);
        void Fatal(string message, Exception exception = null, params object[] args);
        void Debug(string message, params object[] args);
    }
}