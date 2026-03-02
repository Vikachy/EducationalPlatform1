using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class LanguageIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string icon && !string.IsNullOrEmpty(icon))
                return icon;

            // Стандартные иконки для популярных языков
            if (value is string languageName)
            {
                return languageName.ToLower() switch
                {
                    "python" => "🐍",
                    "javascript" => "🟨",
                    "c#" => "🔷",
                    "java" => "☕",
                    "c++" => "⚙️",
                    "php" => "🐘",
                    "ruby" => "💎",
                    "swift" => "🍎",
                    "kotlin" => "📱",
                    "go" => "🔵",
                    "typescript" => "🔷",
                    "rust" => "⚙️",
                    "sql" => "🗄️",
                    _ => "💻"
                };
            }

            return "💻"; // Иконка по умолчанию
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}