using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace EducationalPlatform.Services
{
    public class FileService
    {
        private readonly string _avatarsFolder = "Avatars";
        private readonly string _documentsFolder = "Documents";
        private readonly string _tempFolder = "Temp";

        public FileService()
        {
            CreateDirectories();
        }

        private void CreateDirectories()
        {
            try
            {
                var documentsPath = FileSystem.AppDataDirectory;
                var avatarsPath = Path.Combine(documentsPath, _avatarsFolder);
                var documentsFolderPath = Path.Combine(documentsPath, _documentsFolder);
                var tempPath = Path.Combine(documentsPath, _tempFolder);

                if (!Directory.Exists(avatarsPath))
                    Directory.CreateDirectory(avatarsPath);
                
                if (!Directory.Exists(documentsFolderPath))
                    Directory.CreateDirectory(documentsFolderPath);
                
                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания папок: {ex.Message}");
            }
        }

        public async Task<string> SaveAvatarAsync(Stream imageStream, string fileName)
        {
            try
            {
                var avatarsPath = Path.Combine(FileSystem.AppDataDirectory, _avatarsFolder);
                var fullPath = Path.Combine(avatarsPath, fileName);

                using (var fileStream = File.Create(fullPath))
                {
                    await imageStream.CopyToAsync(fileStream);
                }

                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения аватара: {ex.Message}");
                throw;
            }
        }

        public async Task<string> SaveDocumentAsync(Stream documentStream, string fileName)
        {
            try
            {
                var documentsPath = Path.Combine(FileSystem.AppDataDirectory, _documentsFolder);
                var fullPath = Path.Combine(documentsPath, fileName);

                using (var fileStream = File.Create(fullPath))
                {
                    await documentStream.CopyToAsync(fileStream);
                }

                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения документа: {ex.Message}");
                throw;
            }
        }

        public async Task<string> SaveTempFileAsync(Stream fileStream, string fileName)
        {
            try
            {
                var tempPath = Path.Combine(FileSystem.AppDataDirectory, _tempFolder);
                var fullPath = Path.Combine(tempPath, fileName);

                using (var stream = File.Create(fullPath))
                {
                    await fileStream.CopyToAsync(stream);
                }

                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения временного файла: {ex.Message}");
                throw;
            }
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public void DeleteFile(string filePath)
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
                Console.WriteLine($"Ошибка удаления файла: {ex.Message}");
            }
        }

        public void DeleteTempFiles()
        {
            try
            {
                var tempPath = Path.Combine(FileSystem.AppDataDirectory, _tempFolder);
                if (Directory.Exists(tempPath))
                {
                    var files = Directory.GetFiles(tempPath);
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка очистки временных файлов: {ex.Message}");
            }
        }

        public long GetFileSize(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    return fileInfo.Length;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения размера файла: {ex.Message}");
                return 0;
            }
        }

        public string GetFileExtension(string fileName)
        {
            return Path.GetExtension(fileName).ToLower();
        }

        public bool IsValidImageFile(string fileName)
        {
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            var extension = GetFileExtension(fileName);
            return validExtensions.Contains(extension);
        }

        public bool IsValidDocumentFile(string fileName)
        {
            var validExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf" };
            var extension = GetFileExtension(fileName);
            return validExtensions.Contains(extension);
        }

        public string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{nameWithoutExtension}_{timestamp}{extension}";
        }
    }
}