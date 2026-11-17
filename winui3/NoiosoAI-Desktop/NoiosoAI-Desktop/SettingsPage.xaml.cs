using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NoiosoAI_Desktop
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            IpBox.Text = OllamaService.Ip;
            PortBox.Text = OllamaService.Port;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            OllamaService.Ip = IpBox.Text;
            OllamaService.Port = PortBox.Text;
            SavedBar.IsOpen = true;
        }
    }
}
