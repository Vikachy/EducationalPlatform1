// FileService.cs
using Microsoft.Maui.Storage;

namespace EducationalPlatform.Services
{
    public class FileService
    {
        public async Task<string> SaveAvatarAsync(Stream fileStream, string fileName, int userId)
        {
            try
            {
                // Создаем папку для аватаров, если её нет
                var avatarsFolder = Path.Combine(FileSystem.AppDataDirectory, "Avatars");
                if (!Directory.Exists(avatarsFolder))
                {
                    Directory.CreateDirectory(avatarsFolder);
                }

                // Генерируем уникальное имя файла
                var fileExtension = Path.GetExtension(fileName);
                var newFileName = $"avatar_{userId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var fullPath = Path.Combine(avatarsFolder, newFileName);

                // Сохраняем файл
                using (var file = File.Create(fullPath))
                {
                    await fileStream.CopyToAsync(file);
                }

                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения аватара: {ex.Message}");
                return null;
            }
        }

        public string GetAvatarPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            if (File.Exists(fileName))
                return fileName;

            return null;
        }

        public void DeleteAvatar(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления аватара: {ex.Message}");
            }
        }
    }
}