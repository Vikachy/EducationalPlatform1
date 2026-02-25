using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class AvatarToImageSourceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return "default_avatar.png";

            string avatarPath = value.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(avatarPath))
                return "default_avatar.png";

            // Если это data URL (base64)
            if (avatarPath.StartsWith("data:image"))
            {
                try
                {
                    return ImageSource.FromStream(() =>
                    {
                        var base64Data = avatarPath.Substring(avatarPath.IndexOf(",") + 1);
                        var imageBytes = System.Convert.FromBase64String(base64Data);
                        return new MemoryStream(imageBytes);
                    });
                }
                catch
                {
                    return "default_avatar.png";
                }
            }

            // Если это локальный файл
            if (File.Exists(avatarPath))
            {
                return ImageSource.FromFile(avatarPath);
            }

            // Если это путь в AppData
            string fullPath = Path.Combine(FileSystem.AppDataDirectory, avatarPath);
            if (File.Exists(fullPath))
            {
                return ImageSource.FromFile(fullPath);
            }

            // Аватар по умолчанию для групп
            if (avatarPath.StartsWith("group_") || avatarPath == "default_group.png")
            {
                return "default_group.png";
            }

            return "default_avatar.png";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}