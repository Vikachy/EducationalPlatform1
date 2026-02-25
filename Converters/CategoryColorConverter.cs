using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class CategoryColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "courses" => Color.FromArgb("#4CAF50"),
                "contests" => Color.FromArgb("#FF9800"),
                "system" => Color.FromArgb("#9C27B0"),
                _ => Color.FromArgb("#2196F3")
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}