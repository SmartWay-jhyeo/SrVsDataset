using System;
using System.IO;
using System.Threading.Tasks;
using SrVsDataset.Interfaces;

namespace SrVsDataset.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly string _logFilePath;
        private LogLevel _minLogLevel = LogLevel.Info;
        private readonly object _lock = new object();

        public LoggingService(string logDirectory = null)
        {
            if (string.IsNullOrEmpty(logDirectory))
            {
                logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            }
            
            Directory.CreateDirectory(logDirectory);
            _logFilePath = Path.Combine(logDirectory, $"SrVsDataset_{DateTime.Now:yyyyMMdd}.log");
        }

        public void SetLogLevel(LogLevel minLevel)
        {
            _minLogLevel = minLevel;
        }

        public void Log(LogLevel level, string message)
        {
            if (level < _minLogLevel) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level}] {message}";

            lock (_lock)
            {
                try
                {
                    // 콘솔과 디버그 창에 출력
                    Console.WriteLine(logEntry);
                    System.Diagnostics.Debug.WriteLine(logEntry);

                    // 파일에 기록
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // 로깅 실패 시에도 애플리케이션이 중단되면 안됨
                }
            }
        }

        public void LogDebug(string message) => Log(LogLevel.Debug, message);
        public void LogInfo(string message) => Log(LogLevel.Info, message);
        public void LogWarning(string message) => Log(LogLevel.Warning, message);
        public void LogError(string message) => Log(LogLevel.Error, message);

        public void LogError(string message, Exception exception)
        {
            var fullMessage = $"{message} - Exception: {exception.Message}";
            if (exception.StackTrace != null)
            {
                fullMessage += $"\nStackTrace: {exception.StackTrace}";
            }
            Log(LogLevel.Error, fullMessage);
        }
    }
}