// Converters/MessageStatusColorConverter.cs
using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class MessageStatusColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isRead)
            {
                return isRead ? "#34B7F1" : "#999999";
            }
            return "#999999";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}