using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class StatusButtonColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? Color.FromArgb("#F44336") : Color.FromArgb("#4CAF50");
            }
            return Color.FromArgb("#6C757D");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}