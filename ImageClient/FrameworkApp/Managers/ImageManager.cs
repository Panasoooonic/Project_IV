using System;
using System.Collections.Generic;
using System.IO;
using FrameworkApp.Models;

namespace FrameworkApp.Managers
{
    public class ImageManager
    {
        private readonly string _outputDirectory;
        private readonly SortedDictionary<int, byte[]> _chunks = new();
        private string _finalFileName = "downloaded_image.jpg";

        public ImageManager(string? outputDirectory = null)
        {
            _outputDirectory = outputDirectory ?? "../ImageHolder";
            Directory.CreateDirectory(_outputDirectory);
        }

        public void Reset()
        {
            _chunks.Clear();
            _finalFileName = "downloaded_image.jpg";
        }

        public void AddChunk(Packet packet)
        {
            if (packet.CommandId != CommandIds.ImageChunk)
            {
                return;
            }

            if (!_chunks.ContainsKey(packet.SequenceNumber))
            {
                _chunks[packet.SequenceNumber] = packet.Payload;
            }
        }

        public void SetFinalFileName(string fileNameFromServer)
        {
            if (!string.IsNullOrWhiteSpace(fileNameFromServer))
            {
                _finalFileName = Path.GetFileName(fileNameFromServer);
            }
        }

        public string SaveImage()
        {
            string outputPath = Path.Combine(_outputDirectory, _finalFileName);

            using FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

            foreach (var chunk in _chunks)
            {
                fileStream.Write(chunk.Value, 0, chunk.Value.Length);
            }

            return outputPath;
        }

        public int ChunkCount => _chunks.Count;
    }
}