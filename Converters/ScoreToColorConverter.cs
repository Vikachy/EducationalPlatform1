using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class ScoreToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int score)
            {
                if (score >= 85) return Color.FromArgb("#4CAF50"); // Зеленый
                if (score >= 70) return Color.FromArgb("#2196F3"); // Синий
                if (score >= 50) return Color.FromArgb("#FF9800"); // Оранжевый
                return Color.FromArgb("#F44336"); // Красный
            }
            return Color.FromArgb("#999999");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}