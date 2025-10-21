using Microsoft.Maui.Controls;

namespace EducationalPlatform.Animations
{
    public class FireAnimationAction : TriggerAction<VisualElement>
    {
        protected override async void Invoke(VisualElement sender)
        {
            while (true)
            {
                await sender.FadeTo(0.3, 500, Easing.SinInOut);
                await sender.FadeTo(1, 500, Easing.SinInOut);
                await Task.Delay(200);
            }
        }
    }

    public class FireTextAnimationAction : TriggerAction<Label>
    {
        protected override async void Invoke(Label sender)
        {
            while (true)
            {
                // Анимация изменения цвета текста
                var originalColor = sender.TextColor;
                await ChangeTextColor(sender, Color.FromArgb("#ff6b35"), 1000);
                await ChangeTextColor(sender, Color.FromArgb("#ff8e35"), 1000);
                await ChangeTextColor(sender, Color.FromArgb("#ff3535"), 1000);
                await ChangeTextColor(sender, originalColor, 1000);
            }
        }

        private async Task ChangeTextColor(Label label, Color color, uint duration)
        {
            var animation = new Animation(v => label.TextColor = color);
            animation.Commit(label, "TextColorAnimation", length: duration);
            await Task.Delay((int)duration);
        }
    }
}