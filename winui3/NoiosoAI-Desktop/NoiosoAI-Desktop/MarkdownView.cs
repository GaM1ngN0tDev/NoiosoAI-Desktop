using System;
using System.Linq;
using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace NoiosoAI_Desktop
{
    // Small built-in Markdown renderer (no NuGet needed).
    // Supports: ``` code blocks (with Copy button), `inline code`, **bold**,
    // # headings and - bullet lists.
    public sealed class MarkdownView : UserControl
    {
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register(nameof(Markdown), typeof(string), typeof(MarkdownView),
                new PropertyMetadata("", OnMarkdownChanged));

        public string Markdown
        {
            get => (string)GetValue(MarkdownProperty);
            set => SetValue(MarkdownProperty, value);
        }

        private readonly StackPanel _root = new() { Spacing = 6 };

        private static readonly FontFamily MonoFont = new("Cascadia Mono, Consolas");
        private static readonly Brush InlineCodeBrush = new SolidColorBrush(Color.FromArgb(255, 160, 130, 225));
        private static readonly Brush CodeBackground = new SolidColorBrush(Color.FromArgb(48, 128, 128, 128));

        public MarkdownView()
        {
            Content = _root;
        }

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((MarkdownView)d).Render();

        private void Render()
        {
            _root.Children.Clear();
            var text = Markdown ?? "";
            if (text.Length == 0) return;

            var lines = text.Replace("\r\n", "\n").Split('\n');
            var code = new StringBuilder();
            var inCode = false;

            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("```"))
                {
                    if (inCode)
                    {
                        AddCodeBlock(code.ToString().TrimEnd('\n'));
                        code.Clear();
                        inCode = false;
                    }
                    else
                    {
                        inCode = true;
                    }
                    continue;
                }

                if (inCode)
                {
                    code.Append(line).Append('\n');
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line)) continue;
                AddTextLine(line);
            }

            // Still streaming and the code block isn't closed yet: show what we have.
            if (inCode) AddCodeBlock(code.ToString().TrimEnd('\n'));
        }

        private void AddTextLine(string line)
        {
            var trimmed = line.TrimStart();
            var tb = new TextBlock { TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };

            var level = trimmed.TakeWhile(c => c == '#').Count();
            if (level is > 0 and <= 6 && trimmed.Length > level && trimmed[level] == ' ')
            {
                tb.FontSize = level == 1 ? 22 : level == 2 ? 19 : 16;
                tb.FontWeight = FontWeights.SemiBold;
                AddInlines(tb, trimmed.Substring(level).Trim());
            }
            else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                tb.Margin = new Thickness(8, 0, 0, 0);
                AddInlines(tb, "•  " + trimmed.Substring(2));
            }
            else
            {
                AddInlines(tb, line);
            }

            _root.Children.Add(tb);
        }

        private static void AddInlines(TextBlock tb, string text)
        {
            var sb = new StringBuilder();
            var bold = false;
            var code = false;

            void Flush()
            {
                if (sb.Length == 0) return;
                var run = new Run { Text = sb.ToString() };
                if (bold) run.FontWeight = FontWeights.Bold;
                if (code)
                {
                    run.FontFamily = MonoFont;
                    run.Foreground = InlineCodeBrush;
                }
                tb.Inlines.Add(run);
                sb.Clear();
            }

            for (var i = 0; i < text.Length; i++)
            {
                if (!code && text[i] == '*' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    Flush();
                    bold = !bold;
                    i++;
                }
                else if (text[i] == '`')
                {
                    Flush();
                    code = !code;
                }
                else
                {
                    sb.Append(text[i]);
                }
            }
            Flush();
        }

        private void AddCodeBlock(string code)
        {
            var codeText = new TextBlock
            {
                Text = code,
                FontFamily = MonoFont,
                FontSize = 13,
                IsTextSelectionEnabled = true,
                TextWrapping = TextWrapping.NoWrap
            };

            var scroll = new ScrollViewer
            {
                Content = codeText,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollMode = ScrollMode.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var copy = new Button
            {
                Content = new SymbolIcon(Symbol.Copy),
                Padding = new Thickness(6, 4, 6, 4),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ToolTipService.SetToolTip(copy, "Copy code");
            copy.Click += (_, _) =>
            {
                var package = new DataPackage();
                package.SetText(code);
                Clipboard.SetContent(package);
            };

            var grid = new Grid { RowSpacing = 4 };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(copy, 0);
            Grid.SetRow(scroll, 1);
            grid.Children.Add(copy);
            grid.Children.Add(scroll);

            _root.Children.Add(new Border
            {
                Child = grid,
                Background = CodeBackground,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 10)
            });
        }
    }
}
