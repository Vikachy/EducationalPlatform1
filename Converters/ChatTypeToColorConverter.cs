using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Converters
{
    public class ChatTypeToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string chatType)
            {
                return chatType.ToLower() switch
                {
                    "group" => Color.FromArgb("#4CAF50"),
                    "teacher" => Color.FromArgb("#2196F3"),
                    "support" => Color.FromArgb("#FF9800"),
                    _ => Color.FromArgb("#9E9E9E")
                };
            }
            return Color.FromArgb("#9E9E9E");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ChatTypeToIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string chatType)
            {
                return chatType.ToLower() switch
                {
                    "group" => "👥",
                    "teacher" => "👨‍🏫",
                    "support" => "🆘",
                    _ => "💬"
                };
            }
            return "💬";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}