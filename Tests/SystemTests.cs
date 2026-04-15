using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FrameworkApp.Managers;
using FrameworkApp.Utils;
using ImageServer.Managers;
using ImageServer.Models;
using ImageServer.Utils;

namespace Tests.SystemTests
{
    [TestClass]
    public class FullWorkflowTests
    {
        [TestMethod]
        public async Task FullSystem_Login_And_ImageTransfer_Works()
        {
            // --- SETUP ---
            int port = 5055;

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            
            // Create test image
            string imagePath = Path.Combine(tempDir, "test.jpg");
            File.WriteAllBytes(imagePath, new byte[5000]);

            var serverConfig = new ServerConfig
            {
                Port = port,
                ImageDirectory = tempDir,
                ChunkSize = 1024,
                LogDirectory = tempDir
            };

            var serverLogger = new ImageServer.Utils.Logger(tempDir);

            var server = new ServerManager(
                serverConfig,
                serverLogger,
                _ => { },
                _ => { },
                _ => { }
            );

            server.StartAsync();

            await Task.Delay(500); // allow server to start

            // --- CLIENT SETUP ---
            var clientLogger = new FrameworkApp.Utils.Logger(tempDir);
            var client = new NetworkManager("127.0.0.1", port, clientLogger);
            var imageManager = new ImageManager(tempDir);

            // --- CONNECT ---
            await client.ConnectAsync(CancellationToken.None);

            // --- LOGIN ---
            var loginResult = await client.LoginAsync("admin", "password123", CancellationToken.None);
            Assert.IsTrue(loginResult.Success);

            // --- REQUEST IMAGE ---
            var result = await client.RequestImageAsync(
                "test.jpg",
                imageManager,
                null,
                CancellationToken.None
            );

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.SavedPath);
            Assert.IsTrue(File.Exists(result.SavedPath));

            // --- VERIFY FILE ---
            var fileInfo = new FileInfo(result.SavedPath);
            Assert.IsTrue(fileInfo.Length > 0);

            // --- CLEANUP ---
            client.Disconnect();
            server.Stop();
        }

        [TestMethod]
        public async Task System_InvalidLogin_Fails()
        {
            int port = 5056;

            var config = new ServerConfig { Port = port };
            var server = new ServerManager(config, new ImageServer.Utils.Logger(), _ => { }, _ => { }, _ => { });

            server.StartAsync();
            await Task.Delay(500);

            var client = new NetworkManager("127.0.0.1", port, new FrameworkApp.Utils.Logger());

            await client.ConnectAsync(CancellationToken.None);

            var result = await client.LoginAsync("admin", "wrong", CancellationToken.None);

            Assert.IsFalse(result.Success);

            client.Disconnect();
            server.Stop();
        }

        [TestMethod]
        public async Task System_RequestImage_WithoutLogin_Fails()
        {
            int port = 5057;

            var config = new ServerConfig { Port = port };
            var server = new ServerManager(config, new ImageServer.Utils.Logger(), _ => { }, _ => { }, _ => { });

            server.StartAsync();
            await Task.Delay(500);

            var client = new NetworkManager("127.0.0.1", port, new FrameworkApp.Utils.Logger());
            var imageManager = new ImageManager();

            await client.ConnectAsync(CancellationToken.None);

            var result = await client.RequestImageAsync("test.jpg", imageManager, null, CancellationToken.None);

            Assert.IsFalse(result.Success);

            client.Disconnect();
            server.Stop();
        }
    }
}