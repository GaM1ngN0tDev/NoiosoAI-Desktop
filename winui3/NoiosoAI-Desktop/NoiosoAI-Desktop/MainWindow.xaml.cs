using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NoiosoAI_Desktop
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            AppState.Load();   // restore saved chat before the chat page is created
            Nav.SelectedItem = Nav.MenuItems[0];
        }

        private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
                ContentFrame.Navigate(typeof(SettingsPage));
            else
                ContentFrame.Navigate(typeof(ChatPage));
        }
    }
}
