using dotenv.net;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace GoveeMusicSync
{
    /// <summary>
    /// Logica di interazione per ControlWindow.xaml
    /// </summary>
    public partial class ControlWindow : Window
    {

        private readonly AppManagers appManagers;

        public ControlWindow(AppManagers appManagers)
        {
            this.appManagers = appManagers;
            appManagers.VideoSyncManager = new Managers.VideoSyncManager();
            appManagers.AudioSyncManager = new Managers.AudioSyncManager(this, appManagers.BluetoothConnectionManager);

            InitializeComponent();
        }

        private void Toggle_Power_Click(object sender, RoutedEventArgs e)
        {
            appManagers.BluetoothConnectionManager.TogglePower();
        }

        private void BrightnessSlider_ValueChanged(object sender, DragCompletedEventArgs e)
        {
            appManagers.BluetoothConnectionManager.SetBrightness((byte)Brightness_Slider.Value);
        }

        private void Change_Color_Click(object sender, RoutedEventArgs e)
        {
            appManagers.BluetoothConnectionManager.SwitchColor();
        }

        private void Video_Sync_Click(object sender, RoutedEventArgs e)
        {
            appManagers.VideoSyncManager.ToggleSync(appManagers.BluetoothConnectionManager);
            if (appManagers.VideoSyncManager.Loop)
            {
                Power_Toggle.IsEnabled = false;
                Brightness_Slider.IsEnabled = false;
                Switch_Color.IsEnabled = false;
                Spotify_Sync.IsEnabled = false;
                Video_Sync.Content = "Stop Video Sync";
            }
            else
            {
                Video_Sync.Content = "Video Sync";
                Power_Toggle.IsEnabled = true;
                Brightness_Slider.IsEnabled = true;
                Switch_Color.IsEnabled = true;
                Spotify_Sync.IsEnabled = true;
            }
        }

        private void Spotify_Sync_Click(object sender, RoutedEventArgs e)
        {
            appManagers.AudioSyncManager.ToggleSync();
            if (appManagers.AudioSyncManager.Loop)
            {
                Power_Toggle.IsEnabled = false;
                Brightness_Slider.IsEnabled = false;
                Video_Sync.IsEnabled = false;
                Switch_Color.IsEnabled = false;
                Threshold_Slider.Visibility = Visibility.Visible;
                Smoothing_Slider.Visibility = Visibility.Visible;
                Smoothing_Label.Visibility = Visibility.Visible;
                Threshold_Label.Visibility = Visibility.Visible;
                Spotify_Sync.Content = "Stop Spotify Sync";
            }
            else
            {
                Spotify_Sync.Content = "Spotify Sync";
                Power_Toggle.IsEnabled = true;
                Brightness_Slider.IsEnabled = true;
                Video_Sync.IsEnabled = true;
                Switch_Color.IsEnabled = true;
                Threshold_Slider.Visibility = Visibility.Hidden;
                Smoothing_Slider.Visibility = Visibility.Hidden;
                Smoothing_Label.Visibility = Visibility.Hidden;
                Threshold_Label.Visibility = Visibility.Hidden;
            }
        }

        private void Threshold_ValueChanged(object sender, DragCompletedEventArgs e)
        {
            appManagers.AudioSyncManager.Threshold = (float) Threshold_Slider.Value/100;
        }

        private void Smoothing_Slider_ValueChanged(object sender, DragCompletedEventArgs e)
        {
            appManagers.AudioSyncManager.SmoothingFactor = (float)Smoothing_Slider.Value;
        }
    }
}
