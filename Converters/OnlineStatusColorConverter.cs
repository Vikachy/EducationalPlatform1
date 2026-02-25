using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class OnlineStatusColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isOnline)
            {
                return isOnline ? "#4CAF50" : "#9E9E9E";
            }
            return "#9E9E9E";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}