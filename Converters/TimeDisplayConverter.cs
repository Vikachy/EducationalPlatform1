using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class TimeDisplayConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var now = DateTime.Now;
                var today = now.Date;
                var yesterday = now.AddDays(-1).Date;

                if (dateTime.Date == today)
                    return dateTime.ToString("HH:mm");
                else if (dateTime.Date == yesterday)
                    return "Вчера";
                else if (dateTime.Year == now.Year)
                    return dateTime.ToString("dd MMM");
                else
                    return dateTime.ToString("dd.MM.yy");
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}