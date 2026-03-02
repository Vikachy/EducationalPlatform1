using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class RoleToVisibilityConverter : IValueConverter
    {
        // Параметры для настройки конвертера
        public int? RequiredRole { get; set; }
        public bool Invert { get; set; }
        public bool IsVisible { get; set; } = true;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not int userRole)
                return false;

            // Если указан RequiredRole через свойство
            if (RequiredRole.HasValue)
            {
                bool isVisible = userRole == RequiredRole.Value;
                return Invert ? !isVisible : isVisible;
            }

            // Если параметр передан как строка
            if (parameter is string roleParam)
            {
                var roles = roleParam.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var role in roles)
                {
                    if (int.TryParse(role.Trim(), out int requiredRole))
                    {
                        if (userRole == requiredRole)
                            return !Invert;
                    }
                }
                return Invert;
            }

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}