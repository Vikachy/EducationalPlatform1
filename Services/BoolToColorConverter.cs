using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isMyMessage && isMyMessage)
            {
                return Color.FromArgb("#2E86AB"); // Синий для моих сообщений
            }
            return Color.FromArgb("#666666"); // Серый для чужих
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToTextColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isMyMessage && isMyMessage)
            {
                return Colors.White; // Белый для моих сообщений
            }
            return Colors.Black; // Черный для чужих
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToTimeColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isMyMessage && isMyMessage)
            {
                return Color.FromArgb("#CCCCCC"); // Светло-серый для моих
            }
            return Color.FromArgb("#888888"); // Темно-серый для чужих
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}