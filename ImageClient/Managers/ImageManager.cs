using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageClient.Managers
{
    public class ImageManager
    {
        private Dictionary<int, byte[]> chunks = new Dictionary<int, byte[]>();
        private string directoryPath = "../ImageHolder";

        public void AddChunk(int sequence, byte[] data)
        {
            chunks[sequence] = data;
        }

        public byte[] ReconstructImage()
        {
            return chunks
                .OrderBy(x => x.Key)
                .SelectMany(x => x.Value)
                .ToArray();
        }

        public string SaveImage()
        {
            var imageData = ReconstructImage();
            Directory.CreateDirectory(directoryPath);

            string fileName = $"image_{DateTime.Now:yyyyMMdd}.jpeg";
            string fullPath = Path.Combine(directoryPath, fileName);

            File.WriteAllBytes(fullPath, imageData);
            return fullPath;
        }

        public void Clear()
        {
            chunks.Clear();
        }
    }
}