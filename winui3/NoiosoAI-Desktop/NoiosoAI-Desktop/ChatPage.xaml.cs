using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
using Windows.UI.Core;

namespace NoiosoAI_Desktop
{
    public sealed partial class ChatPage : Page
    {
        // How often the screen is refreshed while a reply streams in.
        private const int UiRefreshMs = 60;

        private readonly OllamaService _service = new();
        private CancellationTokenSource? _cts;
        private bool _busy;
        private ObservableCollection<ChatMessage>? _attached;

        public ChatPage()
        {
            InitializeComponent();

            PersonalityBox.ItemsSource = AppState.PersonalityNames.ToList();
            PersonalityBox.SelectedItem = AppState.SelectedPersonality;

            AppState.CurrentChanged += () => DispatcherQueue.TryEnqueue(AttachConversation);
            AppState.ConversationsChanged += () => DispatcherQueue.TryEnqueue(UpdateTitle);

            AttachConversation();

            // Soft fade-in whenever these controls become visible.
            SetupFadeIn(BusyBar);
            SetupFadeIn(StopButton);
            SetupFadeIn(SendButton);
        }

        private static void SetupFadeIn(UIElement element)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(element).Compositor;
            var show = compositor.CreateScalarKeyFrameAnimation();
            show.Target = "Opacity";
            show.InsertKeyFrame(0f, 0f);
            show.InsertKeyFrame(1f, 1f);
            show.Duration = TimeSpan.FromMilliseconds(220);
            ElementCompositionPreview.SetImplicitShowAnimation(element, show);
        }

        // Show the messages of whichever chat is currently selected.
        private void AttachConversation()
        {
            if (_attached != null) _attached.CollectionChanged -= OnMessagesChanged;

            _attached = AppState.Current.Messages;
            _attached.CollectionChanged += OnMessagesChanged;
            MessagesList.ItemsSource = _attached;

            UpdateEmptyState();
            UpdateTitle();

            DispatcherQueue.TryEnqueue(() =>
            {
                if (_attached.Count > 0) MessagesList.ScrollIntoView(_attached.Last());
            });
        }

        private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateEmptyState();

        private void UpdateTitle() => TitleText.Text = AppState.Current.Title;

        private void UpdateEmptyState() =>
            EmptyText.Visibility = AppState.Current.Messages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

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

        // ---- Animation: new bubbles fade and slide in once ----
        private void Bubble_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;
            if (fe.DataContext is not ChatMessage { IsFresh: true } msg) return;

            msg.IsFresh = false;
            AnimateIn(fe);
        }

        private static void AnimateIn(FrameworkElement element)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;
            ElementCompositionPreview.SetIsTranslationEnabled(element, true);

            var ease = compositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 0.9f), new Vector2(0.3f, 1f));
            var duration = TimeSpan.FromMilliseconds(280);

            var fade = compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(0f, 0f);
            fade.InsertKeyFrame(1f, 1f, ease);
            fade.Duration = duration;

            var slide = compositor.CreateScalarKeyFrameAnimation();
            slide.InsertKeyFrame(0f, 14f);
            slide.InsertKeyFrame(1f, 0f, ease);
            slide.Duration = duration;

            visual.StartAnimation("Opacity", fade);
            visual.StartAnimation("Translation.Y", slide);
        }

        // ---- Controls ----
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

            var conv = AppState.Current;
            conv.Messages.Clear();
            conv.Title = Conversation.DefaultTitle;

            AppState.NotifyConversationsChanged();
            AppState.Save();
        }

        private void Stop_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

        private async void Send_Click(object sender, RoutedEventArgs e) => await SendAsync();

        private async void InputBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter) return;

            var shift = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
                                           .HasFlag(CoreVirtualKeyStates.Down);
            if (shift) return;   // Shift+Enter: let the TextBox insert a new line

            e.Handled = true;
            await SendAsync();
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            SendButton.Visibility = busy ? Visibility.Collapsed : Visibility.Visible;
            StopButton.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            BusyBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        }

        private static string MakeTitle(string text)
        {
            var line = text.Split('\n')[0].Trim();
            return line.Length <= 28 ? line : line.Substring(0, 28).TrimEnd() + "…";
        }

        private void ScrollTo(Conversation conv, ChatMessage message)
        {
            // The reply may belong to a chat the user has switched away from.
            if (AppState.Current == conv) MessagesList.ScrollIntoView(message);
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
            var conv = AppState.Current;

            SetBusy(true);
            InputBox.Text = "";

            // The first message names the chat.
            if (conv.Title == Conversation.DefaultTitle)
            {
                conv.Title = MakeTitle(text);
                AppState.NotifyConversationsChanged();
            }

            var history = conv.Messages.ToList();
            var userMsg = new ChatMessage("user", text) { IsFresh = true };
            conv.Messages.Add(userMsg);
            history.Add(userMsg);

            var reply = new ChatMessage("assistant", "", isPending: true) { IsStreaming = true };
            conv.Messages.Add(reply);
            ScrollTo(conv, reply);

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var sb = new StringBuilder();

            // Only touch the UI every UiRefreshMs so it doesn't flicker.
            var clock = Stopwatch.StartNew();
            long lastRefresh = 0;

            try
            {
                var systemPrompt = AppState.GetPrompt(AppState.SelectedPersonality);
                await foreach (var chunk in _service.ChatStreamAsync(model, history, systemPrompt, token))
                {
                    sb.Append(chunk);

                    if (clock.ElapsedMilliseconds - lastRefresh >= UiRefreshMs)
                    {
                        lastRefresh = clock.ElapsedMilliseconds;
                        reply.IsPending = false;
                        reply.Content = sb.ToString();
                        ScrollTo(conv, reply);
                    }
                }

                // Final update with the complete text.
                reply.IsPending = false;
                reply.Content = sb.Length == 0 ? "(no response)" : sb.ToString();
                ScrollTo(conv, reply);
            }
            catch (OperationCanceledException)
            {
                // User pressed Stop (or Clear): keep whatever has arrived so far.
                reply.IsPending = false;
                reply.Content = sb.Length == 0 ? "[Stopped]" : sb.ToString();
            }
            catch (Exception)
            {
                reply.IsPending = false;
                reply.Content = sb.Length > 0
                    ? sb.ToString() + "\n\n[Connection lost]"
                    : "Error: Unable to connect to Ollama. Please check your server status.";
            }
            finally
            {
                reply.IsStreaming = false;
                _cts?.Dispose();
                _cts = null;
                SetBusy(false);
                AppState.Save();
                InputBox.Focus(FocusState.Programmatic);
            }
        }
    }
}
