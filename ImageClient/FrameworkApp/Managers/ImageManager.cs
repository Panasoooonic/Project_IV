using System;
using System.Collections.Generic;
using System.IO;
using FrameworkApp.Models;

namespace FrameworkApp.Managers
{
    /// <summary>
/// Handles reconstruction of image data on the client side.
/// </summary>
/// <remarks>
/// Collects image chunks and reassembles them into a complete image file.
/// </remarks>
    public class ImageManager
    {
        private readonly string _outputDirectory;
        private readonly SortedDictionary<int, byte[]> _chunks = new();
        private string _finalFileName = "downloaded_image.jpg";

        public ImageManager(string? outputDirectory = null)
        {
            _outputDirectory = outputDirectory ?? Path.Combine(
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\")),
                "ImageHolder");

            Directory.CreateDirectory(_outputDirectory);
        }

/// <summary>
/// Clears all stored chunks.
/// </summary>
        public void Reset()
        {
            _chunks.Clear();
            _finalFileName = "downloaded_image.jpg";
        }

/// <summary>
/// Adds an image chunk to the collection.
/// </summary>
/// <param name="packet">Packet containing image data</param>
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

/// <summary>
/// Saves the reconstructed image to disk.
/// </summary>
/// <returns>File path of saved image</returns>
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