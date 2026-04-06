using System;
using System.Windows;
using ImageServer.Managers;
using ImageServer.Models;
using ImageServer.Utils;

namespace ImageServer
{
    public partial class MainWindow : Window
    {
        private readonly Logger _logger;
        private readonly ServerManager _serverManager;
        private readonly ServerConfig _config;

        public MainWindow()
        {
            InitializeComponent();

            _config = new ServerConfig();
            _logger = new Logger(_config.LogDirectory);

            _serverManager = new ServerManager(
                _config,
                _logger,
                AppendLog,
                UpdateClientCount,
                UpdateServerStatus);

            PortText.Text = _config.Port.ToString();
            DefaultImageText.Text = _config.DefaultImageFileName;

            AppendLog("Server UI initialized.");
            AppendLog($"Configured port: {_config.Port}");
            AppendLog($"Image directory: {_config.ImageDirectory}");
            AppendLog($"Default image: {_config.DefaultImageFileName}");
        }

        private async void StartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartServerButton.IsEnabled = false;
                StopServerButton.IsEnabled = true;

                await _serverManager.StartAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to start server: {ex.Message}");
                UpdateServerStatus(false);
                StartServerButton.IsEnabled = true;
                StopServerButton.IsEnabled = false;
            }
        }

        private void StopServer_Click(object sender, RoutedEventArgs e)
        {
            _serverManager.Stop();

            StartServerButton.IsEnabled = true;
            StopServerButton.IsEnabled = false;
        }

        private void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogOutputTextBox.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                LogOutputTextBox.ScrollToEnd();
            });
        }

        private void UpdateClientCount(int count)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectedClientsText.Text = count.ToString();
            });
        }

        private void UpdateServerStatus(bool isRunning)
        {
            Dispatcher.Invoke(() =>
            {
                ServerStatusText.Text = isRunning ? "Running" : "Stopped";
                ServerStatusText.Foreground = isRunning
                    ? System.Windows.Media.Brushes.ForestGreen
                    : System.Windows.Media.Brushes.Firebrick;
            });
        }
    }
}