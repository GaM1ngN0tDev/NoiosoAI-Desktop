using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace NoiosoAI_Desktop
{
    public sealed partial class ChatPage : Page
    {
        private readonly OllamaService _service = new();
        private CancellationTokenSource? _cts;
        private bool _busy;

        public ChatPage()
        {
            InitializeComponent();

            MessagesList.ItemsSource = AppState.Messages;
            PersonalityBox.ItemsSource = AppState.Personalities.Keys.ToList();
            PersonalityBox.SelectedItem = AppState.SelectedPersonality;

            AppState.Messages.CollectionChanged += (_, _) => UpdateEmptyState();
            UpdateEmptyState();

            // Jump to the newest message when the app starts with saved history.
            Loaded += (_, _) =>
            {
                if (AppState.Messages.Count > 0)
                    MessagesList.ScrollIntoView(AppState.Messages.Last());
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Reload models every time we come back (e.g. after changing server settings).
            var models = await _service.GetModelsAsync();
            ModelBox.ItemsSource = models;

            if (models.Count > 0)
            {
                Status.IsOpen = false;
                if (AppState.SelectedModel == null || !models.Contains(AppState.SelectedModel))
                    AppState.SelectedModel = models[0];
                ModelBox.SelectedItem = AppState.SelectedModel;
            }
            else
            {
                Status.Message = "Could not reach Ollama. Check that it is running and the server settings are correct.";
                Status.IsOpen = true;
            }
        }

        private void UpdateEmptyState() =>
            EmptyText.Visibility = AppState.Messages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        private void ModelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelBox.SelectedItem is string s && s != AppState.SelectedModel)
            {
                AppState.SelectedModel = s;
                AppState.Save();
            }
        }

        private void PersonalityBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PersonalityBox.SelectedItem is string s && s != AppState.SelectedPersonality)
            {
                AppState.SelectedPersonality = s;
                AppState.Save();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            AppState.Messages.Clear();
            AppState.Save();
        }

        private void Stop_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

        private async void Send_Click(object sender, RoutedEventArgs e) => await SendAsync();

        private async void InputBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                await SendAsync();
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            SendButton.Visibility = busy ? Visibility.Collapsed : Visibility.Visible;
            StopButton.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task SendAsync()
        {
            var text = InputBox.Text.Trim();
            if (_busy || text.Length == 0) return;

            if (string.IsNullOrEmpty(AppState.SelectedModel))
            {
                Status.Message = "No model selected. Check the server settings.";
                Status.IsOpen = true;
                return;
            }

            var model = AppState.SelectedModel!;
            SetBusy(true);
            InputBox.Text = "";

            var history = AppState.Messages.ToList();
            var userMsg = new ChatMessage("user", text);
            AppState.Messages.Add(userMsg);
            history.Add(userMsg);

            var reply = new ChatMessage("assistant", "…");
            AppState.Messages.Add(reply);
            MessagesList.ScrollIntoView(reply);

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var sb = new StringBuilder();

            try
            {
                var systemPrompt = AppState.Personalities[AppState.SelectedPersonality];
                await foreach (var chunk in _service.ChatStreamAsync(model, history, systemPrompt, token))
                {
                    sb.Append(chunk);
                    reply.Content = sb.ToString();
                    MessagesList.ScrollIntoView(reply);
                }

                if (sb.Length == 0) reply.Content = "(no response)";
            }
            catch (OperationCanceledException)
            {
                // User pressed Stop (or Clear): keep whatever has arrived so far.
                if (sb.Length == 0) reply.Content = "[Stopped]";
            }
            catch (Exception)
            {
                reply.Content = sb.Length > 0
                    ? sb.ToString() + "\n\n[Connection lost]"
                    : "Error: Unable to connect to Ollama. Please check your server status.";
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                SetBusy(false);
                AppState.Save();
                InputBox.Focus(FocusState.Programmatic);
            }
        }
    }
}
