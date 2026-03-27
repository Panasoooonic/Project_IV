using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ImageClient.Managers;
using ImageClient.Utils;


namespace ImageClient.FrameworkApp;


public partial class MainWindow : Window
{
    private PacketHandler packetHandler = new PacketHandler();
    private ImageManager imageManager = new ImageManager();
    private Logger logger = new Logger();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        string user = UsernameBox.Text;
        string pass = PasswordBox.Password;

        // Simulate success
        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
        {
            LoginStatus.Text = "Login successful";
            logger.Log("Login successful");
            LoginPanel.Visibility = Visibility.Collapsed;
            MainPanel.Visibility = Visibility.Visible;
        }
        else
        {
            LoginStatus.Text = "Invalid login";
            logger.Log("Invalid login");
        }
    }

    

    private async void RequestImage_Click(object sender, RoutedEventArgs e)
    {
        ProgressBar.Value = 0;
        RequestButton.IsEnabled = false;
        StatusText.Text = "Downloading image...";
        
        logger.Log("Image request triggered");

        try
        {
            // Simulate progress (replace later with real packets)
            for (int i = 0; i <= 100; i += 10)
            {
                await Task.Delay(200);
                ProgressBar.Value = i;
            }

            imageManager.Clear();
            imageManager.AddChunk(1, new byte[] { 1, 2, 3 });

            string savedPath = imageManager.SaveImage();
            SavePathBox.Text = savedPath;

            StatusText.Text = "Download complete!";
            logger.Log($"Image saved to: {savedPath}");
        }
        catch (Exception ex)
        {
            StatusText.Text = "Download failed.";
            logger.LogError($"Image request failed: {ex.Message}");
            MessageBox.Show(
                $"Image download failed:\n{ex.Message}",
                "Image Client",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            RequestButton.IsEnabled = true;
        }

        
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        MainPanel.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Visible;

        StatusText.Text = "Ready";
        ProgressBar.Value = 0;
    }

}
