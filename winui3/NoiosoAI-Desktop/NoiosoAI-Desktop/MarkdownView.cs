using System;
using System.Collections.Generic;
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
    //
    // Rendering is incremental: unchanged blocks keep their controls, so
    // streaming text doesn't make the whole bubble flash.
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

        public static readonly DependencyProperty ShowCursorProperty =
            DependencyProperty.Register(nameof(ShowCursor), typeof(bool), typeof(MarkdownView),
                new PropertyMetadata(false, OnMarkdownChanged));

        // Shows a small cursor at the end of the text while a reply is streaming.
        public bool ShowCursor
        {
            get => (bool)GetValue(ShowCursorProperty);
            set => SetValue(ShowCursorProperty, value);
        }

        private sealed record Block(string Kind, string Text);

        private readonly StackPanel _root = new() { Spacing = 6 };
        private List<Block> _blocks = new();

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
            var newBlocks = Parse(Markdown ?? "", ShowCursor);

            for (var i = 0; i < newBlocks.Count; i++)
            {
                var nb = newBlocks[i];

                if (i < _blocks.Count)
                {
                    var ob = _blocks[i];
                    if (ob == nb) continue;                                   // unchanged: keep the control
                    if (ob.Kind == nb.Kind && TryUpdate(_root.Children[i], nb)) continue;
                    _root.Children[i] = Build(nb);                            // different kind: replace
                }
                else
                {
                    _root.Children.Add(Build(nb));
                }
            }

            while (_root.Children.Count > newBlocks.Count)
                _root.Children.RemoveAt(_root.Children.Count - 1);

            _blocks = newBlocks;
        }

        private static List<Block> Parse(string text, bool cursor)
        {
            var blocks = new List<Block>();
            if (text.Length == 0) return blocks;

            var lines = text.Replace("\r\n", "\n").Split('\n');
            var code = new StringBuilder();
            var inCode = false;

            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("```"))
                {
                    if (inCode)
                    {
                        blocks.Add(new Block("code", code.ToString().TrimEnd('\n')));
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

                var trimmed = line.TrimStart();
                var level = trimmed.TakeWhile(c => c == '#').Count();

                if (level is > 0 and <= 6 && trimmed.Length > level && trimmed[level] == ' ')
                    blocks.Add(new Block(level == 1 ? "h1" : level == 2 ? "h2" : "h3", trimmed.Substring(level).Trim()));
                else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                    blocks.Add(new Block("bullet", "•  " + trimmed.Substring(2)));
                else
                    blocks.Add(new Block("p", line));
            }

            // Still streaming and the code block isn't closed yet: show what we have.
            if (inCode) blocks.Add(new Block("code", code.ToString().TrimEnd('\n')));

            if (cursor && blocks.Count > 0)
            {
                var last = blocks[^1];
                if (last.Kind != "code") blocks[^1] = last with { Text = last.Text + " ▍" };
            }

            return blocks;
        }

        // Update an existing control in place. Returns false if it can't be reused.
        private static bool TryUpdate(UIElement element, Block block)
        {
            if (block.Kind == "code")
            {
                if (element is Border { Tag: TextBlock codeText })
                {
                    codeText.Text = block.Text;
                    return true;
                }
                return false;
            }

            if (element is TextBlock tb)
            {
                tb.Inlines.Clear();
                AddInlines(tb, block.Text);
                return true;
            }
            return false;
        }

        private static UIElement Build(Block block)
        {
            if (block.Kind == "code") return BuildCodeBlock(block.Text);

            var tb = new TextBlock { TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };

            switch (block.Kind)
            {
                case "h1":
                    tb.FontSize = 22;
                    tb.FontWeight = FontWeights.SemiBold;
                    break;
                case "h2":
                    tb.FontSize = 19;
                    tb.FontWeight = FontWeights.SemiBold;
                    break;
                case "h3":
                    tb.FontSize = 16;
                    tb.FontWeight = FontWeights.SemiBold;
                    break;
                case "bullet":
                    tb.Margin = new Thickness(8, 0, 0, 0);
                    break;
            }

            AddInlines(tb, block.Text);
            return tb;
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

        private static UIElement BuildCodeBlock(string code)
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
                package.SetText(codeText.Text);   // read at click time so it matches what is shown
                Clipboard.SetContent(package);
            };

            var grid = new Grid { RowSpacing = 4 };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(copy, 0);
            Grid.SetRow(scroll, 1);
            grid.Children.Add(copy);
            grid.Children.Add(scroll);

            return new Border
            {
                Child = grid,
                Tag = codeText,   // lets Render() update the text in place
                Background = CodeBackground,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 10)
            };
        }
    }
}
