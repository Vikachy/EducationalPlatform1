using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class RoleToTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int roleId)
            {
                return roleId switch
                {
                    1 => "Студент",
                    2 => "Преподаватель",
                    3 => "Администратор",
                    4 => "Контент-менеджер",
                    _ => "Неизвестно"
                };
            }
            return "Неизвестно";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}