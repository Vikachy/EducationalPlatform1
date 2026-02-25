using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class ColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string colorHex && !string.IsNullOrEmpty(colorHex))
            {
                try
                {
                    return Color.FromArgb(colorHex);
                }
                catch
                {
                    return Color.FromArgb("#457b9d");
                }
            }
            return Color.FromArgb("#457b9d");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return color.ToHex();
            }
            return "#457b9d";
        }
    }
}