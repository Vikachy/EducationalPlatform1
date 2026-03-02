using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
            }

            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "active" or "активна" => Color.FromArgb("#4CAF50"),
                    "inactive" or "неактивна" => Color.FromArgb("#F44336"),
                    "published" or "опубликован" => Color.FromArgb("#4CAF50"),
                    "draft" or "черновик" => Color.FromArgb("#FF9800"),
                    "completed" or "завершена" => Color.FromArgb("#2196F3"),
                    _ => Color.FromArgb("#999999")
                };
            }

            return Color.FromArgb("#999999");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}