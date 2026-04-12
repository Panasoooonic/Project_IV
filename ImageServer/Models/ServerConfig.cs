using System;
using System.IO;

namespace ImageServer.Models
{
    public class ServerConfig
    {
        public int Port { get; set; } = 5000;
        public string ImageDirectory { get; set; }
        public string LogDirectory { get; set; }
        public string DefaultImageFileName { get; set; } = "image_20260406.jpeg";
        public int ChunkSize { get; set; } = 64 * 1024;

        public ServerConfig()
        {
            string baseDir = AppContext.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\"));

            ImageDirectory = Path.Combine(projectRoot, "ServerImages");
            LogDirectory = Path.Combine(projectRoot, "Logs");
        }

        public string GetImagePath(string? requestedFileName)
        {
            string safeName = string.IsNullOrWhiteSpace(requestedFileName)
                ? DefaultImageFileName
                : Path.GetFileName(requestedFileName);

            return Path.Combine(ImageDirectory, safeName);
        }
    }
}