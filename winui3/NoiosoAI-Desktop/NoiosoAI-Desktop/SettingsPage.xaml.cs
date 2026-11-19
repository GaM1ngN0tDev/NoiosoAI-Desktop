using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace NoiosoAI_Desktop
{
    public sealed partial class SettingsPage : Page
    {
        private readonly bool _loading = true;

        public SettingsPage()
        {
            InitializeComponent();

            ThemeBox.SelectedIndex = AppState.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };

            IpBox.Text = OllamaService.Ip;
            PortBox.Text = OllamaService.Port;

            PersonaBox.ItemsSource = AppState.PersonalityNames.ToList();
            PersonaBox.SelectedItem = AppState.SelectedPersonality;
            PromptBox.Text = AppState.GetPrompt(AppState.SelectedPersonality);

            _loading = false;
        }

        private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;

            AppState.Theme = ThemeBox.SelectedIndex switch { 1 => "Light", 2 => "Dark", _ => "System" };
            AppState.Save();
            MainWindow.Instance?.ApplyTheme(AppState.Theme);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            OllamaService.Ip = IpBox.Text;
            OllamaService.Port = PortBox.Text;
            SavedBar.IsOpen = true;
        }

        private void PersonaBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (PersonaBox.SelectedItem is string name)
            {
                PromptBox.Text = AppState.GetPrompt(name);
                PromptBar.IsOpen = false;
            }
        }

        private void SavePrompt_Click(object sender, RoutedEventArgs e)
        {
            if (PersonaBox.SelectedItem is not string name) return;
            AppState.SetPrompt(name, PromptBox.Text);
            PromptBar.IsOpen = true;
        }

        private void ResetPrompt_Click(object sender, RoutedEventArgs e)
        {
            if (PersonaBox.SelectedItem is not string name) return;
            AppState.SetPrompt(name, AppState.DefaultPrompts[name]);
            PromptBox.Text = AppState.DefaultPrompts[name];
            PromptBar.IsOpen = true;
        }
    }
}
