using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace EducationalPlatform.Services
{
    public static class ServiceHelper
    {
        public static T GetService<T>() where T : class
        {
            var service = Application.Current?.Handler?.MauiContext?.Services.GetService<T>();
            return service ?? throw new InvalidOperationException($"Service {typeof(T)} not registered");
        }

        /// <summary>
        /// Преобразует base64 data URL или путь к файлу в ImageSource
        /// </summary>
        public static ImageSource GetImageSourceFromAvatarData(string? avatarData)
        {
            if (string.IsNullOrEmpty(avatarData))
            {
                Console.WriteLine("⚠️ AvatarData пуст, используем дефолтный аватар");
                return ImageSource.FromFile("default_avatar.png");
            }

            Console.WriteLine($"🔍 Обработка аватара: {avatarData.Substring(0, Math.Min(50, avatarData.Length))}...");

            // Если это base64 data URL (новый формат)
            if (avatarData.StartsWith("data:image"))
            {
                try
                {
                    // Извлекаем base64 часть после запятой
                    var base64Index = avatarData.IndexOf(',');
                    if (base64Index > 0)
                    {
                        var base64String = avatarData.Substring(base64Index + 1);
                        var imageBytes = Convert.FromBase64String(base64String);
                        Console.WriteLine($"✅ Base64 аватар декодирован, размер: {imageBytes.Length} байт");
                        return ImageSource.FromStream(() => new MemoryStream(imageBytes));
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Неверный формат base64 data URL");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка декодирования base64 аватара: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    return ImageSource.FromFile("default_avatar.png");
                }
            }

            // Если это file:// префикс
            if (avatarData.StartsWith("file://"))
            {
                var filePath = avatarData.Substring(7); // Убираем "file://"
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"✅ Файл найден по пути: {filePath}");
                    return ImageSource.FromFile(filePath);
                }
                else
                {
                    Console.WriteLine($"⚠️ Файл не найден: {filePath}");
                }
            }

            // Если это путь к файлу (старый формат для обратной совместимости)
            if (File.Exists(avatarData))
            {
                Console.WriteLine($"✅ Файл найден: {avatarData}");
                return ImageSource.FromFile(avatarData);
            }

            // Если путь относительный, пробуем найти в папке приложения
            var localPath = Path.Combine(FileSystem.AppDataDirectory, avatarData);
            if (File.Exists(localPath))
            {
                Console.WriteLine($"✅ Файл найден по локальному пути: {localPath}");
                return ImageSource.FromFile(localPath);
            }

            Console.WriteLine($"⚠️ Аватар не найден ни по одному пути, используем дефолтный");
            return ImageSource.FromFile("default_avatar.png");
        }

        // УНИВЕРСАЛЬНЫЙ МЕТОД ДЛЯ ПОЛУЧЕНИЯ ИСТОЧНИКА ИЗОБРАЖЕНИЯ
        public static ImageSource GetUniversalImageSource(string imageData)
        {
            return GetImageSourceFromAvatarData(imageData);
        }

        // КОНВЕРТАЦИЯ ИЗОБРАЖЕНИЯ В BASE64
        public static async Task<string> ConvertImageToBase64Async(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                    return null;

                var imageBytes = await File.ReadAllBytesAsync(imagePath);
                var base64String = Convert.ToBase64String(imageBytes);

                var extension = Path.GetExtension(imagePath).ToLower();
                var mimeType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "image/jpeg"
                };

                return $"data:{mimeType};base64,{base64String}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка конвертации изображения в base64: {ex.Message}");
                return null;
            }
        }

        // ПРОВЕРКА ДОСТУПНОСТИ ФАЙЛА
        public static async Task<bool> IsFileAccessibleAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                // Проверяем существование файла
                if (File.Exists(filePath))
                    return true;

                // Пробуем найти файл в папке приложения
                var fileName = Path.GetFileName(filePath);
                var appDataPath = FileSystem.AppDataDirectory;

                var possiblePaths = new[]
                {
                    Path.Combine(appDataPath, fileName),
                    Path.Combine(appDataPath, "Documents", fileName),
                    Path.Combine(appDataPath, "ChatFiles", fileName),
                    Path.Combine(appDataPath, "TheoryFiles", fileName),
                    Path.Combine(appDataPath, "PracticeFiles", fileName)
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка проверки доступности файла: {ex.Message}");
                return false;
            }
        }
    }
}