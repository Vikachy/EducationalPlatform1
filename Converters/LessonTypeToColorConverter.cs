using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class LessonTypeToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string lessonType)
            {
                return lessonType.ToLower() switch
                {
                    "theory" => Color.FromArgb("#2196F3"),      // Синий
                    "practice" => Color.FromArgb("#FF9800"),    // Оранжевый
                    "test" => Color.FromArgb("#4CAF50"),        // Зеленый
                    _ => Color.FromArgb("#607D8B")              // Серый
                };
            }
            return Color.FromArgb("#607D8B");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}                                                                   