using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class MoscowTimeConverter : IValueConverter
    {
        private static TimeZoneInfo? _moscowZone;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                try
                {
                    // Ленивая инициализация часового пояса
                    if (_moscowZone == null)
                    {
                        try
                        {
                            // Для Windows
                            _moscowZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                        }
                        catch
                        {
                            try
                            {
                                // Для Linux/Mac
                                _moscowZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                            }
                            catch
                            {
                                // Если не нашли, создаем смещение +3 часа
                                _moscowZone = TimeZoneInfo.CreateCustomTimeZone(
                                    "Moscow Time",
                                    TimeSpan.FromHours(3),
                                    "Moscow Time",
                                    "Moscow Time");
                            }
                        }
                    }

                    // Определяем, в каком формате время
                    DateTime moscowTime;

                    // Если время с UTC меткой
                    if (dateTime.Kind == DateTimeKind.Utc)
                    {
                        // Прямая конвертация из UTC в московское
                        moscowTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, _moscowZone);
                    }
                    else if (dateTime.Kind == DateTimeKind.Local)
                    {
                        // Конвертируем локальное в UTC, потом в московское
                        var utcTime = dateTime.ToUniversalTime();
                        moscowTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, _moscowZone);
                    }
                    else
                    {
                        // Unspecified - предполагаем что это UTC (как в БД)
                        var utcTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                        moscowTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, _moscowZone);
                    }

                    var nowUtc = DateTime.UtcNow;
                    var nowMoscow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, _moscowZone);

                    var today = nowMoscow.Date;
                    var yesterday = today.AddDays(-1);

                    if (moscowTime.Date == today)
                        return moscowTime.ToString("HH:mm");

                    if (moscowTime.Date == yesterday)
                        return "Вчера";

                    if (moscowTime.Year == nowMoscow.Year)
                        return moscowTime.ToString("dd MMM");

                    return moscowTime.ToString("dd.MM.yy");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Ошибка в конвертере времени: {ex.Message}");

                    // Fallback - просто добавляем 3 часа к тому, что есть
                    return dateTime.AddHours(3).ToString("HH:mm");
                }
            }

            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}