using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageServer.Managers;
using ImageServer.Models;
using ImageServer.Utils;


namespace Tests.UnitTests
{
    [TestClass]
    public class Server_AuthManagerTests
    {
        [TestMethod]
        public void ValidCredentials_ReturnsTrue()
        {
            var auth = new AuthManager();
            byte[] payload = Encoding.UTF8.GetBytes("admin:password123");

            bool result = auth.TryValidateCredentials(payload, out string user);

            Assert.IsTrue(result);
            Assert.AreEqual("admin", user);
        }

        [TestMethod]
        public void InvalidCredentials_ReturnsFalse()
        {
            var auth = new AuthManager();
            byte[] payload = Encoding.UTF8.GetBytes("admin:wrong");

            bool result = auth.TryValidateCredentials(payload, out _);

            Assert.IsFalse(result);
        }
    }

 [TestClass]
    public class Server_StateManagerTests
    {
        [TestMethod]
        public void InitialState_IsWaiting()
        {
            var state = new StateManager();
            Assert.AreEqual(SessionState.WaitingForConnection, state.CurrentState);
        }

        [TestMethod]
        public void StateTransitions_WorkCorrectly()
        {
            var state = new StateManager();

            state.MarkConnected();
            state.MarkAuthenticated();
            state.MarkReady();

            Assert.AreEqual(SessionState.Ready, state.CurrentState);
        }

        [TestMethod]
        public void CanRequestImage_OnlyWhenReadyAndAuthenticated()
        {
            var state = new StateManager();

            state.MarkConnected();
            state.MarkAuthenticated();
            state.MarkReady();

            Assert.IsTrue(state.CanRequestImage(true));
            Assert.IsFalse(state.CanRequestImage(false));
        }
    }


      [TestClass]
    public class Server_PacketHandlerTests
    {
        [TestMethod]
        public async Task Serialize_And_Read_ReturnSamePacket()
        {
            var handler = new PacketHandler();

            var packet = new Packet
            {
                PacketType = 1,
                CommandId = 2,
                SequenceNumber = 3,
                Payload = new byte[] { 1, 2, 3 }
            };

            byte[] data = handler.Serialize(packet);

            using var stream = new MemoryStream(data);

            var result = await handler.ReadPacketAsync(stream, CancellationToken.None);

            Assert.AreEqual(packet.CommandId, result.CommandId);
            CollectionAssert.AreEqual(packet.Payload, result.Payload);
        }
    }

    [TestClass]
    public class Server_ImageTransferTests
    {
        [TestMethod]
        public async Task SendImageAsync_FileExists_SendsChunks()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ServerTestImages");
            Directory.CreateDirectory(dir);

            string filePath = Path.Combine(dir, "test.jpg");
            File.WriteAllBytes(filePath, new byte[2048]);

            var config = new ServerConfig { ImageDirectory = dir, ChunkSize = 512 };
            var logger = new Logger(dir);
            var manager = new ImageTransferManager(config, logger);
            var handler = new PacketHandler();

            using var stream = new MemoryStream();

            int chunks = await manager.SendImageAsync("test.jpg", stream, handler, CancellationToken.None);

            Assert.IsTrue(chunks > 0);
            Assert.IsTrue(stream.Length > 0);
        }
    }

     [TestClass]
    public class Server_ManagerTests
    {
        [TestMethod]
        public void StartStop_DoesNotThrow()
        {
            var config = new ServerConfig { Port = 5050 };
            var logger = new Logger();

            var server = new ServerManager(
                config,
                logger,
                _ => { },
                _ => { },
                _ => { }
            );

            server.StartAsync();
            server.Stop();

            Assert.IsTrue(true); // if no crash, pass
        }
    }
}