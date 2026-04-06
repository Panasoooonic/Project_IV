using System;
using System.Windows;
using ImageServer.Managers;
using ImageServer.Utils;

namespace ImageServer;

public partial class MainWindow : Window
{
    private readonly Logger _logger;
    private readonly ServerManager _serverManager;

    public MainWindow()
    {
        InitializeComponent();

        _logger = new Logger();
        _serverManager = new ServerManager(
            5000,
            _logger,
            AppendLog,
            UpdateClientCount,
            UpdateServerStatus);

        PortText.Text = "5000";
        AppendLog("Server UI initialized.");
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