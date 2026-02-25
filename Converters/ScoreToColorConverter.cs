// Converters/ScoreToColorConverter.cs
using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class ScoreToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int score)
            {
                if (score >= 80) return Color.FromArgb("#4CAF50"); // Зеленый
                if (score >= 60) return Color.FromArgb("#FF9800"); // Оранжевый
                if (score >= 40) return Color.FromArgb("#F44336"); // Красный
            }
            return Color.FromArgb("#9E9E9E"); // Серый для "На проверке"
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

