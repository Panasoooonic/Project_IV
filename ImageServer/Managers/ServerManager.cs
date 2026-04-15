using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ImageServer.Models;
using ImageServer.Utils;

namespace ImageServer.Managers
{
    /// <summary>
/// Manages the server lifecycle.
/// </summary>
/// <remarks>
/// Responsible for starting, stopping, and accepting client connections.
/// </remarks>
    public class ServerManager
    {
        private readonly ServerConfig _config;
        private readonly Logger _logger;
        private readonly Action<string> _uiLog;
        private readonly Action<int> _clientCountUpdater;
        private readonly Action<bool> _serverStatusUpdater;

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private int _connectedClients;
        private bool _isRunning;

        public ServerManager(
            ServerConfig config,
            Logger logger,
            Action<string> uiLog,
            Action<int> clientCountUpdater,
            Action<bool> serverStatusUpdater)
        {
            _config = config;
            _logger = logger;
            _uiLog = uiLog;
            _clientCountUpdater = clientCountUpdater;
            _serverStatusUpdater = serverStatusUpdater;
        }

        public Task StartAsync()
        {
            if (_isRunning)
            {
                _uiLog("Server is already running.");
                return Task.CompletedTask;
            }

            Directory.CreateDirectory(_config.ImageDirectory);
            Directory.CreateDirectory(_config.LogDirectory);

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _config.Port);
            _listener.Start();

            _isRunning = true;
            _serverStatusUpdater(true);

            _logger.Log($"SERVER STARTED | Port={_config.Port}");
            _uiLog($"Server started on port {_config.Port}. Listening for client connections...");

            _ = AcceptClientsLoopAsync(_cts.Token);

            return Task.CompletedTask;
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _cts?.Cancel();

            try
            {
                _listener?.Stop();
            }
            catch
            {
            }

            _isRunning = false;
            _connectedClients = 0;

            _clientCountUpdater(_connectedClients);
            _serverStatusUpdater(false);

            _logger.Log("SERVER STOPPED");
            _uiLog("Server stopped.");
        }

        private async Task AcceptClientsLoopAsync(CancellationToken cancellationToken)
        {
            if (_listener == null)
            {
                return;
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);

                    int count = Interlocked.Increment(ref _connectedClients);
                    _clientCountUpdater(count);

                    ClientSession session = new ClientSession(
                        client,
                        _config,
                        _logger,
                        _uiLog,
                        OnSessionClosed);

                    _ = Task.Run(() => session.RunAsync(cancellationToken), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _uiLog("Accept loop cancelled.");
            }
            catch (ObjectDisposedException)
            {
                _uiLog("Listener disposed.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Server accept loop error: {ex.Message}");
                _uiLog($"Server accept loop error: {ex.Message}");
            }
        }

        private void OnSessionClosed()
        {
            int count = Interlocked.Decrement(ref _connectedClients);
            if (count < 0)
            {
                _connectedClients = 0;
                count = 0;
            }

            _clientCountUpdater(count);
        }
    }
}