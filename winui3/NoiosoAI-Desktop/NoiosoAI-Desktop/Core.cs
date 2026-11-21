using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace NoiosoAI_Desktop
{
    // One chat bubble. Content is observable so the UI updates while streaming.
    public class ChatMessage : INotifyPropertyChanged
    {
        private string _content;
        private bool _isPending;

        public ChatMessage(string role, string content, bool isPending = false)
        {
            Role = role;
            _content = content;
            _isPending = isPending;
        }

        public string Role { get; }
        public bool IsUser => Role == "user";

        // True for messages created live, so they play the fade-in animation once.
        public bool IsFresh { get; set; }

        public string Content
        {
            get => _content;
            set
            {
                if (_content == value) return;
                _content = value;
                Raise(nameof(Content));
            }
        }

        // True while waiting for the first word of a reply (shows a spinner).
        public bool IsPending
        {
            get => _isPending;
            set
            {
                if (_isPending == value) return;
                _isPending = value;
                Raise(nameof(IsPending));
                Raise(nameof(HasContent));
            }
        }

        // True while the reply is still arriving (shows a cursor at the end of the text).
        private bool _isStreaming;
        public bool IsStreaming
        {
            get => _isStreaming;
            set
            {
                if (_isStreaming == value) return;
                _isStreaming = value;
                Raise(nameof(IsStreaming));
            }
        }

        public bool HasContent => !_isPending;

        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class Conversation
    {
        public const string DefaultTitle = "New chat";

        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = DefaultTitle;
        public ObservableCollection<ChatMessage> Messages { get; } = new();
    }

    // ---- Shapes of the files on disk ----
    public class SavedMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    public class SavedConversation
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = Conversation.DefaultTitle;
        public List<SavedMessage> Messages { get; set; } = new();
    }

    public class SavedState
    {
        public string? Model { get; set; }
        public string Personality { get; set; } = "Default";
        public string Theme { get; set; } = "System";
        public string? CurrentId { get; set; }
        public Dictionary<string, string> PromptOverrides { get; set; } = new();
        public List<SavedConversation> Conversations { get; set; } = new();
    }

    // Old single-chat format (chat.json), only used to migrate.
    public class SavedChat
    {
        public string? Model { get; set; }
        public string Personality { get; set; } = "Default";
        public List<SavedMessage> Messages { get; set; } = new();
    }

    // State that survives page navigation and app restarts.
    public static class AppState
    {
        public static ObservableCollection<Conversation> Conversations { get; } = new();
        public static Conversation Current { get; private set; } = new();

        public static string? SelectedModel { get; set; }
        public static string SelectedPersonality { get; set; } = "Default";
        public static string Theme { get; set; } = "System";   // System | Light | Dark

        public static Dictionary<string, string> PromptOverrides { get; } = new();

        public static readonly Dictionary<string, string> DefaultPrompts = new()
        {
            ["Default"] = "You are NoiosoAI, a helpful, polite, and precise AI assistant.",
            ["Engineer"] = "You are a Senior Software Engineer. Give highly technical, code-focused, concise, and optimized answers. Avoid unnecessary small talk.",
            ["Friendly"] = "You are a super friendly, empathetic, and warm assistant. Use an informal tone, support the user, and show enthusiasm using matching emojis.",
            ["Wise"] = "You are a wise philosopher. Respond with deep, well-thought-out reflections, calmly analyzing pros and cons with poetic nuance.",
            ["Sarcastic"] = "You are NoiosoAI, but today you are extremely sarcastic, witty, and slightly lazy. Tease the user playfully while still providing the actual solution.",
        };

        public static IEnumerable<string> PersonalityNames => DefaultPrompts.Keys;

        public static event Action? CurrentChanged;
        public static event Action? ConversationsChanged;

        public static void NotifyConversationsChanged() => ConversationsChanged?.Invoke();

        // ---- Prompts ----
        public static string GetPrompt(string name)
        {
            if (PromptOverrides.TryGetValue(name, out var custom) && !string.IsNullOrWhiteSpace(custom))
                return custom;
            return DefaultPrompts.TryGetValue(name, out var def) ? def : "";
        }

        public static void SetPrompt(string name, string text)
        {
            if (!DefaultPrompts.ContainsKey(name)) return;
            if (string.IsNullOrWhiteSpace(text) || text == DefaultPrompts[name])
                PromptOverrides.Remove(name);
            else
                PromptOverrides[name] = text;
            Save();
        }

        // ---- Conversations ----
        public static Conversation NewConversation()
        {
            // Reuse the current chat if it is still empty.
            if (Current.Messages.Count == 0 && Conversations.Contains(Current))
                return Current;

            var c = new Conversation();
            Conversations.Insert(0, c);
            Current = c;
            ConversationsChanged?.Invoke();
            CurrentChanged?.Invoke();
            Save();
            return c;
        }

        public static void SelectConversation(Conversation c)
        {
            if (Current == c) return;
            Current = c;
            CurrentChanged?.Invoke();
            Save();
        }

        public static void DeleteConversation(Conversation c)
        {
            Conversations.Remove(c);
            if (Current == c)
            {
                if (Conversations.Count == 0)
                {
                    var fresh = new Conversation();
                    Conversations.Add(fresh);
                    Current = fresh;
                }
                else
                {
                    Current = Conversations[0];
                }
                CurrentChanged?.Invoke();
            }
            ConversationsChanged?.Invoke();
            Save();
        }

        public static void RenameConversation(Conversation c, string title)
        {
            title = title.Trim();
            if (title.Length == 0) return;
            c.Title = title;
            ConversationsChanged?.Invoke();
            Save();
        }

        // ---- Saving / loading ----
        private static string StatePath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "chats.json");
        private static string LegacyPath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "chat.json");

        public static void Load()
        {
            try
            {
                if (File.Exists(StatePath))
                {
                    var s = JsonSerializer.Deserialize<SavedState>(File.ReadAllText(StatePath));
                    if (s != null) Apply(s);
                }
                else if (File.Exists(LegacyPath))
                {
                    var old = JsonSerializer.Deserialize<SavedChat>(File.ReadAllText(LegacyPath));
                    if (old != null)
                    {
                        SelectedModel = old.Model;
                        if (DefaultPrompts.ContainsKey(old.Personality)) SelectedPersonality = old.Personality;
                        if (old.Messages.Count > 0)
                        {
                            var c = new Conversation { Title = "Previous chat" };
                            foreach (var m in old.Messages) c.Messages.Add(new ChatMessage(m.Role, m.Content));
                            Conversations.Add(c);
                        }
                    }
                }
            }
            catch
            {
                // Corrupt file: start fresh.
            }

            if (Conversations.Count == 0) Conversations.Add(new Conversation());
            if (!Conversations.Contains(Current)) Current = Conversations[0];
        }

        private static void Apply(SavedState s)
        {
            SelectedModel = s.Model;
            if (DefaultPrompts.ContainsKey(s.Personality)) SelectedPersonality = s.Personality;
            Theme = s.Theme is "Light" or "Dark" ? s.Theme : "System";

            PromptOverrides.Clear();
            foreach (var kv in s.PromptOverrides)
                if (DefaultPrompts.ContainsKey(kv.Key)) PromptOverrides[kv.Key] = kv.Value;

            Conversations.Clear();
            foreach (var sc in s.Conversations)
            {
                var c = new Conversation
                {
                    Id = string.IsNullOrEmpty(sc.Id) ? Guid.NewGuid().ToString("N") : sc.Id,
                    Title = string.IsNullOrWhiteSpace(sc.Title) ? Conversation.DefaultTitle : sc.Title
                };
                foreach (var m in sc.Messages) c.Messages.Add(new ChatMessage(m.Role, m.Content));
                Conversations.Add(c);
            }

            var current = Conversations.FirstOrDefault(c => c.Id == s.CurrentId);
            if (current != null) Current = current;
        }

        public static void Save()
        {
            try
            {
                var s = new SavedState
                {
                    Model = SelectedModel,
                    Personality = SelectedPersonality,
                    Theme = Theme,
                    CurrentId = Current.Id,
                    PromptOverrides = new Dictionary<string, string>(PromptOverrides),
                    Conversations = Conversations.Select(c => new SavedConversation
                    {
                        Id = c.Id,
                        Title = c.Title,
                        Messages = c.Messages
                            .Where(m => !m.IsPending)
                            .Select(m => new SavedMessage { Role = m.Role, Content = m.Content })
                            .ToList()
                    }).ToList()
                };
                File.WriteAllText(StatePath, JsonSerializer.Serialize(s));
            }
            catch
            {
                // Saving is best-effort.
            }
        }
    }

    // Port of ollama_service.dart
    public class OllamaService
    {
        private const string KeyIp = "ollama_ip";
        private const string KeyPort = "ollama_port";

        private static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };

        public static string Ip
        {
            get => ApplicationData.Current.LocalSettings.Values[KeyIp] as string ?? "localhost";
            set => ApplicationData.Current.LocalSettings.Values[KeyIp] = value;
        }

        public static string Port
        {
            get => ApplicationData.Current.LocalSettings.Values[KeyPort] as string ?? "11434";
            set => ApplicationData.Current.LocalSettings.Values[KeyPort] = value;
        }

        public static string BaseUrl
        {
            get
            {
                var ip = Ip.Trim();
                if (!ip.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !ip.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    ip = "http://" + ip;
                return $"{ip}:{Port.Trim()}";
            }
        }

        public async Task<List<string>> GetModelsAsync()
        {
            var models = new List<string>();
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var json = await Http.GetStringAsync($"{BaseUrl}/api/tags", cts.Token);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("models", out var arr))
                {
                    foreach (var m in arr.EnumerateArray())
                    {
                        if (m.TryGetProperty("name", out var n) && n.GetString() is { } name)
                            models.Add(name);
                    }
                }
            }
            catch
            {
                // Server offline: return an empty list.
            }
            return models;
        }

        public async IAsyncEnumerable<string> ChatStreamAsync(
            string model,
            IEnumerable<ChatMessage> history,
            string systemPrompt,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var messages = new List<object>();
            if (!string.IsNullOrEmpty(systemPrompt))
                messages.Add(new { role = "system", content = systemPrompt });
            foreach (var m in history)
                messages.Add(new { role = m.Role, content = m.Content });

            var body = JsonSerializer.Serialize(new { model, messages, stream = true });

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/chat")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("content", out var c))
                {
                    var text = c.GetString();
                    if (!string.IsNullOrEmpty(text)) yield return text;
                }
            }
        }
    }

    // Picks the right bubble template (user = right, assistant = left).
    public class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? UserTemplate { get; set; }
        public DataTemplate? AssistantTemplate { get; set; }

        protected override DataTemplate? SelectTemplateCore(object item) =>
            item is ChatMessage { IsUser: true } ? UserTemplate : AssistantTemplate;

        protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
            SelectTemplateCore(item);
    }
}
