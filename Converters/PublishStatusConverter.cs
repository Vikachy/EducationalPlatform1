using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class PublishStatusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isPublished)
            {
                return isPublished ? "Опубликован" : "Черновик";
            }
            return "Неизвестно";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}