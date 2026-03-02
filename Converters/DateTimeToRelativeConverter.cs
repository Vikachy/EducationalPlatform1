using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class DateTimeToRelativeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var diff = DateTime.Now - dateTime;

                if (diff.TotalMinutes < 1)
                    return "только что";
                if (diff.TotalMinutes < 60)
                    return $"{(int)diff.TotalMinutes} мин назад";
                if (diff.TotalHours < 24)
                    return $"{(int)diff.TotalHours} ч назад";
                if (diff.TotalDays < 7)
                    return $"{(int)diff.TotalDays} дн назад";
                if (diff.TotalDays < 30)
                    return $"{(int)(diff.TotalDays / 7)} нед назад";
                if (diff.TotalDays < 365)
                    return $"{(int)(diff.TotalDays / 30)} мес назад";

                return dateTime.ToString("dd.MM.yyyy");
            }

            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}