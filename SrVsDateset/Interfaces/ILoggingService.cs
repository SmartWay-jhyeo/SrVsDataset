using System;

namespace SrVsDataset.Interfaces
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public interface ILoggingService
    {
        void Log(LogLevel level, string message);
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(string message, Exception exception);
        void SetLogLevel(LogLevel minLevel);
    }
}