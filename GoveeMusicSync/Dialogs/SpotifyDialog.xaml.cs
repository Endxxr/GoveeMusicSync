using System.Windows;

namespace GoveeMusicSync.Dialogs
{
    
    public partial class SpotifyDialog : Window
    {
        public string ClientSecret { get; private set; }
        public string ClientId { get; private set; }


        public SpotifyDialog() => InitializeComponent();


        private void Save_Button_Click(object sender, RoutedEventArgs args)
        {
            ClientSecret = Client_Secret_Box.Password;
            ClientId = Client_ID_Box.Password;
            DialogResult = true;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs args)
        {
            DialogResult = false;
        }



    }
}
