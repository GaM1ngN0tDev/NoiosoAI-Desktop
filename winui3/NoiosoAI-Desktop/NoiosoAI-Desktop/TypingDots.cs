using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace NoiosoAI_Desktop
{
    // Three softly pulsing dots, shown while the model is "thinking".
    public sealed class TypingDots : UserControl
    {
        private static readonly Brush DotBrush = new SolidColorBrush(Color.FromArgb(255, 160, 130, 225));

        public TypingDots()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Margin = new Thickness(2, 8, 2, 8)
            };

            for (var i = 0; i < 3; i++)
                panel.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = DotBrush });

            Content = panel;
            Loaded += (_, _) => StartAnimations(panel);
        }

        private static void StartAnimations(StackPanel panel)
        {
            for (var i = 0; i < panel.Children.Count; i++)
            {
                var dot = panel.Children[i];
                var visual = ElementCompositionPreview.GetElementVisual(dot);
                var compositor = visual.Compositor;
                ElementCompositionPreview.SetIsTranslationEnabled(dot, true);

                var duration = TimeSpan.FromMilliseconds(900);
                var delay = TimeSpan.FromMilliseconds(150 * i);

                var fade = compositor.CreateScalarKeyFrameAnimation();
                fade.InsertKeyFrame(0f, 0.25f);
                fade.InsertKeyFrame(0.5f, 1f);
                fade.InsertKeyFrame(1f, 0.25f);
                fade.Duration = duration;
                fade.DelayTime = delay;
                fade.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
                fade.IterationBehavior = AnimationIterationBehavior.Forever;

                var bounce = compositor.CreateScalarKeyFrameAnimation();
                bounce.InsertKeyFrame(0f, 0f);
                bounce.InsertKeyFrame(0.5f, -4f);
                bounce.InsertKeyFrame(1f, 0f);
                bounce.Duration = duration;
                bounce.DelayTime = delay;
                bounce.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
                bounce.IterationBehavior = AnimationIterationBehavior.Forever;

                visual.StartAnimation("Opacity", fade);
                visual.StartAnimation("Translation.Y", bounce);
            }
        }
    }
}
