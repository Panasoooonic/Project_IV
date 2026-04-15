using System;
using System.IO;

namespace FrameworkApp.Utils
{
    public class Logger
    {
        private readonly string _logFile;
         private static readonly object _lock = new object();

        public Logger(string? logDirectory = null)
        {
            string directory = logDirectory ?? Path.Combine(
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\")),
                "Logs");

            Directory.CreateDirectory(directory);
            _logFile = Path.Combine(directory, "client_log.txt");
        }

         public void Log(string message)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

            lock (_lock)
            {
                File.AppendAllText(_logFile, entry + Environment.NewLine);
            }
        }

        public void LogPacket(string direction, int packetType, int commandId, int sequenceNumber, int payloadLength)
        {
            Log($"{direction} | Type={packetType} | Command={commandId} | Seq={sequenceNumber} | PayloadBytes={payloadLength}");
        }
    }
}