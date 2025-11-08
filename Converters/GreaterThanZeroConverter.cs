using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class GreaterThanZeroConverter : IValueConverter
    {
        public static GreaterThanZeroConverter Instance { get; } = new GreaterThanZeroConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}