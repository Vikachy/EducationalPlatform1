using Microsoft.Maui.Storage;
using System.Net.Http;

namespace EducationalPlatform.Services
{
    public class FileService
    {
        private readonly string _documentsFolder;

        public FileService()
        {
            _documentsFolder = Path.Combine(FileSystem.AppDataDirectory, "Documents");
            if (!Directory.Exists(_documentsFolder))
            {
                Directory.CreateDirectory(_documentsFolder);
            }
        }

        public string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileName = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);

            return $"{fileName}_{timestamp}_{random}{extension}";
        }

        public async Task<string?> SaveDocumentAsync(Stream fileStream, string fileName)
        {
            try
            {
                var filePath = Path.Combine(_documentsFolder, fileName);

                using (var file = File.Create(filePath))
                {
                    await fileStream.CopyToAsync(file);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения файла: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DownloadFileAsync(string filePath, string downloadFileName)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                // Для Android используем MediaStore
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    await ShareFileAsync(filePath, downloadFileName);
                    return true;
                }
                else
                {
                    // Для других платформ используем стандартный механизм
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = "Скачать файл",
                        File = new ShareFile(filePath)
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка скачивания файла: {ex.Message}");
                return false;
            }
        }

        private async Task ShareFileAsync(string filePath, string fileName)
        {
            try
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Скачать файл",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка шеринга файла: {ex.Message}");
            }
        }

        public string GetFileSize(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                long bytes = fileInfo.Length;

                if (bytes >= 1 << 30) // GB
                    return $"{(bytes / (1 << 30)):F1} GB";
                if (bytes >= 1 << 20) // MB
                    return $"{(bytes / (1 << 20)):F1} MB";
                if (bytes >= 1 << 10) // KB
                    return $"{(bytes / (1 << 10)):F1} KB";

                return $"{bytes} B";
            }
            catch
            {
                return "Неизвестно";
            }
        }

        public string GetFileIcon(string fileType)
        {
            return fileType?.ToLower() switch
            {
                ".pdf" => "📄",
                ".doc" or ".docx" => "📝",
                ".ppt" or ".pptx" => "📊",
                ".xls" or ".xlsx" => "📈",
                ".zip" or ".rar" or ".7z" => "📦",
                ".txt" => "📃",
                ".cs" or ".java" or ".py" or ".js" => "💻",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "🖼️",
                ".mp4" or ".avi" or ".mov" => "🎬",
                ".mp3" or ".wav" => "🎵",
                _ => "📎"
            };
        }

        // Скачивание и открытие файла
        public async Task<bool> DownloadAndOpenFileAsync(string fileUrl, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return false;

                // Для веб-URL скачиваем файл
                if (fileUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    return await DownloadFileFromUrlAsync(fileUrl, fileName);
                }

                // Для локальных файлов
                if (File.Exists(fileUrl))
                {
                    // Копируем файл в папку загрузок
                    var downloadPath = Path.Combine(FileSystem.CacheDirectory, fileName);
                    File.Copy(fileUrl, downloadPath, true);
                    
                    // Открываем файл
                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(downloadPath)
                    });
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка открытия файла: {ex.Message}");
                return false;
            }
        }

        // Скачивание файла из URL
        public async Task<bool> DownloadFileFromUrlAsync(string fileUrl, string fileName)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(fileUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    var downloadPath = Path.Combine(_documentsFolder, fileName);
                    
                    await File.WriteAllBytesAsync(downloadPath, fileBytes);
                    
                    // Открываем файл
                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(downloadPath)
                    });
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка скачивания файла: {ex.Message}");
                return false;
            }
        }

    }
}