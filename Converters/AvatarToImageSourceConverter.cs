using System.Globalization;
using EducationalPlatform.Services;
using Microsoft.Maui.Controls;

namespace EducationalPlatform.Converters
{
    /// <summary>
    /// Преобразует строку с данными аватара (base64, file:// или относительный путь)
    /// в пригодный для MAUI ImageSource объект.
    /// </summary>
    public class AvatarToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var avatarData = value as string;
            return ServiceHelper.GetImageSourceFromAvatarData(avatarData);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Обратное преобразование не требуется.
            return value;
        }
    }
}

