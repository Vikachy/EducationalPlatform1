using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class RoleToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int roleId)
            {
                return roleId switch
                {
                    1 => Color.FromArgb("#4CAF50"), // Студент - зеленый
                    2 => Color.FromArgb("#FF9800"), // Преподаватель - оранжевый
                    3 => Color.FromArgb("#9C27B0"), // Админ - фиолетовый
                    4 => Color.FromArgb("#2196F3"), // Контент-менеджер - синий
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