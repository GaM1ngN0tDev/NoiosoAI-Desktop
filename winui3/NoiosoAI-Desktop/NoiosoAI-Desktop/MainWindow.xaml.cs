using System;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NoiosoAI_Desktop
{
    public sealed partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }

        private bool _syncing;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            AppState.Load();
            ApplyTheme(AppState.Theme);

            AppState.ConversationsChanged += () => DispatcherQueue.TryEnqueue(RebuildMenu);

            RebuildMenu();
            ContentFrame.Navigate(typeof(ChatPage));
        }

        // Called from the settings page too.
        public void ApplyTheme(string theme)
        {
            var t = theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            if (Content is FrameworkElement root) root.RequestedTheme = t;

            // Keep the minimize/maximize/close buttons readable.
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var bar = AppWindow.TitleBar;
                if (t == ElementTheme.Dark) bar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                else if (t == ElementTheme.Light) bar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                else bar.ButtonForegroundColor = null;
            }
        }

        private void RebuildMenu()
        {
            _syncing = true;
            try
            {
                Nav.MenuItems.Clear();

                Nav.MenuItems.Add(new NavigationViewItem
                {
                    Content = "New chat",
                    Tag = "new",
                    Icon = new SymbolIcon(Symbol.Add)
                });
                Nav.MenuItems.Add(new NavigationViewItemHeader { Content = "Chats" });

                NavigationViewItem? selected = null;
                foreach (var c in AppState.Conversations)
                {
                    var item = new NavigationViewItem
                    {
                        Content = c.Title,
                        Tag = c,
                        Icon = new SymbolIcon(Symbol.Message),
                        ContextFlyout = BuildFlyout(c)
                    };
                    Nav.MenuItems.Add(item);
                    if (c == AppState.Current) selected = item;
                }

                // Don't steal the selection from the Settings page.
                if (ContentFrame.Content is not SettingsPage && selected != null)
                    Nav.SelectedItem = selected;
            }
            finally
            {
                _syncing = false;
            }
        }

        private MenuFlyout BuildFlyout(Conversation c)
        {
            var flyout = new MenuFlyout();

            var rename = new MenuFlyoutItem { Text = "Rename", Icon = new SymbolIcon(Symbol.Rename) };
            rename.Click += async (_, _) => await RenameAsync(c);

            var delete = new MenuFlyoutItem { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) };
            delete.Click += (_, _) => AppState.DeleteConversation(c);

            flyout.Items.Add(rename);
            flyout.Items.Add(delete);
            return flyout;
        }

        private async Task RenameAsync(Conversation c)
        {
            var box = new TextBox { Text = c.Title };
            var dialog = new ContentDialog
            {
                Title = "Rename chat",
                Content = box,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                AppState.RenameConversation(c, box.Text);
        }

        private void ShowChat()
        {
            if (ContentFrame.Content is not ChatPage)
                ContentFrame.Navigate(typeof(ChatPage));
        }

        private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (_syncing) return;

            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage), null, args.RecommendedNavigationTransitionInfo);
                return;
            }

            if (args.SelectedItem is not NavigationViewItem item) return;

            if (item.Tag is string tag && tag == "new")
            {
                AppState.NewConversation();
                DispatcherQueue.TryEnqueue(RebuildMenu);   // moves the highlight to the current chat
            }
            else if (item.Tag is Conversation c)
            {
                AppState.SelectConversation(c);
            }

            ShowChat();
        }
    }
}
