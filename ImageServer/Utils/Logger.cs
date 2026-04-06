using System;
using System.IO;

namespace ImageServer.Utils
{
    public class Logger
    {
        private readonly string _logFile;

        public Logger(string? logDirectory = null)
        {
            string directory = logDirectory ?? "../Logs";
            Directory.CreateDirectory(directory);
            _logFile = Path.Combine(directory, "server_log.txt");
        }

        public void Log(string message)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            File.AppendAllText(_logFile, entry + Environment.NewLine);
        }

        public void LogPacket(string direction, int packetType, int commandId, int sequenceNumber, int payloadLength)
        {
            Log($"{direction} | Type={packetType} | Command={commandId} | Seq={sequenceNumber} | PayloadBytes={payloadLength}");
        }

        public void LogAuthSuccess(string username)
        {
            Log($"AUTH SUCCESS | User={username}");
        }

        public void LogAuthFailure(string username)
        {
            Log($"AUTH FAILURE | User={username}");
        }

        public void LogTransferStart(string fileName)
        {
            Log($"TRANSFER START | File={fileName}");
        }

        public void LogTransferComplete(string fileName, int chunksSent)
        {
            Log($"TRANSFER COMPLETE | File={fileName} | Chunks={chunksSent}");
        }

        public void LogInvalidCommand(int commandId)
        {
            Log($"INVALID COMMAND | Command={commandId}");
        }

        public void LogMalformedPacket(string detail)
        {
            Log($"MALFORMED PACKET | {detail}");
        }

        public void LogError(string error)
        {
            Log($"ERROR | {error}");
        }
    }
}