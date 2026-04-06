using System.IO;

namespace ImageServer.Models
{
    public class ServerConfig
    {
        public int Port { get; set; } = 5000;
        public string ImageDirectory { get; set; } = "../ServerImages";
        public string LogDirectory { get; set; } = "../Logs";
        public string DefaultImageFileName { get; set; } = "sample.jpg";
        public int ChunkSize { get; set; } = 64 * 1024;

        public string GetImagePath(string? requestedFileName)
        {
            string safeName = string.IsNullOrWhiteSpace(requestedFileName)
                ? DefaultImageFileName
                : Path.GetFileName(requestedFileName);

            return Path.Combine(ImageDirectory, safeName);
        }
    }
}