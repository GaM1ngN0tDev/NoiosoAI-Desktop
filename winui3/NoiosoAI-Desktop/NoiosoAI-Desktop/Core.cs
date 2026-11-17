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

        public ChatMessage(string role, string content)
        {
            Role = role;
            _content = content;
        }

        public string Role { get; }
        public bool IsUser => Role == "user";

        public string Content
        {
            get => _content;
            set
            {
                if (_content == value) return;
                _content = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // Shape of chat.json on disk.
    public class SavedMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    public class SavedChat
    {
        public string? Model { get; set; }
        public string Personality { get; set; } = "Default";
        public List<SavedMessage> Messages { get; set; } = new();
    }

    // State that survives page navigation and app restarts.
    public static class AppState
    {
        public static ObservableCollection<ChatMessage> Messages { get; } = new();
        public static string? SelectedModel { get; set; }
        public static string SelectedPersonality { get; set; } = "Default";

        public static readonly Dictionary<string, string> Personalities = new()
        {
            ["Default"] = "You are NoiosoAI, a helpful, polite, and precise AI assistant.",
            ["Engineer"] = "You are a Senior Software Engineer. Give highly technical, code-focused, concise, and optimized answers. Avoid unnecessary small talk.",
            ["Friendly"] = "You are a super friendly, empathetic, and warm assistant. Use an informal tone, support the user, and show enthusiasm using matching emojis.",
            ["Wise"] = "You are a wise philosopher. Respond with deep, well-thought-out reflections, calmly analyzing pros and cons with poetic nuance.",
            ["Sarcastic"] = "You are NoiosoAI, but today you are extremely sarcastic, witty, and slightly lazy. Tease the user playfully while still providing the actual solution.",
        };

        private static string FilePath =>
            Path.Combine(ApplicationData.Current.LocalFolder.Path, "chat.json");

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                var saved = JsonSerializer.Deserialize<SavedChat>(File.ReadAllText(FilePath));
                if (saved == null) return;

                SelectedModel = saved.Model;
                if (Personalities.ContainsKey(saved.Personality))
                    SelectedPersonality = saved.Personality;

                Messages.Clear();
                foreach (var m in saved.Messages)
                    Messages.Add(new ChatMessage(m.Role, m.Content));
            }
            catch
            {
                // Corrupt or missing file: start with an empty chat.
            }
        }

        public static void Save()
        {
            try
            {
                var saved = new SavedChat
                {
                    Model = SelectedModel,
                    Personality = SelectedPersonality,
                    Messages = Messages.Select(m => new SavedMessage { Role = m.Role, Content = m.Content }).ToList()
                };
                File.WriteAllText(FilePath, JsonSerializer.Serialize(saved));
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
