using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class StatusButtonConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Деактивировать" : "Активировать";
            }
            return "Изменить статус";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}