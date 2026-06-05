using System;
using System.IO;
using System.Threading;

namespace LotComWatcher.Models.Factories
{
    /// <summary>
    /// Simple Logger for Service Monitor
    /// </summary>
    public static class LoggerHelper
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string LogPrefix = "Exception_";
        
        private static readonly ReaderWriterLockSlim CacheLock = new ReaderWriterLockSlim();
        
        /// <summary>
        /// Write Exception Log
        /// </summary>
        /// <param name="message">log content</param>
        public static void LogException(string message)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                string fileName = GetLogFileName();
                string fullPath = Path.Combine(LogDirectory, fileName);
                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                string logEntry = $"----------------------------------------------{Environment.NewLine}";
                logEntry += $"[DateTime] {currentTime}{Environment.NewLine}[InvalidScan] {message}{Environment.NewLine}{Environment.NewLine}";
                
                File.AppendAllText(fullPath, logEntry);

                // Try to write log header if it is a new log file per day
                TryWriteHeader(fullPath, currentTime);
            }
            catch
            {
                // ToDo
            }
        }

        private static string GetLogFileName()
        {
            return $"{LogPrefix}{DateTime.Now:yyyyMMdd}.log";
        }

        private static void TryWriteHeader(string filePath, string currentTime)
        {
            if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
            {
                try
                {
                    CacheLock.EnterWriteLock();
                    if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
                    {
                        string header = $"<time_location>{Environment.NewLine}";
                        header += $"[Time]：{currentTime}";
                        header += $"</time_location>{Environment.NewLine}";
                        header += $"Service Monitor Begin...{Environment.NewLine}{Environment.NewLine}";
                        
                        File.WriteAllText(filePath, header); 
                    }
                }
                finally
                {
                    CacheLock.ExitWriteLock();
                }
            }
        }
    }
}