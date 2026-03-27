using System;
using System.IO;

namespace ImageClient.Utils
{
    public class Logger
    {
        private string logFile;
        // Default to a "Logs" folder in project directory    
        private string directoryPath = "../Logs";

        public Logger()
        {
            // Create folder if it doesn't exist
            Directory.CreateDirectory(directoryPath);

            logFile = Path.Combine(directoryPath, "client_log.txt");
        }
        
        public void Log(string message)
        {
            string entry = $"[{DateTime.Now}] {message}";
            File.AppendAllText(logFile, entry + Environment.NewLine);
        }

        public void LogPacket(string direction, string type)
        {
            Log($"{direction} | {type}");
        }

        public void LogError(string error)
        {
            Log($"ERROR: {error}");
        }
    }
}