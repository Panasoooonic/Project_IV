using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FrameworkApp.Managers;
using FrameworkApp.Models;
using FrameworkApp.Utils;

namespace Tests.UnitTests
{
    [TestClass]
    public class PacketHandlerTests
    {
        [TestMethod]
        public void Serialize_CreatesValidByteArray()
        {
            var handler = new PacketHandler();

            var packet = new Packet
            {
                PacketType = 1,
                CommandId = 2,
                SequenceNumber = 3,
                Payload = new byte[] { 10, 20, 30 }
            };

            byte[] result = handler.Serialize(packet);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public async Task Serialize_Then_ReadPacketAsync_ReturnsSamePacket()
        {
            var handler = new PacketHandler();

            var packet = new Packet
            {
                PacketType = 1,
                CommandId = 2,
                SequenceNumber = 5,
                Payload = new byte[] { 1, 2, 3 }
            };

            byte[] data = handler.Serialize(packet);

            using var stream = new MemoryStream(data);

            var result = await handler.ReadPacketAsync(stream, CancellationToken.None);

            Assert.AreEqual(packet.PacketType, result.PacketType);
            Assert.AreEqual(packet.CommandId, result.CommandId);
            Assert.AreEqual(packet.SequenceNumber, result.SequenceNumber);
            CollectionAssert.AreEqual(packet.Payload, result.Payload);
        }
    }
    [TestClass]
    public class LoggerTests
    {
        private string testDir = Path.Combine(Path.GetTempPath(), "LoggerTests");

        [TestMethod]
        public void LogPacket_WritesFormattedEntry()
        {
            var logger = new Logger(testDir);

            logger.LogPacket("SENT", 1, 2, 3, 100);

            string file = Path.Combine(testDir, "client_log.txt");
            string content = File.ReadAllText(file);

            Assert.IsTrue(content.Contains("SENT"));
            Assert.IsTrue(content.Contains("Type=1"));
            Assert.IsTrue(content.Contains("Command=2"));
        }
    }

    [TestClass]
    public class ImageManagerTests
    {
        private string testDir = Path.Combine(Path.GetTempPath(), "ImageManagerTests");

        [TestMethod]
        public void AddChunk_IncreasesChunkCount()
        {
            var manager = new ImageManager(testDir);

            var packet = new Packet
            {
                CommandId = CommandIds.ImageChunk,
                SequenceNumber = 1,
                Payload = new byte[] { 1, 2, 3 }
            };

            manager.AddChunk(packet);

            Assert.AreEqual(1, manager.ChunkCount);
        }

        [TestMethod]
        public void SaveImage_CreatesFile()
        {
            var manager = new ImageManager(testDir);

            manager.AddChunk(new Packet
            {
                CommandId = CommandIds.ImageChunk,
                SequenceNumber = 1,
                Payload = new byte[] { 1, 2, 3 }
            });

            manager.SetFinalFileName("test.jpg");

            string path = manager.SaveImage();

            Assert.IsTrue(File.Exists(path));
        }

        [TestMethod]
        public void Reset_ClearsChunks()
        {
            var manager = new ImageManager(testDir);

            manager.AddChunk(new Packet
            {
                CommandId = CommandIds.ImageChunk,
                SequenceNumber = 1,
                Payload = new byte[] { 1 }
            });

            manager.Reset();

            Assert.AreEqual(0, manager.ChunkCount);
        }
    }


}