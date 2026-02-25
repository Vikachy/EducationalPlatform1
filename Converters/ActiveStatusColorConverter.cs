using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class ActiveStatusColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
            }
            return Color.FromArgb("#999999");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}