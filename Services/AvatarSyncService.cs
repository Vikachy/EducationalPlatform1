using Microsoft.Maui.Storage;

namespace EducationalPlatform.Services
{
    public class AvatarSyncService
    {
        private readonly DatabaseService _dbService;
        private readonly string _cloudStorageUrl; // URL вашего облачного хранилища

        public AvatarSyncService(DatabaseService dbService, string cloudStorageUrl = "https://your-storage.com/api")
        {
            _dbService = dbService;
            _cloudStorageUrl = cloudStorageUrl;
        }

        /// <summary>
        /// Загружает аватарку в облачное хранилище и сохраняет ссылку в БД
        /// </summary>
        public async Task<string?> UploadAvatarAsync(int userId, Stream imageStream, string fileName)
        {
            try
            {
                // 1. Загружаем в облачное хранилище
                var cloudUrl = await UploadToCloudStorageAsync(userId, imageStream, fileName);
                if (string.IsNullOrEmpty(cloudUrl))
                {
                    return null;
                }

                // 2. Сохраняем ссылку в БД
                var success = await _dbService.UpdateUserAvatarAsync(userId, cloudUrl);
                if (!success)
                {
                    Console.WriteLine("Failed to update avatar URL in database");
                    return null;
                }

                // 3. Сохраняем локально для офлайн доступа
                await SaveAvatarLocallyAsync(userId, imageStream, fileName);

                return cloudUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading avatar: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Загружает аватарку из облачного хранилища при входе
        /// </summary>
        public async Task<string?> DownloadAvatarAsync(int userId)
        {
            try
            {
                // 1. Получаем URL из БД
                var avatarUrl = await _dbService.GetUserAvatarUrlAsync(userId);
                if (string.IsNullOrEmpty(avatarUrl))
                {
                    return null;
                }

                // 2. Проверяем локальный кэш
                var localPath = GetLocalAvatarPath(userId);
                if (File.Exists(localPath))
                {
                    var localFileInfo = new FileInfo(localPath);
                    // Если файл свежий (менее 24 часов), используем локальный
                    if (DateTime.Now - localFileInfo.LastWriteTime < TimeSpan.FromHours(24))
                    {
                        return localPath;
                    }
                }

                // 3. Загружаем из облака
                var imageBytes = await DownloadFromCloudStorageAsync(avatarUrl);
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    return null;
                }

                // 4. Сохраняем локально
                var directory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(localPath, imageBytes);
                return localPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading avatar: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> UploadToCloudStorageAsync(int userId, Stream imageStream, string fileName)
        {
            try
            {
                using var httpClient = new HttpClient();
                using var content = new MultipartFormDataContent();

                // Создаем уникальное имя файла
                var uniqueFileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(fileName)}";
                var streamContent = new StreamContent(imageStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "file", uniqueFileName);

                var response = await httpClient.PostAsync($"{_cloudStorageUrl}/upload/avatar", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    // Предполагаем, что сервер возвращает JSON с полем "url"
                    // В реальном проекте используйте JsonSerializer
                    var url = result.Trim('"');
                    return url;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading to cloud storage: {ex.Message}");
                return null;
            }
        }

        private async Task<byte[]?> DownloadFromCloudStorageAsync(string url)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading from cloud storage: {ex.Message}");
                return null;
            }
        }

        private async Task SaveAvatarLocallyAsync(int userId, Stream imageStream, string fileName)
        {
            try
            {
                var localPath = GetLocalAvatarPath(userId);
                var directory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var fileStream = File.Create(localPath);
                imageStream.Position = 0;
                await imageStream.CopyToAsync(fileStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving avatar locally: {ex.Message}");
            }
        }

        private string GetLocalAvatarPath(int userId)
        {
            var appDataPath = FileSystem.AppDataDirectory;
            return Path.Combine(appDataPath, "Avatars", $"avatar_{userId}.jpg");
        }

        /// <summary>
        /// Синхронизирует аватарку при входе пользователя
        /// </summary>
        public async Task<string?> SyncAvatarOnLoginAsync(int userId)
        {
            return await DownloadAvatarAsync(userId);
        }
    }
}

