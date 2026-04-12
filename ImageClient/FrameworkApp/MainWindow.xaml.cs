using System;
using System.IO;
using System.Threading;
using System.Windows;
using FrameworkApp.Managers;
using FrameworkApp.Utils;

namespace FrameworkApp
{
    public partial class MainWindow : Window
    {
        private readonly Logger _logger;
        private readonly NetworkManager _networkManager;
        private readonly ImageManager _imageManager;

        public MainWindow()
        {
            InitializeComponent();

            string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
            string logDir = Path.Combine(projectRoot, "Logs");
            string imageHolderDir = Path.Combine(projectRoot, "ImageHolder");

            _logger = new Logger(logDir);
            _networkManager = new NetworkManager("127.0.0.1", 5000, _logger);
            _imageManager = new ImageManager(imageHolderDir);

            PasswordBox.Password = "password123";
            ImageFileTextBox.Text = "image_20260406.jpeg";

            AppendLog("Client initialized.");
            AppendLog($"Client log directory: {logDir}");
            AppendLog($"Image output directory: {imageHolderDir}");
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusTextBlock.Text = "Connecting to server...";
                AppendLog("Connecting to server...");

                await _networkManager.ConnectAsync(CancellationToken.None);

                var result = await _networkManager.LoginAsync(
                    UsernameTextBox.Text.Trim(),
                    PasswordBox.Password,
                    CancellationToken.None);

                StatusTextBlock.Text = result.Message;
                AppendLog(result.Message);

                if (result.Success)
                {
                    RequestImageButton.IsEnabled = true;
                    LoginButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Login failed: {ex.Message}";
                AppendLog($"Login failed: {ex.Message}");
            }
        }

        private async void RequestImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RequestImageButton.IsEnabled = false;
                TransferProgressBar.Value = 0;
                StatusTextBlock.Text = "Requesting image...";

                Progress<int> progress = new Progress<int>(value =>
                {
                    TransferProgressBar.Value = value;
                });

                var result = await _networkManager.RequestImageAsync(
                    ImageFileTextBox.Text.Trim(),
                    _imageManager,
                    progress,
                    CancellationToken.None);

                StatusTextBlock.Text = result.Message;
                AppendLog(result.Message);

                if (result.Success && result.SavedPath != null)
                {
                    AppendLog($"Saved image to: {result.SavedPath}");
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Image request failed: {ex.Message}";
                AppendLog($"Image request failed: {ex.Message}");
            }
            finally
            {
                RequestImageButton.IsEnabled = true;
            }
        }

        private void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                LogTextBox.AppendText(line + Environment.NewLine);
                LogTextBox.ScrollToEnd();
            });
        }

        protected override async void OnClosed(EventArgs e)
        {
            try
            {
                if (_networkManager.IsConnected)
                {
                    await _networkManager.LogoutAsync(CancellationToken.None);
                }
            }
            catch
            {
            }

            _networkManager.Disconnect();
            base.OnClosed(e);
        }
    }
}