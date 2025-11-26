using Microsoft.Maui.Storage;
using System.Net.Http;

namespace EducationalPlatform.Services
{
    public class FileService
    {
        private readonly string _documentsFolder;
        private readonly string _chatFilesFolder;
        private readonly string _theoryFilesFolder;
        private readonly string _practiceFilesFolder;
        private readonly string _downloadsFolder;
        private readonly string _avatarsFolder;
        private readonly string _sharedFilesRoot;
        private readonly string _sharedAvatarsRoot;

        public FileService()
        {
            // Инициализируем все необходимые папки
            _documentsFolder = Path.Combine(FileSystem.AppDataDirectory, "Documents");
            _chatFilesFolder = Path.Combine(FileSystem.AppDataDirectory, "ChatFiles");
            _theoryFilesFolder = Path.Combine(FileSystem.AppDataDirectory, "TheoryFiles");
            _practiceFilesFolder = Path.Combine(FileSystem.AppDataDirectory, "PracticeFiles");
            _downloadsFolder = Path.Combine(FileSystem.AppDataDirectory, "Downloads");
            _avatarsFolder = Path.Combine(FileSystem.AppDataDirectory, "Avatars");
            _sharedFilesRoot = Path.Combine(FileSystem.AppDataDirectory, "files");
            _sharedAvatarsRoot = Path.Combine(FileSystem.AppDataDirectory, "avatars");

            // Создаем папки если они не существуют
            CreateDirectoryIfNotExists(_documentsFolder);
            CreateDirectoryIfNotExists(_chatFilesFolder);
            CreateDirectoryIfNotExists(_theoryFilesFolder);
            CreateDirectoryIfNotExists(_practiceFilesFolder);
            CreateDirectoryIfNotExists(_downloadsFolder);
            CreateDirectoryIfNotExists(_avatarsFolder);
            CreateDirectoryIfNotExists(_sharedFilesRoot);
            CreateDirectoryIfNotExists(_sharedAvatarsRoot);
        }

        private void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Console.WriteLine($"✅ Создана папка: {path}");
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

        // УНИВЕРСАЛЬНЫЙ МЕТОД СОХРАНЕНИЯ ФАЙЛОВ ДЛЯ ВСЕХ ПЛАТФОРМ
        public async Task<string?> SaveFileCrossPlatform(Stream fileStream, string fileName, string folderType = "Documents")
        {
            try
            {
                string targetFolder = GetTargetFolder(folderType);
                var filePath = Path.Combine(targetFolder, fileName);

                using (var file = File.Create(filePath))
                {
                    await fileStream.CopyToAsync(file);
                }

                Console.WriteLine($"✅ Файл сохранен: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения файла: {ex.Message}");
                return null;
            }
        }

        // СОВМЕСТИМОСТЬ С СУЩЕСТВУЮЩИМ КОДОМ
        public async Task<string?> SaveDocumentAsync(Stream fileStream, string fileName)
        {
            return await SaveFileCrossPlatform(fileStream, fileName, "Documents");
        }

        public bool IsDataUrl(string? value)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> SaveDataUrlToFileAsync(string dataUrl, string fileName, string folderType = "Documents")
        {
            if (string.IsNullOrWhiteSpace(dataUrl))
                return string.Empty;

            var bytes = ConvertDataUrlToBytes(dataUrl, out var mimeExtension);
            var safeFileName = EnsureFileNameHasExtension(fileName, mimeExtension);
            var uniqueName = GenerateUniqueFileName(safeFileName);
            var targetFolder = GetTargetFolder(folderType);
            var destination = Path.Combine(targetFolder, uniqueName);
            await File.WriteAllBytesAsync(destination, bytes);
            return destination;
        }

        public async Task<string> CreateDataUrlAsync(Stream fileStream, string fileName)
        {
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            var mimeType = GetMimeType(Path.GetExtension(fileName));

            var base64 = Convert.ToBase64String(bytes);
            return $"data:{mimeType};base64,{base64}";
        }

        public async Task<bool> DownloadFileAsync(string filePath, string downloadFileName)
        {
            try
            {
                Console.WriteLine($"📥 DownloadFileAsync: {filePath} -> {downloadFileName}");

                // РЕШАЕМ ПУТЬ К ФАЙЛУ ПЕРЕД СКАЧИВАНИЕМ
                var resolvedPath = await ResolveFilePath(filePath, downloadFileName);

                if (!File.Exists(resolvedPath))
                {
                    Console.WriteLine($"❌ Файл не существует: {resolvedPath}");
                    return false;
                }

                // Для всех платформ используем Share API для скачивания
                try
                {
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = "Скачать файл",
                        File = new ShareFile(resolvedPath)
                    });
                    Console.WriteLine($"✅ Файл успешно предложен для скачивания: {downloadFileName}");
                    return true;
                }
                catch (Exception shareEx)
                {
                    Console.WriteLine($"⚠️ Ошибка Share API: {shareEx.Message}");

                    // Fallback: копируем в папку Downloads
                    string downloadsPath;
                    if (DeviceInfo.Platform == DevicePlatform.WinUI)
                    {
                        downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", downloadFileName);
                    }
                    else
                    {
                        // Для Android/iOS используем AppDataDirectory
                        downloadsPath = Path.Combine(_downloadsFolder, downloadFileName);
                    }

                    File.Copy(resolvedPath, downloadsPath, overwrite: true);
                    Console.WriteLine($"✅ Файл скопирован в: {downloadsPath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка скачивания файла: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        // УНИВЕРСАЛЬНЫЙ МЕТОД ДЛЯ РЕШЕНИЯ ПУТЕЙ К ФАЙЛАМ НА ВСЕХ ПЛАТФОРМАХ
        public async Task<string> ResolveFilePath(string filePath, string? preferredFileName = null, string folderType = "Documents")
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            try
            {
                if (IsDataUrl(filePath))
                {
                    Console.WriteLine("🧩 Восстанавливаем файл из base64 data URL");
                    var targetFolder = GetTargetFolder(folderType);

                    // ИСПРАВЛЕНИЕ: используем другое имя переменной
                    var localFileName = EnsureFileNameHasExtension(preferredFileName ?? $"shared_{DateTime.UtcNow:yyyyMMddHHmmss}", null);
                    var uniqueName = GenerateUniqueFileName(localFileName);
                    var destination = Path.Combine(targetFolder, uniqueName);
                    var bytes = ConvertDataUrlToBytes(filePath, out var mimeExtension);

                    if (!string.IsNullOrEmpty(mimeExtension))
                    {
                        destination = Path.ChangeExtension(destination, mimeExtension);
                    }

                    await File.WriteAllBytesAsync(destination, bytes);
                    return destination;
                }

                // Если файл существует по указанному пути
                if (File.Exists(filePath))
                    return filePath;

                // Ищем файл в различных папках приложения
                var searchFileName = Path.GetFileName(filePath); // ИСПРАВЛЕНИЕ: другое имя переменной

                var possiblePaths = new[]
                {
            Path.Combine(_sharedFilesRoot, searchFileName),
            Path.Combine(_chatFilesFolder, searchFileName),
            Path.Combine(_theoryFilesFolder, searchFileName),
            Path.Combine(_practiceFilesFolder, searchFileName),
            Path.Combine(_documentsFolder, searchFileName),
            Path.Combine(_downloadsFolder, searchFileName),
            Path.Combine(_avatarsFolder, searchFileName),
            Path.Combine(FileSystem.AppDataDirectory, searchFileName)
        };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"✅ Файл найден по альтернативному пути: {path}");
                        return path;
                    }
                }

                Console.WriteLine($"⚠️ Файл не найден ни по одному пути: {searchFileName}");
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка решения пути файла: {ex.Message}");
                return filePath;
            }
        }

        // ПРОВЕРКА СУЩЕСТВОВАНИЯ ФАЙЛА
        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                if (File.Exists(filePath))
                    return true;

                var resolvedPath = await ResolveFilePath(filePath);
                return File.Exists(resolvedPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка проверки файла: {ex.Message}");
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

        public string GetMimeType(string? extension)
        {
            return (extension ?? string.Empty).ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".zip" => "application/zip",
                ".mp4" => "video/mp4",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
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

                // РЕШАЕМ ПУТЬ ДЛЯ ЛОКАЛЬНЫХ ФАЙЛОВ
                var resolvedPath = await ResolveFilePath(fileUrl);

                if (File.Exists(resolvedPath))
                {
                    // Копируем файл в папку загрузок
                    var downloadPath = Path.Combine(FileSystem.CacheDirectory, fileName);
                    File.Copy(resolvedPath, downloadPath, true);

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

        // ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ ДЛЯ РАБОТЫ С ФАЙЛАМИ
        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var resolvedPath = await ResolveFilePath(filePath);

                if (File.Exists(resolvedPath))
                {
                    File.Delete(resolvedPath);
                    Console.WriteLine($"✅ Файл удален: {resolvedPath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка удаления файла: {ex.Message}");
                return false;
            }
        }

        public List<string> GetFilesInDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    return Directory.GetFiles(directoryPath).ToList();
                }
                return new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения файлов из директории: {ex.Message}");
                return new List<string>();
            }
        }


        public async Task<string?> SaveFileAsync(byte[] fileBytes, string fileName, string folderName)
        {
            try
            {
                Console.WriteLine($"💾 Сохраняем файл {fileName} в папку {folderName}");

                // Создаем папку если её нет
                var folderPath = Path.Combine(FileSystem.AppDataDirectory, folderName);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    Console.WriteLine($"📁 Создана папка: {folderPath}");
                }

                // Генерируем уникальное имя файла
                var fileExtension = Path.GetExtension(fileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var fullPath = Path.Combine(folderPath, uniqueFileName);

                // Сохраняем файл
                await File.WriteAllBytesAsync(fullPath, fileBytes);

                Console.WriteLine($"✅ Файл сохранен: {fullPath}");
                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения файла: {ex.Message}");
                return null;
            }
        }

        // ФОРМАТИРОВАНИЕ РАЗМЕРА ФАЙЛА (совместимость)
        public string FormatFileSize(long bytes)
        {
            if (bytes >= 1 << 30) return $"{(bytes / (1 << 30)):F1} GB";
            if (bytes >= 1 << 20) return $"{(bytes / (1 << 20)):F1} MB";
            if (bytes >= 1 << 10) return $"{(bytes / (1 << 10)):F1} KB";
            return $"{bytes} B";
        }

        private byte[] ConvertDataUrlToBytes(string dataUrl, out string? extensionFromMime)
        {
            extensionFromMime = null;
            try
            {
                var commaIndex = dataUrl.IndexOf(',');
                if (commaIndex <= 0)
                {
                    return Array.Empty<byte>();
                }

                var meta = dataUrl.Substring(5, commaIndex - 5); // skip "data:"
                var base64Part = dataUrl.Substring(commaIndex + 1);
                var metaParts = meta.Split(';');
                if (metaParts.Length > 0)
                {
                    var mime = metaParts[0];
                    extensionFromMime = MimeToExtension(mime);
                }

                return Convert.FromBase64String(base64Part);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка декодирования data URL: {ex.Message}");
                return Array.Empty<byte>();
            }
        }

        private string? MimeToExtension(string mime)
        {
            return mime.ToLower() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "application/pdf" => ".pdf",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                "application/vnd.ms-powerpoint" => ".ppt",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
                "application/vnd.ms-excel" => ".xls",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
                "text/plain" => ".txt",
                "video/mp4" => ".mp4",
                _ => ".bin"
            };
        }

        private string EnsureFileNameHasExtension(string fileName, string? fallbackExtension)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = $"file_{DateTime.UtcNow:yyyyMMddHHmmss}";
            }

            if (Path.HasExtension(fileName))
            {
                return fileName;
            }

            var extension = fallbackExtension ?? ".bin";
            return $"{fileName}{extension}";
        }

        private string GetTargetFolder(string folderType)
        {
            return folderType switch
            {
                "ChatFiles" => _chatFilesFolder,
                "TheoryFiles" => _theoryFilesFolder,
                "PracticeFiles" => _practiceFilesFolder,
                "Avatars" => _avatarsFolder,
                "Downloads" => _downloadsFolder,
                "SharedFiles" => _sharedFilesRoot,
                "SharedAvatars" => _sharedAvatarsRoot,
                _ => _documentsFolder
            };
        }
    }
}