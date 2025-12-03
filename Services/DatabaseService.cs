using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using EducationalPlatform.Models;
using Microsoft.Maui.Storage;
using System.Data.Common;

namespace EducationalPlatform.Services
{
    public class DatabaseService
    {
        private string _connectionString;
        public string ConnectionString => _connectionString;

        public DatabaseService()
        {
            _connectionString = "Server=EducationalPlatform.mssql.somee.com;Database=EducationalPlatform;User Id=yyullechkaaa_SQLLogin_1;Password=xtbnfhvyqu;TrustServerCertificate=true;";
        }

        // Проверка: состоит ли пользователь в какой-либо активной учебной группе
        public async Task<bool> IsUserInAnyActiveGroupAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
SELECT COUNT(*)
FROM GroupMembers gm
JOIN StudyGroup g ON gm.GroupId = g.GroupId
WHERE gm.UserId = @UserId AND g.IsActive = 1";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                var result = await cmd.ExecuteScalarAsync();
                int count = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
                return count > 0;
            }
            catch
            {
                // Если неизвестна схема таблиц, не блокируем участие
                return true;
            }
        }

        // СИСТЕМА СОГЛАСИЙ
        public async Task<bool> CheckUserPrivacyConsentAsync(int userId)
        {
            try
            {
                // Сначала проверяем SecureStorage (привязано к учетной записи, работает на всех устройствах)
                var secureConsent = await SecureStorage.GetAsync($"PrivacyConsent_{userId}");
                if (secureConsent == "true")
                {
                    return true;
                }

                // Затем проверяем базу данных (для синхронизации)
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT COUNT(*) 
            FROM PrivacyConsent 
            WHERE UserId = @UserId AND IsActive = 1";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                bool hasDbConsent = Convert.ToInt32(result) > 0;

                // Если согласие есть в БД, но нет в SecureStorage - синхронизируем
                if (hasDbConsent)
                {
                    await SecureStorage.SetAsync($"PrivacyConsent_{userId}", "true");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking privacy consent: {ex.Message}");

                // Если ошибка, проверяем SecureStorage как fallback
                try
                {
                    var secureConsent = await SecureStorage.GetAsync($"PrivacyConsent_{userId}");
                    return secureConsent == "true";
                }
                catch
                {
                    return false;
                }
            }
        }
        public async Task<bool> SaveLessonAttachmentAsync(int lessonId, string fileName, string filePath, string fileType, long fileSizeBytes)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO LessonAttachments (LessonId, FileName, FilePath, FileType, FileSize, UploadDate, IsActive)
            VALUES (@LessonId, @FileName, @FilePath, @FileType, @FileSize, GETDATE(), 1)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LessonId", lessonId);
                command.Parameters.AddWithValue("@FileName", fileName);
                command.Parameters.AddWithValue("@FilePath", filePath);
                command.Parameters.AddWithValue("@FileType", fileType);
                command.Parameters.AddWithValue("@FileSize", FormatFileSize(fileSizeBytes));

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения вложения: {ex.Message}");
                return false;
            }
        }

        private bool _lessonAttachmentSchemaEnsured;

        private async Task EnsureLessonAttachmentSchemaAsync()
        {
            if (_lessonAttachmentSchemaEnsured) return;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var alterQuery = @"
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'LessonAttachments' 
      AND COLUMN_NAME = 'FilePath'
      AND (CHARACTER_MAXIMUM_LENGTH IS NULL OR CHARACTER_MAXIMUM_LENGTH <> -1)
)
BEGIN
    ALTER TABLE LessonAttachments ALTER COLUMN FilePath NVARCHAR(MAX);
END";

                using var alterCommand = new SqlCommand(alterQuery, connection);
                await alterCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Не удалось обновить схему вложений: {ex.Message}");
            }

            _lessonAttachmentSchemaEnsured = true;
        }

        private string BuildDataUrl(byte[] bytes, string extension)
        {
            var sanitizedExtension = extension?.Trim().ToLower();
            if (string.IsNullOrEmpty(sanitizedExtension) || !sanitizedExtension.StartsWith('.'))
            {
                sanitizedExtension = ".bin";
            }

            var mimeType = sanitizedExtension switch
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
                ".txt" => "text/plain",
                ".mp4" => "video/mp4",
                _ => "application/octet-stream"
            };

            var base64 = Convert.ToBase64String(bytes);
            return $"data:{mimeType};base64,{base64}";
        }

        // Улучшенный метод для добавления вложений уроков
        // Улучшенный метод для добавления вложений (как в чатах)
        public async Task<LessonAttachment?> AddLessonAttachmentAsync(int lessonId, string fileName, string fileType, string fileSize, byte[] fileBytes)
        {
            try
            {
                Console.WriteLine($"📎 Сохраняем файл в БД: {fileName}, размер: {fileBytes.Length} байт");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Создаем data URL как в чатах
                var mimeType = GetMimeType(fileType);
                string base64File = Convert.ToBase64String(fileBytes);
                string filePathValue = $"data:{mimeType};base64,{base64File}";

                Console.WriteLine($"💾 Data URL создан, длина: {filePathValue.Length} символов");

                var query = @"
            INSERT INTO LessonAttachments (LessonId, FileName, FilePath, FileType, FileSize, UploadDate, IsActive)
            OUTPUT INSERTED.AttachmentId
            VALUES (@LessonId, @FileName, @FilePath, @FileType, @FileSize, GETDATE(), 1)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LessonId", lessonId);
                command.Parameters.AddWithValue("@FileName", fileName);
                command.Parameters.Add("@FilePath", SqlDbType.NVarChar, -1).Value = filePathValue;
                command.Parameters.AddWithValue("@FileType", fileType);
                command.Parameters.AddWithValue("@FileSize", fileSize);

                var result = await command.ExecuteScalarAsync();
                if (result == null)
                {
                    Console.WriteLine($"❌ Не удалось получить AttachmentId");
                    return null;
                }

                var attachmentId = Convert.ToInt32(result);
                Console.WriteLine($"✅ Файл сохранен в БД с ID: {attachmentId}");

                return new LessonAttachment
                {
                    AttachmentId = attachmentId,
                    LessonId = lessonId,
                    FileName = fileName,
                    FilePath = filePathValue,
                    FileType = fileType,
                    FileSize = fileSize,
                    UploadDate = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка добавления вложения: {ex.Message}");
                return null;
            }
        }

        // Метод для получения MIME типа (как в FileService)
        private string GetMimeType(string fileExtension)
        {
            var ext = fileExtension?.ToLower().TrimStart('.');
            return ext switch
            {
                "pdf" => "application/pdf",
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "ppt" => "application/vnd.ms-powerpoint",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "xls" => "application/vnd.ms-excel",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "zip" => "application/zip",
                "txt" => "text/plain",
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "gif" => "image/gif",
                "mp4" => "video/mp4",
                _ => "application/octet-stream"
            };
        }

        // Улучшенный метод получения вложений
        public async Task<List<LessonAttachment>> GetLessonAttachmentsAsync(int lessonId)
        {
            var attachments = new List<LessonAttachment>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT AttachmentId, LessonId, FileName, FilePath, FileType, FileSize, UploadDate
            FROM LessonAttachments
            WHERE LessonId = @LessonId AND IsActive = 1
            ORDER BY UploadDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LessonId", lessonId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    attachments.Add(new LessonAttachment
                    {
                        AttachmentId = reader.GetInt32("AttachmentId"),
                        LessonId = reader.GetInt32("LessonId"),
                        FileName = reader.GetString("FileName"),
                        FilePath = reader.GetString("FilePath"),
                        FileType = reader.GetString("FileType"),
                        FileSize = reader.GetString("FileSize"),
                        UploadDate = reader.GetDateTime("UploadDate")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки вложений: {ex.Message}");
            }
            return attachments;
        }
        public async Task<bool> DeleteLessonAttachmentAsync(int attachmentId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE LessonAttachments 
            SET IsActive = 0 
            WHERE AttachmentId = @AttachmentId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AttachmentId", attachmentId);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления вложения: {ex.Message}");
                return false;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        // В класс DatabaseService добавьте эти методы:
        public async Task<List<PracticeSubmission>> GetPracticeSubmissionsAsync(int lessonId)
        {
            var submissions = new List<PracticeSubmission>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT ps.SubmissionId, ps.StudentId, ps.SubmissionText, ps.SubmissionFileUrl, 
                   ps.SubmissionDate, ps.TeacherScore, ps.TeacherComment, ps.Status,
                   u.FirstName + ' ' + u.LastName as StudentName
            FROM PracticeSubmissions ps
            JOIN Users u ON ps.StudentId = u.UserId
            WHERE ps.LessonId = @LessonId
            ORDER BY ps.SubmissionDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LessonId", lessonId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    submissions.Add(new PracticeSubmission
                    {
                        SubmissionId = reader.GetInt32("SubmissionId"),
                        StudentId = reader.GetInt32("StudentId"),
                        SubmissionText = reader.IsDBNull("SubmissionText") ? null : reader.GetString("SubmissionText"),
                        SubmissionFileUrl = reader.IsDBNull("SubmissionFileUrl") ? null : reader.GetString("SubmissionFileUrl"),
                        SubmissionDate = reader.GetDateTime("SubmissionDate"),
                        TeacherScore = reader.IsDBNull("TeacherScore") ? null : reader.GetInt32("TeacherScore"),
                        TeacherComment = reader.IsDBNull("TeacherComment") ? null : reader.GetString("TeacherComment"),
                        Status = reader.GetString("Status"),
                        StudentName = reader.GetString("StudentName")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки решений: {ex.Message}");
            }
            return submissions;
        }



        public async Task<bool> SavePracticeSubmissionAsync(int lessonId, int studentId, string? submissionText, string? submissionFileUrl)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO PracticeSubmissions (LessonId, StudentId, SubmissionText, SubmissionFileUrl, SubmissionDate, Status)
            VALUES (@LessonId, @StudentId, @SubmissionText, @SubmissionFileUrl, GETDATE(), 'submitted')";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LessonId", lessonId);
                command.Parameters.AddWithValue("@StudentId", studentId);
                command.Parameters.AddWithValue("@SubmissionText", (object?)submissionText ?? DBNull.Value);
                command.Parameters.AddWithValue("@SubmissionFileUrl", (object?)submissionFileUrl ?? DBNull.Value);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения решения: {ex.Message}");
                return false;
            }
        }

        public async Task<PracticeAttachment?> AddPracticeAttachmentAsync(int practiceId, string fileName, string fileType, string fileSize, byte[] fileBytes)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Создаем data URL как для теории
                var mimeType = GetMimeType(fileType);
                string base64File = Convert.ToBase64String(fileBytes);
                string filePathValue = $"data:{mimeType};base64,{base64File}";

                var query = @"
            INSERT INTO PracticeAttachments (PracticeId, FileName, FilePath, FileType, FileSize, UploadDate, IsActive)
            OUTPUT INSERTED.AttachmentId
            VALUES (@PracticeId, @FileName, @FilePath, @FileType, @FileSize, GETDATE(), 1)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PracticeId", practiceId);
                command.Parameters.AddWithValue("@FileName", fileName);
                command.Parameters.Add("@FilePath", SqlDbType.NVarChar, -1).Value = filePathValue;
                command.Parameters.AddWithValue("@FileType", fileType);
                command.Parameters.AddWithValue("@FileSize", fileSize);

                var result = await command.ExecuteScalarAsync();
                if (result == null) return null;

                return new PracticeAttachment
                {
                    AttachmentId = Convert.ToInt32(result),
                    PracticeId = practiceId,
                    FileName = fileName,
                    FilePath = filePathValue,
                    FileType = fileType,
                    FileSize = fileSize,
                    UploadDate = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления вложения практики: {ex.Message}");
                return null;
            }
        }



        public async Task<List<PracticeAttachment>> GetPracticeAttachmentsAsync(int practiceId)
        {
            var attachments = new List<PracticeAttachment>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT AttachmentId, PracticeId, FileName, FilePath, FileType, FileSize, UploadDate
            FROM PracticeAttachments
            WHERE PracticeId = @PracticeId AND IsActive = 1
            ORDER BY UploadDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PracticeId", practiceId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    attachments.Add(new PracticeAttachment
                    {
                        AttachmentId = reader.GetInt32("AttachmentId"),
                        PracticeId = reader.GetInt32("PracticeId"),
                        FileName = reader.GetString("FileName"),
                        FilePath = reader.GetString("FilePath"),
                        FileType = reader.GetString("FileType"),
                        FileSize = reader.GetString("FileSize"),
                        UploadDate = reader.GetDateTime("UploadDate")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки вложений практики: {ex.Message}");
            }
            return attachments;
        }

        public async Task<bool> GradePracticeSubmissionAsync(int submissionId, int teacherId, int score, string? comment)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE PracticeSubmissions 
            SET TeacherScore = @Score, TeacherComment = @Comment, Status = 'graded', GradedBy = @TeacherId, GradedAt = GETDATE()
            WHERE SubmissionId = @SubmissionId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SubmissionId", submissionId);
                command.Parameters.AddWithValue("@Score", score);
                command.Parameters.AddWithValue("@Comment", (object?)comment ?? DBNull.Value);
                command.Parameters.AddWithValue("@TeacherId", teacherId);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка оценивания решения: {ex.Message}");
                return false;
            }
        }


        // МЕТОДЫ ДЛЯ РАБОТЫ С АВАТАРКОЙ
        public async Task<bool> UpdateUserAvatarAsync(int userId, string avatarUrl)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE Users 
            SET AvatarUrl = @AvatarUrl, LastUpdated = GETDATE()
            WHERE UserId = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@AvatarUrl", avatarUrl);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating avatar: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> GetUserAvatarUrlAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT AvatarUrl 
            FROM Users 
            WHERE UserId = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting avatar URL: {ex.Message}");
                return null;
            }
        }

        private string? GetUserIPAddress()
        {
            try
            {
                var ipAddress = DeviceInfo.Platform == DevicePlatform.Android ?
                    "mobile_device" : "desktop_device";
                return ipAddress;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SavePrivacyConsentAsync(int userId, string consentText, string version)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Деактивируем предыдущие согласия пользователя
                var deactivateQuery = @"
            UPDATE PrivacyConsent 
            SET IsActive = 0 
            WHERE UserId = @UserId";

                using var deactivateCommand = new SqlCommand(deactivateQuery, connection);
                deactivateCommand.Parameters.AddWithValue("@UserId", userId);
                await deactivateCommand.ExecuteNonQueryAsync();

                // Создаем новое согласие с IPAddress
                var insertQuery = @"
            INSERT INTO PrivacyConsent (UserId, ConsentText, Version, ConsentDate, IPAddress, IsActive)
            VALUES (@UserId, @ConsentText, @Version, GETDATE(), @IPAddress, 1)";

                using var insertCommand = new SqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@UserId", userId);
                insertCommand.Parameters.AddWithValue("@ConsentText", consentText ?? string.Empty);
                insertCommand.Parameters.AddWithValue("@Version", version ?? "1.0");
                insertCommand.Parameters.AddWithValue("@IPAddress", GetUserIPAddress() ?? (object)DBNull.Value);

                var result = await insertCommand.ExecuteNonQueryAsync();
                
                // Обновляем поле HasPrivacyConsent в таблице Users для синхронизации между устройствами
                if (result > 0)
                {
                    var updateUserQuery = @"
                UPDATE Users 
                SET HasPrivacyConsent = 1 
                WHERE UserId = @UserId";
                    
                    using var updateCommand = new SqlCommand(updateUserQuery, connection);
                    updateCommand.Parameters.AddWithValue("@UserId", userId);
                    await updateCommand.ExecuteNonQueryAsync();
                }
                
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving privacy consent: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddGameCurrencyAsync(int userId, int amount, string reason)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE Users SET GameCurrency = ISNULL(GameCurrency, 0) + @Amount 
            WHERE UserId = @UserId;
            
            INSERT INTO CurrencyTransactions (UserId, Amount, TransactionType, Reason, TransactionDate)
            VALUES (@UserId, @Amount, 'income', @Reason, GETDATE())";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@Reason", reason ?? "bonus");

                var result = await command.ExecuteNonQueryAsync();

                if (result > 0)
                {
                    Console.WriteLine($"✅ Начислено {amount} монет пользователю {userId} по причине: {reason}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось начислить монеты пользователю {userId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка начисления валюты: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "UPDATE Users SET IsActive = 0 WHERE UserId = @UserId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка деактивации пользователя: {ex.Message}");
                return false;
            }
        }

        // ОСТАЛЬНЫЕ МЕТОДЫ
        public async Task<string> GetRandomLoginGreetingAsync(string languageCode = "ru", bool forTeens = false)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT TOP 1 MessageText 
                    FROM LoginGreetings 
                    WHERE LanguageCode = @LanguageCode AND ForTeens = @ForTeens AND IsActive = 1
                    ORDER BY NEWID()";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LanguageCode", languageCode);
                command.Parameters.AddWithValue("@ForTeens", forTeens);

                var result = await command.ExecuteScalarAsync();
                return result?.ToString() ?? "Добро пожаловать!";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения приветствия: {ex.Message}");
                return "Добро пожаловать!";
            }
        }

        // СИСТЕМА МАГАЗИНА
        public async Task<List<ShopItem>> GetShopItemsAsync()
        {
            var items = new List<ShopItem>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT ItemId, Name, Description, Price, ItemType, Icon
                    FROM ShopItems 
                    WHERE IsActive = 1
                    ORDER BY ItemType, Price";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    items.Add(new ShopItem
                    {
                        ItemId = reader.GetInt32("ItemId"),
                        Name = reader.GetString("Name"),
                        Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                        Price = reader.GetInt32("Price"),
                        ItemType = reader.GetString("ItemType"),
                        Icon = reader.IsDBNull("Icon") ? "" : reader.GetString("Icon")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки товаров: {ex.Message}");
            }
            return items;
        }

        public async Task<bool> CheckItemOwnershipAsync(int userId, int itemId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM UserInventory WHERE UserId = @UserId AND ItemId = @ItemId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@ItemId", itemId);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки владения: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckItemEquippedAsync(int userId, int itemId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM UserInventory WHERE UserId = @UserId AND ItemId = @ItemId AND IsEquipped = 1";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@ItemId", itemId);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки экипировки: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PurchaseShopItemAsync(int userId, int itemId, int price)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Начинаем транзакцию
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. Проверяем баланс
                    var balanceQuery = "SELECT GameCurrency FROM Users WHERE UserId = @UserId";
                    using var balanceCommand = new SqlCommand(balanceQuery, connection, transaction);
                    balanceCommand.Parameters.AddWithValue("@UserId", userId);
                    var balance = Convert.ToInt32(await balanceCommand.ExecuteScalarAsync());

                    if (balance < price)
                        return false;

                    // 2. Списываем средства
                    var deductQuery = "UPDATE Users SET GameCurrency = GameCurrency - @Price WHERE UserId = @UserId";
                    using var deductCommand = new SqlCommand(deductQuery, connection, transaction);
                    deductCommand.Parameters.AddWithValue("@Price", price);
                    deductCommand.Parameters.AddWithValue("@UserId", userId);
                    await deductCommand.ExecuteNonQueryAsync();

                    // 3. Добавляем товар в инвентарь
                    var inventoryQuery = @"
                        INSERT INTO UserInventory (UserId, ItemId, PurchaseDate, IsEquipped)
                        VALUES (@UserId, @ItemId, GETDATE(), 0)";
                    using var inventoryCommand = new SqlCommand(inventoryQuery, connection, transaction);
                    inventoryCommand.Parameters.AddWithValue("@UserId", userId);
                    inventoryCommand.Parameters.AddWithValue("@ItemId", itemId);
                    await inventoryCommand.ExecuteNonQueryAsync();

                    // 4. Фиксируем транзакцию
                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка покупки товара: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EquipShopItemAsync(int userId, int itemId, string itemType)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Сначала снимаем все предметы этого типа
                var unequipQuery = @"
                    UPDATE UserInventory 
                    SET IsEquipped = 0 
                    WHERE UserId = @UserId 
                    AND ItemId IN (SELECT ItemId FROM ShopItems WHERE ItemType = @ItemType)";
                using var unequipCommand = new SqlCommand(unequipQuery, connection);
                unequipCommand.Parameters.AddWithValue("@UserId", userId);
                unequipCommand.Parameters.AddWithValue("@ItemType", itemType);
                await unequipCommand.ExecuteNonQueryAsync();

                // Затем надеваем выбранный предмет
                var equipQuery = "UPDATE UserInventory SET IsEquipped = 1 WHERE UserId = @UserId AND ItemId = @ItemId";
                using var equipCommand = new SqlCommand(equipQuery, connection);
                equipCommand.Parameters.AddWithValue("@UserId", userId);
                equipCommand.Parameters.AddWithValue("@ItemId", itemId);

                var result = await equipCommand.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка экипировки товара: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UnequipShopItemAsync(int userId, string itemType)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    UPDATE UserInventory 
                    SET IsEquipped = 0 
                    WHERE UserId = @UserId 
                    AND ItemId IN (SELECT ItemId FROM ShopItems WHERE ItemType = @ItemType)";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@ItemType", itemType);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка снятия товара: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ShopItem>> GetUserInventoryAsync(int userId)
        {
            var items = new List<ShopItem>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT ui.ItemId, si.Name, si.Description, si.Price, si.ItemType, si.Icon, ui.IsEquipped
                    FROM UserInventory ui
                    JOIN ShopItems si ON ui.ItemId = si.ItemId
                    WHERE ui.UserId = @UserId
                    ORDER BY si.ItemType, si.Name";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new ShopItem
                    {
                        ItemId = reader.GetInt32("ItemId"),
                        Name = reader.GetString("Name"),
                        Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                        Price = reader.GetInt32("Price"),
                        ItemType = reader.GetString("ItemType"),
                        Icon = reader.IsDBNull("Icon") ? "" : reader.GetString("Icon"),
                        IsPurchased = true,
                        IsEquipped = reader.GetBoolean("IsEquipped")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки инвентаря: {ex.Message}");
            }
            return items;
        }

        public async Task<int> GetUserGameCurrencyAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT ISNULL(GameCurrency,0) FROM Users WHERE UserId = @UserId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения баланса: {ex.Message}");
                return 0;
            }
        }

        public async Task<double> GetOverallLearningProgressAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        CAST(
                            CASE WHEN COUNT(*) = 0 THEN 0 
                                 ELSE 100.0 * SUM(CASE WHEN Status = 'completed' THEN 1 ELSE 0 END) / COUNT(*) 
                            END AS FLOAT)
                    FROM StudentProgress WHERE StudentId = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                var result = await command.ExecuteScalarAsync();
                return result == null ? 0 : Convert.ToDouble(result) / 100.0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка расчета общего прогресса: {ex.Message}");
                return 0;
            }
        }

        // СИСТЕМА СТАТИСТИКИ
        public async Task<UserStatistics> GetUserStatisticsAsync(int userId)
        {
            try
            {
                Console.WriteLine($"📊 Загружаем статистику для пользователя {userId}");
                
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        (SELECT COUNT(*) FROM StudentProgress WHERE StudentId = @UserId) as TotalCourses,
                        (SELECT COUNT(*) FROM StudentProgress WHERE StudentId = @UserId AND Status = 'completed') as CompletedCourses,
                        (SELECT ISNULL(AVG(CAST(Score AS FLOAT)), 0) FROM StudentProgress WHERE StudentId = @UserId AND Score IS NOT NULL) as AverageScore,
                        (SELECT ISNULL(StreakDays, 0) FROM Users WHERE UserId = @UserId) as CurrentStreak,
                        (SELECT ISNULL(MAX(StreakDays), 0) FROM Users WHERE UserId = @UserId) as LongestStreak,
                        (SELECT ISNULL(DATEDIFF(day, RegistrationDate, GETDATE()), 0) FROM Users WHERE UserId = @UserId) as TotalDays,
                        (SELECT ISNULL(SUM(DATEDIFF(hour, StartDate, ISNULL(CompletionDate, GETDATE()))), 0) FROM StudentProgress WHERE StudentId = @UserId) as TotalTimeSpent";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var totalCourses = reader.IsDBNull("TotalCourses") ? 0 : reader.GetInt32("TotalCourses");
                    var completedCourses = reader.IsDBNull("CompletedCourses") ? 0 : reader.GetInt32("CompletedCourses");
                    var averageScore = reader.IsDBNull("AverageScore") ? 0.0 : reader.GetDouble("AverageScore");
                    var currentStreak = reader.IsDBNull("CurrentStreak") ? 0 : reader.GetInt32("CurrentStreak");
                    var longestStreak = reader.IsDBNull("LongestStreak") ? 0 : reader.GetInt32("LongestStreak");
                    var totalDays = reader.IsDBNull("TotalDays") ? 0 : reader.GetInt32("TotalDays");
                    var totalTimeSpent = reader.IsDBNull("TotalTimeSpent") ? 0 : reader.GetInt32("TotalTimeSpent");

                    var statistics = new UserStatistics
                    {
                        TotalCourses = totalCourses,
                        CompletedCourses = completedCourses,
                        TotalTimeSpent = totalTimeSpent,
                        AverageScore = averageScore,
                        CompletionRate = totalCourses > 0 ? (double)completedCourses / totalCourses : 0.0,
                        CurrentStreak = currentStreak,
                        LongestStreak = longestStreak,
                        TotalDays = totalDays
                    };
                    
                    Console.WriteLine($"✅ Статистика загружена: курсов {totalCourses}, завершено {completedCourses}, средний балл {averageScore:F1}");
                    return statistics;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки статистики: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine($"⚠️ Возвращаем пустую статистику");
            return new UserStatistics();
        }

        public async Task<List<Achievement>> GetRecentAchievementsAsync(int userId, int count)
        {
            var achievements = new List<Achievement>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT TOP (@Count) a.AchievementId, a.Name, a.Description, a.IconUrl, ua.EarnedDate
                    FROM UserAchievements ua
                    JOIN Achievements a ON ua.AchievementId = a.AchievementId
                    WHERE ua.UserId = @UserId
                    ORDER BY ua.EarnedDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Count", count);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    achievements.Add(new Achievement
                    {
                        AchievementId = reader.GetInt32("AchievementId"),
                        Name = reader.GetString("Name"),
                        Description = reader.GetString("Description"),
                        Icon = reader.IsDBNull("IconUrl") ? "🏆" : reader.GetString("IconUrl"),
                        EarnedDate = reader.GetDateTime("EarnedDate")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки достижений: {ex.Message}");

                // Резервная реализация с тестовыми данными
                if (achievements.Count == 0)
                {
                    achievements.Add(new Achievement
                    {
                        Icon = "🏆",
                        Name = "Первый курс",
                        Description = "Завершил первый курс",
                        EarnedDate = DateTime.Now.AddDays(-5)
                    });
                    achievements.Add(new Achievement
                    {
                        Icon = "🔥",
                        Name = "Серия 7 дней",
                        Description = "Входил 7 дней подряд",
                        EarnedDate = DateTime.Now.AddDays(-2)
                    });
                }
            }
            return achievements;
        }

        // АВАТАРЫ
        public async Task<string?> UploadAvatarAsync(Stream imageStream, string fileName, int userId)
        {
            byte[] imageBytes = Array.Empty<byte>();

            try
            {
                Console.WriteLine($"📸 Начинаем загрузку аватара для пользователя {userId}, файл: {fileName}");
                
                // Читаем изображение в массив байтов
                using var memoryStream = new MemoryStream();
                await imageStream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
                
                Console.WriteLine($"📦 Размер изображения: {imageBytes.Length} байт");

                // Сохраняем файл в папку приложения
                var avatarsFolder = Path.Combine(FileSystem.AppDataDirectory, "Avatars");
                if (!Directory.Exists(avatarsFolder))
                {
                    Directory.CreateDirectory(avatarsFolder);
                    Console.WriteLine($"📁 Создана папка для аватаров: {avatarsFolder}");
                }

                // Определяем расширение файла (поддерживаем любой формат)
                var fileExtension = Path.GetExtension(fileName);
                if (string.IsNullOrEmpty(fileExtension))
                {
                    // Если расширение не указано, пытаемся определить по содержимому или используем .jpg
                    fileExtension = ".jpg";
                }
                
                var newFileName = $"avatar_{userId}{fileExtension}";
                var fullPath = Path.Combine(avatarsFolder, newFileName);

                // Сохраняем файл локально
                await File.WriteAllBytesAsync(fullPath, imageBytes);
                Console.WriteLine($"✅ Аватар сохранен локально: {fullPath}");

                // Определяем MIME тип для любого формата изображения
                var mimeType = fileExtension.ToLower().TrimStart('.') switch
                {
                    "jpg" or "jpeg" => "image/jpeg",
                    "png" => "image/png",
                    "gif" => "image/gif",
                    "webp" => "image/webp",
                    "bmp" => "image/bmp",
                    "svg" => "image/svg+xml",
                    "ico" => "image/x-icon",
                    "tiff" or "tif" => "image/tiff",
                    _ => "image/jpeg" // По умолчанию JPEG
                };
                
                // Сохраняем base64 в БД для синхронизации между устройствами
                // Убираем ограничение на размер - сохраняем любой размер
                string base64Image = Convert.ToBase64String(imageBytes);
                string avatarDataUrl = $"data:{mimeType};base64,{base64Image}";
                
                Console.WriteLine($"💾 Размер base64 строки: {avatarDataUrl.Length} символов");

                // Сохраняем в БД
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "UPDATE Users SET AvatarUrl = @AvatarUrl WHERE UserId = @UserId";
                using var command = new SqlCommand(query, connection);
                var param = command.Parameters.Add("@AvatarUrl", System.Data.SqlDbType.NVarChar, -1);
                param.Value = avatarDataUrl; // Сохраняем base64 для синхронизации
                command.Parameters.AddWithValue("@UserId", userId);

                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    Console.WriteLine($"✅ Аватар успешно сохранен в БД для пользователя {userId}");
                    return avatarDataUrl; // Возвращаем data URL для немедленного использования
                }
                else
                {
                    Console.WriteLine($"⚠️ Пользователь {userId} не найден в БД, но файл сохранен локально");
                    return avatarDataUrl; // Все равно возвращаем data URL
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки аватара: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Пытаемся сохранить хотя бы локально
                try
                {
                    var avatarsFolder = Path.Combine(FileSystem.AppDataDirectory, "Avatars");
                    if (!Directory.Exists(avatarsFolder))
                    {
                        Directory.CreateDirectory(avatarsFolder);
                    }
                    var fileExtension = Path.GetExtension(fileName) ?? ".jpg";
                    var newFileName = $"avatar_{userId}{fileExtension}";
                    var fullPath = Path.Combine(avatarsFolder, newFileName);
                    await File.WriteAllBytesAsync(fullPath, imageBytes);
                    Console.WriteLine($"⚠️ Аватар сохранен только локально из-за ошибки БД: {fullPath}");
                    return fullPath;
                }
                catch (Exception localEx)
                {
                    Console.WriteLine($"❌ Не удалось сохранить аватар даже локально: {localEx.Message}");
                    return null;
                }
            }
        }

        private async Task<string?> SaveAvatarAsFile(byte[] imageBytes, string fileName, int userId)
        {
            try
            {
                Console.WriteLine($"⚠️ Пытаемся сохранить аватар как файл (размер: {imageBytes.Length} байт)");
                
                // Пробуем сжать изображение перед сохранением в base64
                // Если размер все еще слишком большой, сохраняем как файл
                // Но для кроссплатформенности лучше всегда использовать base64
                
                // Пробуем уменьшить размер изображения
                byte[] compressedBytes = imageBytes;
                int maxSize = 2 * 1024 * 1024; // 2 МБ максимум для base64
                
                if (imageBytes.Length > maxSize)
                {
                    Console.WriteLine($"⚠️ Изображение слишком большое ({imageBytes.Length} байт), пробуем сжать...");
                    // Здесь можно добавить сжатие изображения, но для простоты сохраняем как есть
                    // В реальном приложении можно использовать библиотеку для сжатия изображений
                }

                // Конвертируем в base64 даже для больших файлов
                string base64Image = Convert.ToBase64String(compressedBytes);
                var fileExtension = Path.GetExtension(fileName).TrimStart('.');
                string mimeType = fileExtension.ToLower() switch
                {
                    "jpg" or "jpeg" => "image/jpeg",
                    "png" => "image/png",
                    "gif" => "image/gif",
                    "webp" => "image/webp",
                    _ => "image/jpeg"
                };

                string avatarDataUrl = $"data:{mimeType};base64,{base64Image}";

                // Пробуем сохранить в БД
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "UPDATE Users SET AvatarUrl = @AvatarUrl WHERE UserId = @UserId";
                using var command = new SqlCommand(query, connection);
                var param = command.Parameters.Add("@AvatarUrl", System.Data.SqlDbType.NVarChar, -1);
                param.Value = avatarDataUrl;
                command.Parameters.AddWithValue("@UserId", userId);

                try
                {
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"✅ Аватар сохранен в БД как base64 (размер: {compressedBytes.Length} байт, base64 длина: {avatarDataUrl.Length})");
                        return avatarDataUrl;
                    }
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Number == 8152 || sqlEx.Message.Contains("String or binary data would be truncated"))
                    {
                        Console.WriteLine($"❌ Данные все еще слишком большие для БД даже после сжатия");
                        // В крайнем случае сохраняем локально, но это не будет работать на других устройствах
                        var avatarsFolder = Path.Combine(FileSystem.AppDataDirectory, "Avatars");
                        if (!Directory.Exists(avatarsFolder))
                        {
                            Directory.CreateDirectory(avatarsFolder);
                        }

                        var newFileName = $"avatar_{userId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(fileName)}";
                        var fullPath = Path.Combine(avatarsFolder, newFileName);
                        await File.WriteAllBytesAsync(fullPath, imageBytes);

                        // Сохраняем путь в БД
                        var pathCommand = new SqlCommand("UPDATE Users SET AvatarUrl = @AvatarUrl WHERE UserId = @UserId", connection);
                        pathCommand.Parameters.AddWithValue("@AvatarUrl", fullPath);
                        pathCommand.Parameters.AddWithValue("@UserId", userId);
                        await pathCommand.ExecuteNonQueryAsync();

                        Console.WriteLine($"⚠️ Аватар сохранен локально: {fullPath} (НЕ будет работать на других устройствах!)");
                        return fullPath;
                    }
                    throw;
                }

                return avatarDataUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения аватара как файла: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        private async Task<string?> SaveAvatarAsync(Stream fileStream, string fileName, int userId)
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

                Console.WriteLine($"Аватар сохранен: {fullPath}");
                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения аватара: {ex.Message}");
                return null;
            }
        }

        public string? GetAvatarPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            if (File.Exists(fileName))
                return fileName;

            return null;
        }

        public async Task<string?> GetUserAvatarAsync(int userId)
        {
            try
            {
                // 1. ВСЕГДА сначала берем самое актуальное значение из БД
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT AvatarUrl FROM Users WHERE UserId = @UserId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                var avatarData = result?.ToString();

                if (!string.IsNullOrEmpty(avatarData))
                {
                    Console.WriteLine($"🔍 Получен актуальный аватар из БД для пользователя {userId}");
                    return avatarData; // base64 data URL или путь/URL — UI сам разберётся через ServiceHelper
                }

                Console.WriteLine($"⚠️ Аватар в БД не найден для пользователя {userId}, пробуем локальный кэш");

                // 2. Если в БД ничего нет – пробуем локальный кэш (офлайн режим)
                var avatarsFolder = Path.Combine(FileSystem.AppDataDirectory, "Avatars");
                if (Directory.Exists(avatarsFolder))
                {
                    var localFiles = Directory.GetFiles(avatarsFolder, $"avatar_{userId}.*");
                    if (localFiles.Length > 0)
                    {
                        var localPath = localFiles[0];
                        if (File.Exists(localPath))
                        {
                            Console.WriteLine($"✅ Локальный аватар найден: {localPath}");
                            return localPath;
                        }
                    }
                }

                return null;

                // Если это путь к файлу
                if (File.Exists(avatarData))
                {
                    return avatarData;
                }

                // Если путь относительный
                var relativePath = Path.Combine(FileSystem.AppDataDirectory, avatarData);
                if (File.Exists(relativePath))
                {
                    return relativePath;
                }

                Console.WriteLine($"⚠️ Аватар не найден для пользователя {userId}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения аватара: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        // АУТЕНТИФИКАЦИЯ И ПОЛЬЗОВАТЕЛИ
        public async Task<bool> TestConnection()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckUserExistsAsync(string username, string email)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT COUNT(*) FROM Users 
                    WHERE Username = @Username OR Email = @Email";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username ?? "");
                command.Parameters.AddWithValue("@Email", email ?? "");

                var result = await command.ExecuteScalarAsync();
                var count = result is null ? 0 : (int)result;
                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки пользователя: {ex.Message}");
                return true;
            }
        }

        public async Task<List<Role>> GetRolesAsync()
        {
            var roles = new List<Role>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"SELECT RoleId, RoleName, Description, CanCreateCourses, CanManageUsers, 
                            CanManageSystem, CanViewAllData, CanTakeCourses, CanJoinGroups, 
                            CanPurchaseItems, CanManageGroups, CanGradeTests, CanGenerateReports, 
                            CanManageContent, CanPublishNews, CanModerateReviews FROM Roles WHERE RoleName != 'Admin' ORDER BY RoleId";
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    roles.Add(new Role
                    {
                        RoleId = reader.GetInt32("RoleId"),
                        RoleName = reader.GetString("RoleName"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        CanCreateCourses = reader.GetBoolean("CanCreateCourses"),
                        CanManageUsers = reader.GetBoolean("CanManageUsers"),
                        CanManageSystem = reader.GetBoolean("CanManageSystem"),
                        CanViewAllData = reader.GetBoolean("CanViewAllData"),
                        CanTakeCourses = reader.GetBoolean("CanTakeCourses"),
                        CanJoinGroups = reader.GetBoolean("CanJoinGroups"),
                        CanPurchaseItems = reader.GetBoolean("CanPurchaseItems"),
                        CanManageGroups = reader.GetBoolean("CanManageGroups"),
                        CanGradeTests = reader.GetBoolean("CanGradeTests"),
                        CanGenerateReports = reader.GetBoolean("CanGenerateReports"),
                        CanManageContent = reader.GetBoolean("CanManageContent"),
                        CanPublishNews = reader.GetBoolean("CanPublishNews"),
                        CanModerateReviews = reader.GetBoolean("CanModerateReviews")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки ролей: {ex.Message}");
            }

            return roles;
        }

        public async Task<bool> RegisterUserAsync(string username, string email, string password, string firstName, string lastName)
        {
            try
            {
                if (await CheckUserExistsAsync(username, email))
                    return false;

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, RoleId, LanguagePref, RegistrationDate)
                    VALUES (@Username, @Email, @PasswordHash, @FirstName, @LastName, 1, 'ru', GETDATE())";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username ?? "");
                command.Parameters.AddWithValue("@Email", email ?? "");
                command.Parameters.AddWithValue("@PasswordHash", HashPassword(password ?? ""));
                command.Parameters.AddWithValue("@FirstName", firstName ?? "");
                command.Parameters.AddWithValue("@LastName", lastName ?? "");

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка регистрации: {ex.Message}");
                return false;
            }
        }

        public async Task<User?> LoginAsync(string? username, string? password)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT u.UserId, u.Username, u.Email, u.FirstName, u.LastName, u.RoleId,
                           u.LanguagePref, u.InterfaceStyle, u.GameCurrency, u.StreakDays, 
                           u.RegistrationDate, u.IsActive, u.AvatarUrl, u.LastLoginDate,
                           r.RoleName
                    FROM Users u
                    LEFT JOIN Roles r ON u.RoleId = r.RoleId
                    WHERE u.Username = @Username AND u.PasswordHash = @PasswordHash AND u.IsActive = 1";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username ?? "");
                command.Parameters.AddWithValue("@PasswordHash", HashPassword(password ?? ""));

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var userId = reader.GetInt32("UserId");
                    var hasConsent = await CheckUserPrivacyConsentAsync(userId);

                    return new User
                    {
                        UserId = userId,
                        Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? null : reader.GetString("Username"),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString("Email"),
                        FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString("FirstName"),
                        LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString("LastName"),
                        AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl")) ? null : reader.GetString("AvatarUrl"),
                        RoleId = reader.GetInt32("RoleId"),
                        RoleName = reader.IsDBNull(reader.GetOrdinal("RoleName")) ? null : reader.GetString("RoleName"),
                        LanguagePref = reader.IsDBNull(reader.GetOrdinal("LanguagePref")) ? "ru" : reader.GetString("LanguagePref"),
                        InterfaceStyle = reader.IsDBNull(reader.GetOrdinal("InterfaceStyle")) ? "standard" : reader.GetString("InterfaceStyle"),
                        GameCurrency = reader.GetInt32("GameCurrency"),
                        StreakDays = reader.GetInt32("StreakDays"),
                        RegistrationDate = reader.GetDateTime("RegistrationDate"),
                        LastLoginDate = reader.IsDBNull(reader.GetOrdinal("LastLoginDate")) ? null : reader.GetDateTime("LastLoginDate"),
                        IsActive = reader.GetBoolean("IsActive"),
                        HasPrivacyConsent = hasConsent
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка входа: {ex.Message}");
                return null;
            }
        }

        public async Task UpdateLoginStreakAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Получаем текущую серию и время последнего входа (UTC)
                var getQuery = @"SELECT StreakDays, LastLoginDate FROM Users WHERE UserId = @UserId";
                using var getCmd = new SqlCommand(getQuery, connection);
                getCmd.Parameters.AddWithValue("@UserId", userId);

                int currentStreak = 0;
                DateTime? lastLoginUtc = null;

                using (var r = await getCmd.ExecuteReaderAsync())
                {
                    if (await r.ReadAsync())
                    {
                        currentStreak = r.IsDBNull(r.GetOrdinal("StreakDays")) ? 0 : r.GetInt32(r.GetOrdinal("StreakDays"));
                        if (!r.IsDBNull(r.GetOrdinal("LastLoginDate")))
                        {
                            var lastLogin = r.GetDateTime(r.GetOrdinal("LastLoginDate"));
                            // Если дата хранится как Local, конвертируем в UTC
                            lastLoginUtc = lastLogin.Kind == DateTimeKind.Unspecified 
                                ? DateTime.SpecifyKind(lastLogin, DateTimeKind.Utc) 
                                : lastLogin.ToUniversalTime();
                        }
                    }
                }

                // Текущее время UTC
                var nowUtc = DateTime.UtcNow;
                bool increment = false;
                int newStreak = currentStreak;

                if (lastLoginUtc.HasValue)
                {
                    // Вычисляем разницу во времени
                    var timeSinceLastLogin = nowUtc - lastLoginUtc.Value;

                    // Если прошло более 24 часов с последнего визита - увеличиваем стрик
                    if (timeSinceLastLogin.TotalHours >= 24.0)
                    {
                        // Проверяем, не прошло ли более 48 часов (пропущен день)
                        if (timeSinceLastLogin.TotalHours >= 48.0)
                    {
                            // Сбрасываем серию, начинаем заново
                            newStreak = 1;
                        }
                        else
                        {
                            // Увеличиваем серию
                            newStreak = currentStreak + 1;
                            increment = true;
                        }
                    }
                    // Если прошло менее 24 часов - не увеличиваем, сохраняем текущую серию
                    else
                    {
                        newStreak = currentStreak;
                        increment = false;
                    }
                }
                else
                {
                    // Первый вход - начинаем серию
                    newStreak = 1;
                    increment = false; // Не начисляем монеты за первый вход
                }

                // Обновляем серию и время последнего входа (сохраняем в UTC)
                var updateQuery = @"
            UPDATE Users 
            SET StreakDays = @NewStreak,
                LastLoginDate = @LastLoginUtc
            WHERE UserId = @UserId";

                using var updCmd = new SqlCommand(updateQuery, connection);
                updCmd.Parameters.AddWithValue("@NewStreak", newStreak);
                updCmd.Parameters.AddWithValue("@LastLoginUtc", nowUtc);
                updCmd.Parameters.AddWithValue("@UserId", userId);
                await updCmd.ExecuteNonQueryAsync();

                // Начисляем монеты только при увеличении серии
                if (increment && newStreak > 1)
                {
                    int reward = Math.Min((newStreak - 1) * 10, 50); // до 50 монет максимум
                    var rewardQuery = @"
                UPDATE Users SET GameCurrency = ISNULL(GameCurrency,0) + @Amount WHERE UserId = @UserId;
                INSERT INTO CurrencyTransactions (UserId, Amount, TransactionType, Reason, TransactionDate)
                VALUES (@UserId, @Amount, 'income', 'streak_reward', GETDATE());";

                    using var rwCmd = new SqlCommand(rewardQuery, connection);
                    rwCmd.Parameters.AddWithValue("@UserId", userId);
                    rwCmd.Parameters.AddWithValue("@Amount", reward);
                    await rwCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления серии: {ex.Message}");
            }
        }

        public async Task<bool> UpdateUserAsync(int userId, string firstName, string lastName, string username, string email, string? avatarUrl = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    UPDATE Users 
                    SET FirstName = @FirstName, 
                        LastName = @LastName, 
                        Username = @Username, 
                        Email = @Email,
                        AvatarUrl = @AvatarUrl
                    WHERE UserId = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FirstName", firstName ?? "");
                command.Parameters.AddWithValue("@LastName", lastName ?? "");
                command.Parameters.AddWithValue("@Username", username ?? "");
                command.Parameters.AddWithValue("@Email", email ?? "");
                command.Parameters.AddWithValue("@AvatarUrl", avatarUrl ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления пользователя: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckUserExistsAsync(string username, string email, int excludeUserId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT COUNT(*) FROM Users
                    WHERE (Username = @Username OR Email = @Email)
                    AND UserId != @ExcludeUserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username ?? "");
                command.Parameters.AddWithValue("@Email", email ?? "");
                command.Parameters.AddWithValue("@ExcludeUserId", excludeUserId);

                var result = await command.ExecuteScalarAsync();
                var count = result is null ? 0 : (int)result;

                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки пользователя: {ex.Message}");
                return true;
            }
        }

        // КУРСЫ
        public async Task<List<Course>> GetCoursesAsync()
        {
            var courses = new List<Course>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT c.CourseId, c.CourseName, c.Description, c.IsPublished, 
                           l.LanguageName, d.DifficultyName
                    FROM Courses c
                    LEFT JOIN ProgrammingLanguages l ON c.LanguageId = l.LanguageId
                    LEFT JOIN CourseDifficulties d ON c.DifficultyId = d.DifficultyId
                    WHERE c.IsPublished = 1";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    courses.Add(new Course
                    {
                        CourseId = reader.GetInt32("CourseId"),
                        CourseName = reader.IsDBNull(reader.GetOrdinal("CourseName")) ? "Название не указано" : reader.GetString("CourseName"),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "Описание отсутствует" : reader.GetString("Description"),
                        LanguageName = reader.IsDBNull(reader.GetOrdinal("LanguageName")) ? "Не указан" : reader.GetString("LanguageName"),
                        DifficultyName = reader.IsDBNull(reader.GetOrdinal("DifficultyName")) ? "Не указана" : reader.GetString("DifficultyName"),
                        IsPublished = reader.GetBoolean("IsPublished")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки курсов: {ex.Message}");
            }

            return courses;
        }

        public async Task<bool> UpdateProgressAsync(int userId, int courseId, string status)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    IF EXISTS (SELECT 1 FROM StudentProgress WHERE StudentId = @UserId AND CourseId = @CourseId)
                        UPDATE StudentProgress SET Status = @Status WHERE StudentId = @UserId AND CourseId = @CourseId
                    ELSE
                        INSERT INTO StudentProgress (StudentId, CourseId, Status, StartDate) VALUES (@UserId, @CourseId, @Status, GETDATE())";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@Status", status);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления прогресса: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Course>> GetAvailableCoursesAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT
                        c.CourseId,
                        ISNULL(c.CourseName, 'Без названия') as CourseName,
                        ISNULL(c.Description, 'Описание отсутствует') as Description,
                        ISNULL(pl.LanguageName, 'Не указан') as LanguageName,
                        ISNULL(cd.DifficultyName, 'Не указана') as DifficultyName,
                        c.IsPublished
                    FROM Courses c
                    LEFT JOIN ProgrammingLanguages pl ON c.LanguageId = pl.LanguageId
                    LEFT JOIN CourseDifficulties cd ON c.DifficultyId = cd.DifficultyId
                    WHERE c.IsPublished = 1
                    ORDER BY c.CourseName";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                var courses = new List<Course>();
                while (await reader.ReadAsync())
                {
                    courses.Add(new Course
                    {
                        CourseId = reader.GetInt32("CourseId"),
                        CourseName = reader.GetString("CourseName"),
                        Description = reader.GetString("Description"),
                        LanguageName = reader.GetString("LanguageName"),
                        DifficultyName = reader.GetString("DifficultyName"),
                        IsPublished = reader.GetBoolean("IsPublished")
                    });
                }
                return courses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения курсов: {ex.Message}");
                return new List<Course>();
            }
        }

        public async Task<bool> EnrollStudentInCourseAsync(int studentId, int courseId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Проверяем, не записан ли студент уже на курс
                var checkQuery = @"
                    SELECT COUNT(*) 
                    FROM StudentProgress 
                    WHERE StudentId = @StudentId AND CourseId = @CourseId";

                using var checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@StudentId", studentId);
                checkCommand.Parameters.AddWithValue("@CourseId", courseId);

                var existingCount = (int)await checkCommand.ExecuteScalarAsync();
                if (existingCount > 0)
                {
                    return false;
                }

                // Записываем студента на курс
                var enrollQuery = @"
                    INSERT INTO StudentProgress (StudentId, CourseId, Status, StartDate)
                    VALUES (@StudentId, @CourseId, 'not_started', GETDATE())";

                using var enrollCommand = new SqlCommand(enrollQuery, connection);
                enrollCommand.Parameters.AddWithValue("@StudentId", studentId);
                enrollCommand.Parameters.AddWithValue("@CourseId", courseId);

                var result = await enrollCommand.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи на курс: {ex.Message}");
                return false;
            }
        }

        // ДЛЯ УЧИТЕЛЯ
        public async Task<List<TeacherCourse>> GetTeacherCoursesAsync(int teacherId)
        {
            var courses = new List<TeacherCourse>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT c.CourseId, c.CourseName, c.Description, c.IsPublished,
                           l.LanguageName, d.DifficultyName,
                           COUNT(DISTINCT ge.StudentId) as StudentCount,
                           AVG(cr.Rating) as AverageRating
                    FROM Courses c
                    LEFT JOIN ProgrammingLanguages l ON c.LanguageId = l.LanguageId
                    LEFT JOIN CourseDifficulties d ON c.DifficultyId = d.DifficultyId
                    LEFT JOIN StudyGroups sg ON c.CourseId = sg.CourseId
                    LEFT JOIN GroupEnrollments ge ON sg.GroupId = ge.GroupId AND ge.Status = 'active'
                    LEFT JOIN CourseReviews cr ON c.CourseId = cr.CourseId AND cr.IsApproved = 1
                    WHERE c.CreatedByUserId = @TeacherId
                    GROUP BY c.CourseId, c.CourseName, c.Description, c.IsPublished, 
                             l.LanguageName, d.DifficultyName";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@TeacherId", teacherId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var course = new TeacherCourse
                    {
                        CourseId = reader.GetInt32("CourseId"),
                        CourseName = reader.GetString("CourseName"),
                        Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                        LanguageName = reader.GetString("LanguageName"),
                        DifficultyName = reader.GetString("DifficultyName"),
                        IsPublished = reader.GetBoolean("IsPublished"),
                        StudentCount = reader.IsDBNull("StudentCount") ? 0 : reader.GetInt32("StudentCount"),
                        AverageRating = reader.IsDBNull("AverageRating") ? 0 : Math.Round(reader.GetDouble("AverageRating"), 1)
                    };

                    course.Groups = await GetCourseGroupsAsync(course.CourseId);
                    courses.Add(course);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки курсов учителя: {ex.Message}");
            }
            return courses;
        }

        private async Task<List<StudyGroup>> GetCourseGroupsAsync(int courseId)
        {
            var groups = new List<StudyGroup>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT sg.GroupId, sg.GroupName, sg.StartDate, sg.EndDate, sg.IsActive,
                           COUNT(ge.StudentId) as StudentCount
                    FROM StudyGroups sg
                    LEFT JOIN GroupEnrollments ge ON sg.GroupId = ge.GroupId AND ge.Status = 'active'
                    WHERE sg.CourseId = @CourseId
                    GROUP BY sg.GroupId, sg.GroupName, sg.StartDate, sg.EndDate, sg.IsActive";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    groups.Add(new StudyGroup
                    {
                        GroupId = reader.GetInt32("GroupId"),
                        GroupName = reader.GetString("GroupName"),
                        StartDate = reader.GetDateTime("StartDate"),
                        EndDate = reader.GetDateTime("EndDate"),
                        IsActive = reader.GetBoolean("IsActive"),
                        StudentCount = reader.IsDBNull("StudentCount") ? 0 : reader.GetInt32("StudentCount")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки групп: {ex.Message}");
            }
            return groups;
        }

        public async Task<StudyGroup?> GetStudyGroupByIdAsync(int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT sg.GroupId, sg.GroupName, sg.StartDate, sg.EndDate, sg.IsActive, sg.CourseId,
                           COUNT(DISTINCT ge.StudentId) as StudentCount
                    FROM StudyGroups sg
                    LEFT JOIN GroupEnrollments ge ON sg.GroupId = ge.GroupId AND ge.Status = 'active'
                    WHERE sg.GroupId = @GroupId
                    GROUP BY sg.GroupId, sg.GroupName, sg.StartDate, sg.EndDate, sg.IsActive, sg.CourseId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new StudyGroup
                    {
                        GroupId = reader.GetInt32("GroupId"),
                        GroupName = reader.GetString("GroupName"),
                        StartDate = reader.GetDateTime("StartDate"),
                        EndDate = reader.GetDateTime("EndDate"),
                        IsActive = reader.GetBoolean("IsActive"),
                        StudentCount = reader.IsDBNull("StudentCount") ? 0 : reader.GetInt32("StudentCount")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения группы: {ex.Message}");
            }
            return null;
        }

        public async Task<List<StudyGroup>> GetTeacherStudyGroupsAsync(int teacherId)
        {
            var groups = new List<StudyGroup>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT sg.GroupId, sg.GroupName, sg.StartDate, sg.EndDate, sg.IsActive,
                   sg.CourseId, c.CourseName,
                   COUNT(ge.StudentId) as StudentCount
            FROM StudyGroups sg
            LEFT JOIN Courses c ON sg.CourseId = c.CourseId
            LEFT JOIN GroupEnrollments ge ON sg.GroupId = ge.GroupId AND ge.Status = 'active'
            WHERE sg.TeacherId = @TeacherId AND sg.IsActive = 1
            GROUP BY sg.GroupId, sg.GroupName, sg.StartDate, sg.EndDate, sg.IsActive, 
                     sg.CourseId, c.CourseName";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@TeacherId", teacherId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    groups.Add(new StudyGroup
                    {
                        GroupId = reader.GetInt32("GroupId"),
                        GroupName = reader.GetString("GroupName"),
                        CourseId = reader.GetInt32("CourseId"),
                        CourseName = reader.GetString("CourseName"), // Добавлено
                        StartDate = reader.GetDateTime("StartDate"),
                        EndDate = reader.GetDateTime("EndDate"),
                        IsActive = reader.GetBoolean("IsActive"),
                        StudentCount = reader.IsDBNull("StudentCount") ? 0 : reader.GetInt32("StudentCount")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки групп учителя: {ex.Message}");
            }
            return groups;
        }

        public async Task<List<StudentProgress>> GetStudentProgressAsync(int studentId)
        {
            var progress = new List<StudentProgress>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT c.CourseId, c.CourseName, sp.Status, sp.Score, sp.CompletionDate, sp.AttemptsCount
                    FROM StudentProgress sp
                    JOIN Courses c ON sp.CourseId = c.CourseId
                    WHERE sp.StudentId = @StudentId
                    ORDER BY sp.StartDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentId", studentId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    progress.Add(new StudentProgress
                    {
                        CourseId = reader.GetInt32("CourseId"),
                        CourseName = reader.GetString("CourseName"),
                        Status = reader.GetString("Status"),
                        Score = reader.IsDBNull("Score") ? 0 : reader.GetInt32("Score"),
                        CompletionDate = reader.IsDBNull("CompletionDate") ? null : reader.GetDateTime("CompletionDate"),
                        Attempts = reader.GetInt32("AttemptsCount")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки прогресса: {ex.Message}");
            }
            return progress;
        }

        // УРОКИ КУРСА
        public class LessonDto
        {
            public int LessonId { get; set; }
            public int ModuleId { get; set; }
            public string LessonType { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string? Content { get; set; }
            public int LessonOrder { get; set; }
        }


        public async Task<(int? FrameItemId, string? EmojiIcon, string? ThemeName)> GetEquippedItemsAsync(int userId)
        {
            int? frameId = null; string? emoji = null; string? theme = null;
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT si.ItemId, si.ItemType, si.Icon, si.Name
                    FROM UserInventory ui
                    JOIN ShopItems si ON si.ItemId = ui.ItemId
                    WHERE ui.UserId = @UserId AND ui.IsEquipped = 1";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var type = reader.GetString("ItemType").ToLower();
                    if (type == "avatar_frame") frameId = reader.GetInt32("ItemId");
                    else if (type == "emoji") emoji = reader.IsDBNull("Icon") ? null : reader.GetString("Icon");
                    else if (type == "theme") theme = reader.GetString("Name");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения экипированных предметов: {ex.Message}");
            }
            return (frameId, emoji, theme);
        }



        public async Task<bool> CreateCourseAsync(string courseName, string description, int languageId, int difficultyId, int createdByUserId, bool isGroupCourse)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    INSERT INTO Courses (
                        CourseName, Description, LanguageId, DifficultyId, 
                        CreatedByUserId, IsPublished, IsGroupCourse, CreatedDate
                    ) VALUES (
                        @CourseName, @Description, @LanguageId, @DifficultyId,
                        @CreatedByUserId, 0, @IsGroupCourse, GETDATE()
                    )";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseName", courseName ?? "");
                command.Parameters.AddWithValue("@Description", description ?? "");
                command.Parameters.AddWithValue("@LanguageId", languageId);
                command.Parameters.AddWithValue("@DifficultyId", difficultyId);
                command.Parameters.AddWithValue("@CreatedByUserId", createdByUserId);
                command.Parameters.AddWithValue("@IsGroupCourse", isGroupCourse);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания курса: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PublishCourseAsync(int courseId, int teacherId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    UPDATE Courses 
                    SET IsPublished = 1 
                    WHERE CourseId = @CourseId AND CreatedByUserId = @TeacherId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@TeacherId", teacherId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка публикации курса: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateStudyGroupAsync(string groupName, int courseId, DateTime startDate, DateTime endDate, int teacherId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO StudyGroups (GroupName, CourseId, TeacherId, StartDate, EndDate, IsActive)
            VALUES (@GroupName, @CourseId, @TeacherId, @StartDate, @EndDate, 1)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupName", groupName ?? "");
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@TeacherId", teacherId);
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания группы: {ex.Message}");
                Console.WriteLine($"Подробности: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<List<ProgrammingLanguage>> GetProgrammingLanguagesAsync()
        {
            var languages = new List<ProgrammingLanguage>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT LanguageId, LanguageName, IconUrl FROM ProgrammingLanguages WHERE IsActive = 1";
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    languages.Add(new ProgrammingLanguage
                    {
                        LanguageId = reader.GetInt32("LanguageId"),
                        LanguageName = reader.GetString("LanguageName"),
                        IconUrl = reader.IsDBNull("IconUrl") ? null : reader.GetString("IconUrl")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки языков: {ex.Message}");
            }
            return languages;
        }

        public async Task<List<CourseDifficulty>> GetCourseDifficultiesAsync()
        {
            var difficulties = new List<CourseDifficulty>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT DifficultyId, DifficultyName, Description, HasTheory, HasPractice FROM CourseDifficulties";
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    difficulties.Add(new CourseDifficulty
                    {
                        DifficultyId = reader.GetInt32("DifficultyId"),
                        DifficultyName = reader.GetString("DifficultyName"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        HasTheory = reader.GetBoolean("HasTheory"),
                        HasPractice = reader.GetBoolean("HasPractice")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки сложностей: {ex.Message}");
            }
            return difficulties;
        }

        public async Task<bool> CreateTestAsync(int courseId, string title, string description, int timeLimit, int passingScore)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var lessonQuery = @"
                    INSERT INTO Lessons (ModuleId, LessonType, Title, LessonOrder)
                    VALUES ((SELECT TOP 1 ModuleId FROM CourseModules WHERE CourseId = @CourseId), 'test', @Title, 1)";

                using var lessonCommand = new SqlCommand(lessonQuery, connection);
                lessonCommand.Parameters.AddWithValue("@CourseId", courseId);
                lessonCommand.Parameters.AddWithValue("@Title", title);
                await lessonCommand.ExecuteNonQueryAsync();

                var getLessonIdQuery = "SELECT SCOPE_IDENTITY()";
                using var idCommand = new SqlCommand(getLessonIdQuery, connection);
                var lessonId = Convert.ToInt32(await idCommand.ExecuteScalarAsync());

                var testQuery = @"
                    INSERT INTO Tests (LessonId, Title, Description, TimeLimitMinutes, PassingScore)
                    VALUES (@LessonId, @Title, @Description, @TimeLimit, @PassingScore)";

                using var testCommand = new SqlCommand(testQuery, connection);
                testCommand.Parameters.AddWithValue("@LessonId", lessonId);
                testCommand.Parameters.AddWithValue("@Title", title);
                testCommand.Parameters.AddWithValue("@Description", description ?? "");
                testCommand.Parameters.AddWithValue("@TimeLimit", timeLimit);
                testCommand.Parameters.AddWithValue("@PassingScore", passingScore);

                var result = await testCommand.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания теста: {ex.Message}");
                return false;
            }
        }

        // ВОССТАНОВЛЕНИЕ ПАРОЛЯ - ИСПРАВЛЕННЫЕ МЕТОДЫ
        // ДОБАВЬТЕ ЭТОТ МЕТОД ДЛЯ ХЭШИРОВАНИЯ ПАРОЛЕЙ
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // ВОССТАНОВЛЕНИЕ ПАРОЛЯ - ИСПРАВЛЕННЫЕ МЕТОДЫ
        // В DatabaseService добавьте эти методы:

        public async Task<bool> SavePasswordResetCodeAsync(int userId, string code)
        {
            try
            {
                Console.WriteLine($"🔧 Начинаем сохранение кода {code} для пользователя {userId}");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                Console.WriteLine("✅ Подключение к БД установлено");

                // Сначала проверим существующие коды
                var checkQuery = "SELECT COUNT(*) FROM PasswordResetCodes WHERE UserId = @UserId";
                using var checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@UserId", userId);
                var existingCount = (int)await checkCommand.ExecuteScalarAsync();
                Console.WriteLine($"📊 Найдено существующих кодов: {existingCount}");

                // Удаляем старые коды для этого пользователя
                var deleteQuery = "DELETE FROM PasswordResetCodes WHERE UserId = @UserId";
                using var deleteCommand = new SqlCommand(deleteQuery, connection);
                deleteCommand.Parameters.AddWithValue("@UserId", userId);
                var deletedRows = await deleteCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"🗑️ Удалено старых кодов: {deletedRows}");

                // Добавляем новый код на 60 минут
                var insertQuery = @"
            INSERT INTO PasswordResetCodes (UserId, Code, ExpiryDate, IsUsed, CreatedDate) 
            VALUES (@UserId, @Code, DATEADD(MINUTE, 60, GETUTCDATE()), 0, GETUTCDATE())";

                using var insertCommand = new SqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@UserId", userId);
                insertCommand.Parameters.AddWithValue("@Code", code);

                var result = await insertCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"📝 Результат вставки: {result} строк");

                if (result > 0)
                {
                    Console.WriteLine($"✅ Код {code} успешно сохранен для пользователя {userId}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось сохранить код {code}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка сохранения кода: {ex.Message}");
                Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<User?> GetUserByResetCodeAsync(string code)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT u.UserId, u.Username, u.Email, u.FirstName, u.LastName, u.RoleId
            FROM Users u
            INNER JOIN PasswordResetCodes prc ON u.UserId = prc.UserId
            WHERE prc.Code = @Code AND prc.IsUsed = 0 AND prc.ExpiryDate > GETUTCDATE()";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Code", code);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    Console.WriteLine($"✅ Найден пользователь по коду {code}");
                    return new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        Email = reader.GetString("Email"),
                        FirstName = reader.IsDBNull("FirstName") ? null : reader.GetString("FirstName"),
                        LastName = reader.IsDBNull("LastName") ? null : reader.GetString("LastName"),
                        RoleId = reader.GetInt32("RoleId")
                    };
                }

                Console.WriteLine($"❌ Код {code} не найден или устарел");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка проверки кода: {ex.Message}");
                return null;
            }
        }

        // Добавьте этот метод для диагностики
        private async Task CheckCodeStatus(string code)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT Code, ExpiryDate, IsUsed, UserId 
            FROM PasswordResetCodes 
            WHERE Code = @Code";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Code", code);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var expiryDate = reader.GetDateTime("ExpiryDate");
                    var isUsed = reader.GetBoolean("IsUsed");
                    var userId = reader.GetInt32("UserId");

                    Console.WriteLine($"🔍 Диагностика кода {code}:");
                    Console.WriteLine($"   UserId: {userId}");
                    Console.WriteLine($"   Expiry: {expiryDate}");
                    Console.WriteLine($"   IsUsed: {isUsed}");
                    Console.WriteLine($"   Current: {DateTime.UtcNow}");
                    Console.WriteLine($"   Valid: {expiryDate > DateTime.UtcNow}");
                }
                else
                {
                    Console.WriteLine($"🔍 Код {code} не найден в базе");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка диагностики: {ex.Message}");
            }
        }

        public async Task<bool> ChangePasswordWithResetAsync(int userId, string newPassword)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Начинаем транзакцию
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Хэшируем новый пароль
                    var hashedPassword = HashPassword(newPassword);

                    // 1. Обновляем пароль
                    var updateQuery = "UPDATE Users SET PasswordHash = @PasswordHash WHERE UserId = @UserId";
                    using var updateCommand = new SqlCommand(updateQuery, connection, transaction);
                    updateCommand.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    updateCommand.Parameters.AddWithValue("@UserId", userId);
                    await updateCommand.ExecuteNonQueryAsync();

                    // 2. Помечаем все коды восстановления как использованные
                    var deleteQuery = "UPDATE PasswordResetCodes SET IsUsed = 1 WHERE UserId = @UserId";
                    using var deleteCommand = new SqlCommand(deleteQuery, connection, transaction);
                    deleteCommand.Parameters.AddWithValue("@UserId", userId);
                    await deleteCommand.ExecuteNonQueryAsync();

                    // 3. Фиксируем транзакцию
                    transaction.Commit();

                    Console.WriteLine($"✅ Пароль успешно изменен для пользователя {userId}");
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка смены пароля: {ex.Message}");
                return false;
            }
        }


        public async Task<List<DisputedTestAttempt>> GetDisputedTestAttemptsAsync(int teacherId)
        {
            var disputedTests = new List<DisputedTestAttempt>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT ta.AttemptId, u.FirstName + ' ' + u.LastName as StudentName, 
                           c.CourseName, t.Title as TestTitle, ta.Score
                    FROM TestAttempts ta
                    JOIN Tests t ON ta.TestId = t.TestId
                    JOIN Lessons l ON t.LessonId = l.LessonId
                    JOIN CourseModules cm ON l.ModuleId = cm.ModuleId
                    JOIN Courses c ON cm.CourseId = c.CourseId
                    JOIN Users u ON ta.StudentId = u.UserId
                    WHERE c.CreatedByUserId = @TeacherId AND ta.IsDisputed = 1";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@TeacherId", teacherId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    disputedTests.Add(new DisputedTestAttempt
                    {
                        AttemptId = reader.GetInt32("AttemptId"),
                        StudentName = reader.GetString("StudentName"),
                        CourseName = reader.GetString("CourseName"),
                        TestTitle = reader.GetString("TestTitle"),
                        Score = reader.IsDBNull("Score") ? 0 : reader.GetInt32("Score")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки оспоренных тестов: {ex.Message}");
            }
            return disputedTests;
        }

        public async Task<bool> SaveUserSettingsAsync(int userId, string language, string theme)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE Users 
            SET LanguagePref = @Language, InterfaceStyle = @Theme
            WHERE UserId = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Language", language);
                command.Parameters.AddWithValue("@Theme", theme);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveUserThemeAsync(int userId, string theme)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE Users 
            SET InterfaceStyle = @Theme
            WHERE UserId = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Theme", theme);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения темы: {ex.Message}");
                return false;
            }
        }

        // СИСТЕМА ПОДДЕРЖКИ
        public async Task<bool> CreateSupportTicketAsync(int userId, string subject, string description, string ticketType = "question")
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO SupportTickets (UserId, Subject, Description, TicketType, Status, Priority, CreatedDate)
            VALUES (@UserId, @Subject, @Description, @TicketType, 'open', 3, GETDATE())";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Subject", subject);
                command.Parameters.AddWithValue("@Description", description);
                command.Parameters.AddWithValue("@TicketType", ticketType);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания обращения: {ex.Message}");
                return false;
            }
        }

        public async Task<List<SupportTicket>> GetUserSupportTicketsAsync(int userId)
        {
            var tickets = new List<SupportTicket>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT TicketId, Subject, Description, TicketType, Status, Priority, 
                   CreatedDate, ResolvedDate, AdminComment
            FROM SupportTickets 
            WHERE UserId = @UserId 
            ORDER BY CreatedDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tickets.Add(new SupportTicket
                    {
                        TicketId = reader.GetInt32("TicketId"),
                        UserId = userId,
                        Subject = reader.GetString("Subject"),
                        Description = reader.GetString("Description"),
                        TicketType = reader.GetString("TicketType"),
                        Status = reader.GetString("Status"),
                        Priority = reader.GetInt32("Priority"),
                        CreatedDate = reader.GetDateTime("CreatedDate"),
                        ResolvedDate = reader.IsDBNull("ResolvedDate") ? null : reader.GetDateTime("ResolvedDate"),
                        AdminComment = reader.IsDBNull("AdminComment") ? null : reader.GetString("AdminComment")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки обращений: {ex.Message}");
            }
            return tickets;
        }

        // СИСТЕМА НОВОСТЕЙ
        public async Task<List<News>> GetNewsAsync(string languageCode = "ru", bool forTeens = false)
        {
            var news = new List<News>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT n.NewsId, n.Title, n.Content, n.AuthorId, n.PublishedDate, n.IsActive,
                   u.FirstName + ' ' + u.LastName as AuthorName
            FROM News n
            LEFT JOIN Users u ON n.AuthorId = u.UserId
            WHERE n.IsActive = 1 AND n.LanguageCode = @LanguageCode AND n.ForTeens = @ForTeens
            ORDER BY n.PublishedDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LanguageCode", languageCode);
                command.Parameters.AddWithValue("@ForTeens", forTeens);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    news.Add(new News
                    {
                        NewsId = reader.GetInt32("NewsId"),
                        Title = reader.GetString("Title"),
                        Content = reader.GetString("Content"),
                        AuthorId = reader.GetInt32("AuthorId"),
                        PublishedDate = reader.GetDateTime("PublishedDate"),
                        IsActive = reader.GetBoolean("IsActive"),
                        LanguageCode = languageCode,
                        ForTeens = forTeens
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки новостей: {ex.Message}");
            }
            return news;
        }

        // СИСТЕМА ОТЗЫВОВ
        public async Task<bool> CreateCourseReviewAsync(int courseId, int studentId, int rating, string comment)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO CourseReviews (CourseId, StudentId, Rating, Comment, ReviewDate, IsApproved)
            VALUES (@CourseId, @StudentId, @Rating, @Comment, GETDATE(), 1)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@StudentId", studentId);
                command.Parameters.AddWithValue("@Rating", rating);
                command.Parameters.AddWithValue("@Comment", comment ?? "");

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания отзыва: {ex.Message}");
                return false;
            }
        }

        public async Task<List<CourseReview>> GetCourseReviewsAsync(int courseId)
        {
            var reviews = new List<CourseReview>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT cr.ReviewId, cr.CourseId, cr.StudentId, cr.Rating, cr.Comment, cr.ReviewDate,
                   u.FirstName + ' ' + u.LastName as StudentName
            FROM CourseReviews cr
            LEFT JOIN Users u ON cr.StudentId = u.UserId
            WHERE cr.CourseId = @CourseId AND cr.IsApproved = 1
            ORDER BY cr.ReviewDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    reviews.Add(new CourseReview
                    {
                        ReviewId = reader.GetInt32("ReviewId"),
                        CourseId = courseId,
                        StudentId = reader.GetInt32("StudentId"),
                        Rating = reader.GetInt32("Rating"),
                        Comment = reader.IsDBNull("Comment") ? null : reader.GetString("Comment"),
                        ReviewDate = reader.GetDateTime("ReviewDate"),
                        IsApproved = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки отзывов: {ex.Message}");
            }
            return reviews;
        }

        // КОНКУРСЫ
        public async Task<List<Contest>> GetActiveContestsAsync()
        {
            var contests = new List<Contest>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT c.ContestId, c.ContestName, c.Description, c.StartDate, c.EndDate, 
                   c.PrizeCurrency, pl.LanguageName
            FROM Contests c
            LEFT JOIN ProgrammingLanguages pl ON c.LanguageId = pl.LanguageId
            WHERE c.IsActive = 1 AND c.EndDate >= GETDATE()
            ORDER BY c.StartDate DESC";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    contests.Add(new Contest
                    {
                        ContestId = reader.GetInt32("ContestId"),
                        ContestName = reader.GetString("ContestName"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        StartDate = reader.GetDateTime("StartDate"),
                        EndDate = reader.GetDateTime("EndDate"),
                        PrizeCurrency = reader.GetInt32("PrizeCurrency"),
                        Language = new ProgrammingLanguage
                        {
                            LanguageName = reader.IsDBNull("LanguageName") ? "Не указан" : reader.GetString("LanguageName")
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки активных конкурсов: {ex.Message}");
            }
            return contests;
        }

        public async Task<List<Contest>> GetCompletedContestsAsync()
        {
            var contests = new List<Contest>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT c.ContestId, c.ContestName, c.Description, c.StartDate, c.EndDate, 
                   c.PrizeCurrency, pl.LanguageName
            FROM Contests c
            LEFT JOIN ProgrammingLanguages pl ON c.LanguageId = pl.LanguageId
            WHERE c.IsActive = 1 AND c.EndDate < GETDATE()
            ORDER BY c.EndDate DESC";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    contests.Add(new Contest
                    {
                        ContestId = reader.GetInt32("ContestId"),
                        ContestName = reader.GetString("ContestName"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        StartDate = reader.GetDateTime("StartDate"),
                        EndDate = reader.GetDateTime("EndDate"),
                        PrizeCurrency = reader.GetInt32("PrizeCurrency"),
                        Language = new ProgrammingLanguage
                        {
                            LanguageName = reader.IsDBNull("LanguageName") ? "Не указан" : reader.GetString("LanguageName")
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки завершенных конкурсов: {ex.Message}");
            }
            return contests;
        }

        public async Task<List<ContestSubmission>> GetUserContestSubmissionsAsync(int userId)
        {
            var submissions = new List<ContestSubmission>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT cs.SubmissionId, cs.ContestId, cs.ProjectName, cs.Description, 
                   cs.SubmissionDate, cs.TeacherScore, cs.TeacherComment,
                   c.ContestName
            FROM ContestSubmissions cs
            JOIN Contests c ON cs.ContestId = c.ContestId
            WHERE cs.StudentId = @UserId
            ORDER BY cs.SubmissionDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    submissions.Add(new ContestSubmission
                    {
                        SubmissionId = reader.GetInt32("SubmissionId"),
                        ContestId = reader.GetInt32("ContestId"),
                        ProjectName = reader.GetString("ProjectName"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        SubmissionDate = reader.GetDateTime("SubmissionDate"),
                        TeacherScore = reader.IsDBNull("TeacherScore") ? null : reader.GetInt32("TeacherScore"),
                        TeacherComment = reader.IsDBNull("TeacherComment") ? null : reader.GetString("TeacherComment"),
                        Contest = new Contest { ContestName = reader.GetString("ContestName") }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки заявок: {ex.Message}");
            }
            return submissions;
        }

        public async Task<bool> CreateContestSubmissionAsync(int contestId, int studentId, string projectName, string projectFileUrl, string? description)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO ContestSubmissions (ContestId, StudentId, ProjectName, ProjectFileUrl, Description, SubmissionDate)
            VALUES (@ContestId, @StudentId, @ProjectName, @ProjectFileUrl, @Description, GETDATE())";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ContestId", contestId);
                command.Parameters.AddWithValue("@StudentId", studentId);
                command.Parameters.AddWithValue("@ProjectName", projectName ?? "");
                command.Parameters.AddWithValue("@ProjectFileUrl", projectFileUrl ?? "");
                command.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания заявки на конкурс: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ContestSubmission>> GetContestSubmissionsForContestAsync(int contestId)
        {
            var submissions = new List<ContestSubmission>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT cs.SubmissionId, cs.StudentId, cs.ProjectName, cs.ProjectFileUrl, cs.Description, cs.SubmissionDate,
                   cs.TeacherScore, cs.TeacherComment,
                   u.FirstName + ' ' + u.LastName as StudentName
            FROM ContestSubmissions cs
            JOIN Users u ON cs.StudentId = u.UserId
            WHERE cs.ContestId = @ContestId
            ORDER BY cs.SubmissionDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ContestId", contestId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    submissions.Add(new ContestSubmission
                    {
                        SubmissionId = reader.GetInt32("SubmissionId"),
                        ContestId = contestId,
                        StudentId = reader.GetInt32("StudentId"),
                        ProjectName = reader.GetString("ProjectName"),
                        ProjectFileUrl = reader.GetString("ProjectFileUrl"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        SubmissionDate = reader.GetDateTime("SubmissionDate"),
                        TeacherScore = reader.IsDBNull("TeacherScore") ? null : reader.GetInt32("TeacherScore"),
                        TeacherComment = reader.IsDBNull("TeacherComment") ? null : reader.GetString("TeacherComment"),
                        Student = new User 
                        { 
                            FirstName = reader.GetString("StudentName").Split(' ').FirstOrDefault() ?? "",
                            LastName = reader.GetString("StudentName").Split(' ').Skip(1).FirstOrDefault() ?? ""
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки заявок конкурса: {ex.Message}");
            }
            return submissions;
        }

        public async Task<bool> GradeContestSubmissionAsync(int submissionId, int score, string? comment)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"UPDATE ContestSubmissions SET TeacherScore = @Score, TeacherComment = @Comment WHERE SubmissionId = @SubmissionId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SubmissionId", submissionId);
                command.Parameters.AddWithValue("@Score", score);
                command.Parameters.AddWithValue("@Comment", (object?)comment ?? DBNull.Value);
                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка оценки работы: {ex.Message}");
                return false;
            }
        }

        // ГРУППЫ: студенты в группе и операции
        public async Task<List<User>> GetGroupStudentsAsync(int groupId)
        {
            var students = new List<User>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT u.UserId, u.Username, u.FirstName, u.LastName, u.Email
            FROM GroupEnrollments ge
            JOIN Users u ON ge.StudentId = u.UserId
            WHERE ge.GroupId = @GroupId AND ge.Status = 'active'";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    students.Add(new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        FirstName = reader.IsDBNull("FirstName") ? null : reader.GetString("FirstName"),
                        LastName = reader.IsDBNull("LastName") ? null : reader.GetString("LastName"),
                        Email = reader.GetString("Email")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки студентов группы: {ex.Message}");
            }
            return students;
        }

        public async Task<bool> DeactivateGroupAsync(int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"UPDATE StudyGroups SET IsActive=0 WHERE GroupId=@GroupId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка деактивации группы: {ex.Message}");
                return false;
            }
        }

        // ПОЛЬЗОВАТЕЛИ
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"SELECT TOP 1 UserId, Username, FirstName, LastName, Email, RoleId FROM Users WHERE Username=@Username";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username ?? string.Empty);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        FirstName = reader.IsDBNull("FirstName") ? null : reader.GetString("FirstName"),
                        LastName = reader.IsDBNull("LastName") ? null : reader.GetString("LastName"),
                        Email = reader.GetString("Email"),
                        RoleId = reader.GetInt32("RoleId")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения пользователя: {ex.Message}");
            }
            return null;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"SELECT TOP 1 UserId, Username, FirstName, LastName, Email, RoleId FROM Users WHERE Email=@Email";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Email", email ?? string.Empty);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        FirstName = reader.IsDBNull("FirstName") ? null : reader.GetString("FirstName"),
                        LastName = reader.IsDBNull("LastName") ? null : reader.GetString("LastName"),
                        Email = reader.GetString("Email"),
                        RoleId = reader.GetInt32("RoleId")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения пользователя по email: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"UPDATE Users SET PasswordHash=@Hash WHERE UserId=@UserId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Hash", HashPassword(newPassword ?? ""));
                var res = await cmd.ExecuteNonQueryAsync();
                return res > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка смены пароля: {ex.Message}");
                return false;
            }
        }


        public async Task<bool> UpdateProgressWithScoreAsync(int userId, int courseId, int lessonId, string status, int score)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"
            IF EXISTS (SELECT 1 FROM StudentProgress WHERE StudentId=@UserId AND CourseId=@CourseId AND LessonId=@LessonId)
                UPDATE StudentProgress SET Status=@Status, Score=@Score, CompletionDate=CASE WHEN @Status='completed' THEN GETDATE() ELSE CompletionDate END
                WHERE StudentId=@UserId AND CourseId=@CourseId AND LessonId=@LessonId
            ELSE
                INSERT INTO StudentProgress (StudentId, CourseId, LessonId, Status, StartDate, CompletionDate, Score)
                VALUES (@UserId, @CourseId, @LessonId, @Status, GETDATE(), CASE WHEN @Status='completed' THEN GETDATE() ELSE NULL END, @Score);

            -- Бонус за завершение курса (один раз на курс)
            IF @Status='completed'
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM CurrencyTransactions 
                    WHERE UserId=@UserId AND TransactionType='income' AND Reason='course_completion_bonus' AND ReferenceId=@CourseId)
                BEGIN
                    UPDATE Users SET GameCurrency = ISNULL(GameCurrency,0) + 500 WHERE UserId=@UserId;
                    INSERT INTO CurrencyTransactions (UserId, Amount, TransactionType, Reason, ReferenceId, TransactionDate)
                    VALUES (@UserId, 500, 'income', 'course_completion_bonus', @CourseId, GETDATE());
                END
            END";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@LessonId", lessonId);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Score", score);
                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления прогресса со счетом: {ex.Message}");
                return false;
            }
        }

        public async Task<int?> StartTestAttemptAsync(int testId, int studentId, int? groupId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"INSERT INTO TestAttempts (TestId, StudentId, GroupId, StartTime, Status)
                      VALUES (@TestId, @StudentId, @GroupId, GETDATE(), 'in_progress'); SELECT SCOPE_IDENTITY();";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@TestId", testId);
                cmd.Parameters.AddWithValue("@StudentId", studentId);
                cmd.Parameters.AddWithValue("@GroupId", (object?)groupId ?? DBNull.Value);
                var id = await cmd.ExecuteScalarAsync();
                return id == null ? null : Convert.ToInt32(id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка старта попытки: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CompleteTestAttemptAsync(int attemptId, int autoScore)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"UPDATE TestAttempts SET AutoScore=@Score, EndTime=GETDATE(), Status='completed' WHERE AttemptId=@AttemptId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@AttemptId", attemptId);
                cmd.Parameters.AddWithValue("@Score", autoScore);
                var res = await cmd.ExecuteNonQueryAsync();
                return res > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка завершения попытки: {ex.Message}");
                return false;
            }
        }

        // ЧАТЫ: получение групп для студентов и учителей
        public async Task<List<StudentGroupChat>> GetStudentGroupChatsAsync(int studentId)
        {
            var groups = new List<StudentGroupChat>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT 
                sg.GroupId, 
                sg.GroupName, 
                c.CourseName,
                COUNT(DISTINCT ge.StudentId) as StudentCount,
                u.FirstName + ' ' + u.LastName as TeacherName
            FROM StudyGroups sg
            INNER JOIN GroupEnrollments ge ON sg.GroupId = ge.GroupId
            INNER JOIN Courses c ON sg.CourseId = c.CourseId
            INNER JOIN Users u ON sg.TeacherId = u.UserId
            WHERE ge.StudentId = @StudentId 
                AND ge.Status = 'active'
                AND sg.IsActive = 1
            GROUP BY sg.GroupId, sg.GroupName, c.CourseName, u.FirstName, u.LastName
            ORDER BY sg.GroupName";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentId", studentId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    groups.Add(new StudentGroupChat
                    {
                        GroupId = reader.GetInt32("GroupId"),
                        GroupName = reader.GetString("GroupName"),
                        CourseName = reader.GetString("CourseName"),
                        StudentCount = reader.GetInt32("StudentCount"),
                        TeacherName = reader.GetString("TeacherName")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения групп студента: {ex.Message}");
            }
            return groups;
        }


        // СТУДЕНТЫ КУРСА
        public async Task<List<CourseStudent>> GetCourseStudentsAsync(int courseId)
        {
            var students = new List<CourseStudent>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"SELECT DISTINCT u.UserId, u.FirstName + ' ' + u.LastName as StudentName, u.Username,
                      ISNULL(AVG(sp.ProgressPercentage), 0) as Progress,
                      ISNULL(MAX(sp.LastUpdated), GETDATE()) as LastActivity
                      FROM Users u
                      INNER JOIN StudentGroupMemberships sgm ON u.UserId = sgm.StudentId
                      INNER JOIN StudyGroups sg ON sgm.GroupId = sg.GroupId
                      LEFT JOIN StudentProgress sp ON u.UserId = sp.StudentId AND sp.CourseId = @CourseId
                      WHERE sg.CourseId = @CourseId AND u.RoleId = 1
                      GROUP BY u.UserId, u.FirstName, u.LastName, u.Username";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@CourseId", courseId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    students.Add(new CourseStudent
                    {
                        StudentId = r.GetInt32("UserId"),
                        StudentName = r.GetString("StudentName"),
                        Username = r.GetString("Username"),
                        Progress = Convert.ToInt32(r.GetDecimal("Progress")),
                        LastActivity = r.GetDateTime("LastActivity")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения студентов курса: {ex.Message}");
            }
            return students;
        }

        public class CourseStudent
        {
            public int StudentId { get; set; }
            public string StudentName { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public int Progress { get; set; }
            public DateTime LastActivity { get; set; }
        }

        // ОЦЕНИВАНИЕ КОНКУРСОВ
        public async Task<Contest?> GetContestByIdAsync(int contestId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT c.ContestId, c.ContestName, c.Description, c.StartDate, c.EndDate, 
                   c.PrizeCurrency, c.IsActive, pl.LanguageName
            FROM Contests c
            LEFT JOIN ProgrammingLanguages pl ON c.LanguageId = pl.LanguageId
            WHERE c.ContestId = @ContestId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ContestId", contestId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Contest
                    {
                        ContestId = reader.GetInt32("ContestId"),
                        ContestName = reader.GetString("ContestName"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        StartDate = reader.GetDateTime("StartDate"),
                        EndDate = reader.GetDateTime("EndDate"),
                        PrizeCurrency = reader.GetInt32("PrizeCurrency"),
                        IsActive = reader.GetBoolean("IsActive"),
                        Language = new ProgrammingLanguage
                        {
                            LanguageName = reader.IsDBNull("LanguageName") ? "Не указан" : reader.GetString("LanguageName")
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения конкурса: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> GradeContestSubmissionAsync(int submissionId, int teacherId, int score, string feedback)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // Сначала получаем информацию о конкурсе через submission
                var contestQuery = @"
                    SELECT c.ContestId, c.StartDate, c.EndDate
                    FROM ContestSubmissions cs
                    INNER JOIN Contests c ON cs.ContestId = c.ContestId
                    WHERE cs.SubmissionId = @SubmissionId";
                
                Contest? contest = null;
                using (var contestCmd = new SqlCommand(contestQuery, connection))
                {
                    contestCmd.Parameters.AddWithValue("@SubmissionId", submissionId);
                    using var reader = await contestCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        contest = new Contest
                        {
                            ContestId = reader.GetInt32("ContestId"),
                            StartDate = reader.GetDateTime("StartDate"),
                            EndDate = reader.GetDateTime("EndDate")
                        };
                    }
                }

                // Проверяем период конкурса - оценка разрешена только после окончания конкурса
                if (contest != null)
                {
                    var now = DateTime.Now;
                    if (now < contest.EndDate)
                    {
                        Console.WriteLine($"Конкурс еще не завершен. Окончание: {contest.EndDate}, Текущее время: {now}");
                        return false; // Конкурс еще не завершен, оценка не разрешена
                    }
                }

                var query = @"UPDATE ContestSubmissions 
                      SET TeacherScore = @Score, TeacherComment = @Comment, 
                          GradedBy = @TeacherId, GradedAt = GETDATE(), Status = 'graded'
                      WHERE SubmissionId = @SubmissionId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@SubmissionId", submissionId);
                cmd.Parameters.AddWithValue("@Score", score);
                cmd.Parameters.AddWithValue("@Comment", (object?)feedback ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TeacherId", teacherId);
                var res = await cmd.ExecuteNonQueryAsync();
                return res > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка оценивания: {ex.Message}");
                return false;
            }
        }

        public async Task<List<StudyGroup>> GetUserStudyGroupsAsync(int userId)
        {
            var groups = new List<StudyGroup>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT 
                sg.GroupId, 
                sg.GroupName, 
                sg.StartDate, 
                sg.EndDate, 
                sg.IsActive,
                c.CourseName,
                COUNT(DISTINCT ge.StudentId) as StudentCount
            FROM StudyGroups sg
            INNER JOIN GroupEnrollments ge ON sg.GroupId = ge.GroupId
            INNER JOIN Courses c ON sg.CourseId = c.CourseId
            WHERE ge.StudentId = @UserId 
                AND ge.Status = 'active'
                AND sg.IsActive = 1
            GROUP BY sg.GroupId, sg.GroupName, sg.StartDate, sg.EndDate, sg.IsActive, c.CourseName
            ORDER BY sg.StartDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    groups.Add(new StudyGroup
                    {
                        GroupId = reader.GetInt32("GroupId"),
                        GroupName = reader.GetString("GroupName"),
                        StartDate = reader.GetDateTime("StartDate"),
                        EndDate = reader.GetDateTime("EndDate"),
                        IsActive = reader.GetBoolean("IsActive"),
                        CourseName = reader.GetString("CourseName"),
                        StudentCount = reader.GetInt32("StudentCount")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки групп пользователя: {ex.Message}");
            }
            return groups;
        }

        public async Task<List<GroupChatMessage>> GetGroupChatMessagesAsync(int groupId, int count = 200)
        {
            var messages = new List<GroupChatMessage>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            WITH EquippedEmoji AS (
                SELECT ui.UserId, MAX(si.Icon) AS EmojiIcon
                FROM UserInventory ui
                JOIN ShopItems si ON si.ItemId = ui.ItemId AND si.ItemType = 'emoji'
                WHERE ui.IsEquipped = 1
                GROUP BY ui.UserId
            )
            SELECT TOP (@Count) 
                m.MessageId, m.GroupId, m.SenderId, m.MessageText, m.SentAt, m.IsRead,
                u.FirstName + ' ' + u.LastName as SenderName,
                ISNULL(u.AvatarUrl, 'default_avatar.png') as SenderAvatar,
                ee.EmojiIcon
            FROM GroupChatMessages m
            JOIN Users u ON m.SenderId = u.UserId
            LEFT JOIN EquippedEmoji ee ON ee.UserId = m.SenderId
            WHERE m.GroupId = @GroupId
            ORDER BY m.SentAt ASC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@Count", count);

                using var reader = await command.ExecuteReaderAsync();
                // Получаем московский часовой пояс
                var moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                if (moscowTimeZone == null)
                {
                    // Fallback для Linux/Mac
                    try
                    {
                        moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                    }
                    catch
                    {
                        moscowTimeZone = TimeZoneInfo.Utc;
                    }
                }

                while (await reader.ReadAsync())
                {
                    var sentAt = reader.GetDateTime("SentAt");
                    // Конвертируем в московское время
                    if (sentAt.Kind == DateTimeKind.Unspecified)
                    {
                        // Предполагаем, что время из БД в UTC
                        sentAt = DateTime.SpecifyKind(sentAt, DateTimeKind.Utc);
                    }
                    
                    if (sentAt.Kind == DateTimeKind.Utc)
                    {
                        sentAt = TimeZoneInfo.ConvertTimeFromUtc(sentAt, moscowTimeZone);
                    }

                    messages.Add(new GroupChatMessage
                    {
                        MessageId = reader.GetInt32("MessageId"),
                        GroupId = reader.GetInt32("GroupId"),
                        SenderId = reader.GetInt32("SenderId"),
                        MessageText = reader.GetString("MessageText"),
                        SentAt = sentAt,
                        IsRead = reader.GetBoolean("IsRead"),
                        SenderName = reader.GetString("SenderName"),
                        SenderAvatar = reader.GetString("SenderAvatar"),
                        UserEmoji = reader.IsDBNull("EmojiIcon") ? null : reader.GetString("EmojiIcon")
                    });
                }

                return messages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки сообщений чата: {ex.Message}");
                return messages;
            }
        }

        public async Task<bool> CreateSimpleTestAsync(int courseId, string title, string description, int timeLimit, int passingScore)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Создаем модуль для курса если его нет
                var moduleQuery = @"
            IF NOT EXISTS (SELECT 1 FROM CourseModules WHERE CourseId = @CourseId)
                INSERT INTO CourseModules (CourseId, ModuleName, ModuleOrder) 
                VALUES (@CourseId, 'Основной модуль', 1);
            SELECT TOP 1 ModuleId FROM CourseModules WHERE CourseId = @CourseId";

                using var moduleCommand = new SqlCommand(moduleQuery, connection);
                moduleCommand.Parameters.AddWithValue("@CourseId", courseId);
                var moduleId = await moduleCommand.ExecuteScalarAsync();

                if (moduleId == null) return false;

                // Создаем урок типа "test"
                var lessonQuery = @"
            INSERT INTO Lessons (ModuleId, LessonType, Title, LessonOrder, IsActive)
            VALUES (@ModuleId, 'test', @Title, 1, 1);
            SELECT SCOPE_IDENTITY();";

                using var lessonCommand = new SqlCommand(lessonQuery, connection);
                lessonCommand.Parameters.AddWithValue("@ModuleId", moduleId);
                lessonCommand.Parameters.AddWithValue("@Title", title);
                var lessonId = await lessonCommand.ExecuteScalarAsync();

                if (lessonId == null) return false;

                // Создаем тест
                var testQuery = @"
            INSERT INTO Tests (LessonId, Title, Description, TimeLimitMinutes, PassingScore)
            VALUES (@LessonId, @Title, @Description, @TimeLimit, @PassingScore)";

                using var testCommand = new SqlCommand(testQuery, connection);
                testCommand.Parameters.AddWithValue("@LessonId", lessonId);
                testCommand.Parameters.AddWithValue("@Title", title);
                testCommand.Parameters.AddWithValue("@Description", description ?? "");
                testCommand.Parameters.AddWithValue("@TimeLimit", timeLimit);
                testCommand.Parameters.AddWithValue("@PassingScore", passingScore);

                var result = await testCommand.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания теста: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendGroupChatMessageAsync(int groupId, int senderId, string message, bool isSystemMessage = false)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO GroupChatMessages (GroupId, SenderId, MessageText, SentAt, IsRead)
            VALUES (@GroupId, @SenderId, @MessageText, GETDATE(), 0)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@SenderId", senderId);
                command.Parameters.AddWithValue("@MessageText", message);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
                return false;
            }
        }


        public async Task<bool> AddSystemMessageToGroupAsync(int groupId, string message)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Используем правильное имя таблицы - GroupChatMessages
                var query = @"
            INSERT INTO GroupChatMessages (GroupId, SenderId, MessageText, SentAt, IsRead, IsSystemMessage)
            VALUES (@GroupId, 0, @MessageText, GETDATE(), 0, 1)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@MessageText", message);

                var result = await command.ExecuteNonQueryAsync();
                Console.WriteLine($"✅ Системное сообщение добавлено в группу {groupId}: {message}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки системного сообщения: {ex.Message}");
                Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<int> GetLastCreatedGroupId() 
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var query = "SELECT TOP 1 GroupId FROM StudyGroups ORDER BY GroupId DESC";
                using var command = new SqlCommand(query, connection);

                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
        }


        public Task<List<GroupChatMessage>> GetGroupChatMessagesAsync(int groupId)
            => GetGroupChatMessagesAsync(groupId, 200);


        public async Task<int?> AddTheoryLessonAsync(int courseId, string title, string? htmlContent, int order = 1)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Сначала получаем первый модуль курса
                var moduleQuery = @"
            SELECT TOP 1 ModuleId FROM CourseModules 
            WHERE CourseId = @CourseId 
            ORDER BY ModuleOrder";

                using var moduleCommand = new SqlCommand(moduleQuery, connection);
                moduleCommand.Parameters.AddWithValue("@CourseId", courseId);
                var moduleId = await moduleCommand.ExecuteScalarAsync();

                if (moduleId == null)
                {
                    // Создаем модуль, если его нет
                    var createModuleQuery = @"
                INSERT INTO CourseModules (CourseId, ModuleName, ModuleOrder)
                VALUES (@CourseId, 'Основной модуль', 1);
                SELECT SCOPE_IDENTITY();";

                    using var createModuleCommand = new SqlCommand(createModuleQuery, connection);
                    createModuleCommand.Parameters.AddWithValue("@CourseId", courseId);
                    moduleId = await createModuleCommand.ExecuteScalarAsync();
                }

                var query = @"
            INSERT INTO Lessons (ModuleId, LessonType, Title, Content, LessonOrder, IsActive)
            VALUES (@ModuleId, 'theory', @Title, @Content, @Order, 1);
            SELECT SCOPE_IDENTITY();";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ModuleId", Convert.ToInt32(moduleId));
                command.Parameters.AddWithValue("@Title", title ?? "");
                command.Parameters.AddWithValue("@Content", (object?)htmlContent ?? DBNull.Value);
                command.Parameters.AddWithValue("@Order", order);

                var result = await command.ExecuteScalarAsync();
                return result == null ? null : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления теории: {ex.Message}");
                return null;
            }
        }

        public async Task<int?> AddPracticeLessonAsync(int courseId, string title, string? starterCode, string? expectedOutput, string? testCases, string? hint, int order = 1)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Сначала получаем первый модуль курса
                var moduleQuery = @"
            SELECT TOP 1 ModuleId FROM CourseModules 
            WHERE CourseId = @CourseId 
            ORDER BY ModuleOrder";

                using var moduleCommand = new SqlCommand(moduleQuery, connection);
                moduleCommand.Parameters.AddWithValue("@CourseId", courseId);
                var moduleId = await moduleCommand.ExecuteScalarAsync();

                if (moduleId == null)
                {
                    // Создаем модуль, если его нет
                    var createModuleQuery = @"
                INSERT INTO CourseModules (CourseId, ModuleName, ModuleOrder)
                VALUES (@CourseId, 'Основной модуль', 1);
                SELECT SCOPE_IDENTITY();";

                    using var createModuleCommand = new SqlCommand(createModuleQuery, connection);
                    createModuleCommand.Parameters.AddWithValue("@CourseId", courseId);
                    moduleId = await createModuleCommand.ExecuteScalarAsync();
                }

                // Создаем урок
                var lessonQuery = @"
            INSERT INTO Lessons (ModuleId, LessonType, Title, LessonOrder, IsActive)
            VALUES (@ModuleId, 'practice', @Title, @Order, 1);
            SELECT SCOPE_IDENTITY();";

                using var lessonCommand = new SqlCommand(lessonQuery, connection);
                lessonCommand.Parameters.AddWithValue("@ModuleId", Convert.ToInt32(moduleId));
                lessonCommand.Parameters.AddWithValue("@Title", title ?? "");
                lessonCommand.Parameters.AddWithValue("@Order", order);

                var lessonId = await lessonCommand.ExecuteScalarAsync();
                if (lessonId == null) return null;

                // Создаем практическое задание
                var practiceQuery = @"
            INSERT INTO PracticeExercises (LessonId, StarterCode, ExpectedOutput, TestCases, Hint)
            VALUES (@LessonId, @StarterCode, @ExpectedOutput, @TestCases, @Hint)";

                using var practiceCommand = new SqlCommand(practiceQuery, connection);
                practiceCommand.Parameters.AddWithValue("@LessonId", Convert.ToInt32(lessonId));
                practiceCommand.Parameters.AddWithValue("@StarterCode", (object?)starterCode ?? DBNull.Value);
                practiceCommand.Parameters.AddWithValue("@ExpectedOutput", (object?)expectedOutput ?? DBNull.Value);
                practiceCommand.Parameters.AddWithValue("@TestCases", (object?)testCases ?? DBNull.Value);
                practiceCommand.Parameters.AddWithValue("@Hint", (object?)hint ?? DBNull.Value);

                await practiceCommand.ExecuteNonQueryAsync();
                return Convert.ToInt32(lessonId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления практики: {ex.Message}");
                return null;
            }
        }

        // Для индивидуальных чатов с учителями
        public async Task<List<ChatMessage>> GetPrivateChatMessagesAsync(int studentId, int teacherId)
        {
            var messages = new List<ChatMessage>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            WITH EquippedEmoji AS (
                SELECT ui.UserId, MAX(si.Icon) AS EmojiIcon
                FROM UserInventory ui
                JOIN ShopItems si ON si.ItemId = ui.ItemId AND si.ItemType = 'emoji'
                WHERE ui.IsEquipped = 1
                GROUP BY ui.UserId
            )
            SELECT MessageId, SenderId, ReceiverId, MessageText, SentAt, IsRead,
                   u.FirstName + ' ' + u.LastName as SenderName,
                   ISNULL(u.AvatarUrl, 'default_avatar.png') as SenderAvatar,
                   ee.EmojiIcon
            FROM PrivateChats pc
            JOIN Users u ON pc.SenderId = u.UserId
            LEFT JOIN EquippedEmoji ee ON ee.UserId = pc.SenderId
            WHERE (SenderId = @StudentId AND ReceiverId = @TeacherId)
               OR (SenderId = @TeacherId AND ReceiverId = @StudentId)
            ORDER BY SentAt ASC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentId", studentId);
                command.Parameters.AddWithValue("@TeacherId", teacherId);

                using var reader = await command.ExecuteReaderAsync();
                // Получаем московский часовой пояс
                var moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                if (moscowTimeZone == null)
                {
                    try
                    {
                        moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                    }
                    catch
                    {
                        moscowTimeZone = TimeZoneInfo.Utc;
                    }
                }

                while (await reader.ReadAsync())
                {
                    var sentAt = reader.GetDateTime("SentAt");
                    // Конвертируем в московское время
                    if (sentAt.Kind == DateTimeKind.Unspecified)
                    {
                        sentAt = DateTime.SpecifyKind(sentAt, DateTimeKind.Utc);
                    }
                    
                    if (sentAt.Kind == DateTimeKind.Utc)
                    {
                        sentAt = TimeZoneInfo.ConvertTimeFromUtc(sentAt, moscowTimeZone);
                    }

                    messages.Add(new ChatMessage
                    {
                        MessageId = reader.GetInt32("MessageId"),
                        SenderId = reader.GetInt32("SenderId"),
                        MessageText = reader.GetString("MessageText"),
                        SentAt = sentAt,
                        IsRead = reader.GetBoolean("IsRead"),
                        SenderName = reader.GetString("SenderName"),
                        SenderAvatar = reader.GetString("SenderAvatar"),
                        UserEmoji = reader.IsDBNull("EmojiIcon") ? null : reader.GetString("EmojiIcon")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки приватных сообщений: {ex.Message}");
            }
            return messages;
        }

        public async Task<bool> SendPrivateMessageAsync(int senderId, int receiverId, string message)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO PrivateChats (SenderId, ReceiverId, MessageText, SentAt, IsRead)
            VALUES (@SenderId, @ReceiverId, @MessageText, GETDATE(), 0)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SenderId", senderId);
                command.Parameters.AddWithValue("@ReceiverId", receiverId);
                command.Parameters.AddWithValue("@MessageText", message);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки приватного сообщения: {ex.Message}");
                return false;
            }
        }

        // ТЕОРИЯ - простой текст вместо HTML
        public async Task<int?> AddSimpleTheoryAsync(int courseId, string title, string content, int order = 1)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var moduleId = await GetOrCreateModuleAsync(connection, courseId);

                var query = @"
            INSERT INTO Lessons (ModuleId, LessonType, Title, Content, LessonOrder, IsActive)
            VALUES (@ModuleId, 'theory', @Title, @Content, @Order, 1);
            SELECT SCOPE_IDENTITY();";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ModuleId", moduleId);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Content", content);
                command.Parameters.AddWithValue("@Order", order);

                var result = await command.ExecuteScalarAsync();
                return result == null ? null : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления теории: {ex.Message}");
                return null;
            }
        }

        // ПРАКТИКА с разными типами ответов
        public async Task<int?> AddPracticeWithAnswerTypeAsync(int courseId, string title, string description,
            string answerType, string? starterCode, string? expectedAnswer, string? hint, int order = 1)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var moduleId = await GetOrCreateModuleAsync(connection, courseId);

                // Создаем урок
                var lessonQuery = @"
            INSERT INTO Lessons (ModuleId, LessonType, Title, Content, LessonOrder, IsActive)
            VALUES (@ModuleId, 'practice', @Title, @Description, @Order, 1);
            SELECT SCOPE_IDENTITY();";

                using var lessonCommand = new SqlCommand(lessonQuery, connection);
                lessonCommand.Parameters.AddWithValue("@ModuleId", moduleId);
                lessonCommand.Parameters.AddWithValue("@Title", title);
                lessonCommand.Parameters.AddWithValue("@Description", description);
                lessonCommand.Parameters.AddWithValue("@Order", order);

                var lessonId = await lessonCommand.ExecuteScalarAsync();
                if (lessonId == null) return null;

                // Создаем практическое задание
                var practiceQuery = @"
            INSERT INTO PracticeExercises (LessonId, StarterCode, ExpectedOutput, TestCases, Hint, AnswerType)
            VALUES (@LessonId, @StarterCode, @ExpectedAnswer, @AnswerType, @Hint, @AnswerType)";

                using var practiceCommand = new SqlCommand(practiceQuery, connection);
                practiceCommand.Parameters.AddWithValue("@LessonId", Convert.ToInt32(lessonId));
                practiceCommand.Parameters.AddWithValue("@StarterCode", (object?)starterCode ?? DBNull.Value);
                practiceCommand.Parameters.AddWithValue("@ExpectedAnswer", (object?)expectedAnswer ?? DBNull.Value);
                practiceCommand.Parameters.AddWithValue("@AnswerType", answerType);
                practiceCommand.Parameters.AddWithValue("@Hint", (object?)hint ?? DBNull.Value);

                await practiceCommand.ExecuteNonQueryAsync();
                return Convert.ToInt32(lessonId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления практики: {ex.Message}");
                return null;
            }
        }

        // ТЕСТ с вопросами и ответами
        public async Task<int?> CreateTestWithQuestionsAsync(int courseId, string title, string description,
     int timeLimit, int passingScore, List<QuestionCreationModel> questions)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();

                try
                {
                    var moduleId = await GetOrCreateModuleAsync(connection, courseId, transaction);

                    // Создаем урок-тест
                    var lessonQuery = @"
                INSERT INTO Lessons (ModuleId, LessonType, Title, Content, LessonOrder, IsActive)
                VALUES (@ModuleId, 'test', @Title, @Description, 1, 1);
                SELECT SCOPE_IDENTITY();";

                    using var lessonCommand = new SqlCommand(lessonQuery, connection, transaction);
                    lessonCommand.Parameters.AddWithValue("@ModuleId", moduleId);
                    lessonCommand.Parameters.AddWithValue("@Title", title);
                    lessonCommand.Parameters.AddWithValue("@Description", description);

                    var lessonId = await lessonCommand.ExecuteScalarAsync();
                    if (lessonId == null)
                    {
                        transaction.Rollback();
                        return null;
                    }

                    // Создаем тест
                    var testQuery = @"
                INSERT INTO Tests (LessonId, Title, Description, TimeLimitMinutes, PassingScore)
                VALUES (@LessonId, @Title, @Description, @TimeLimit, @PassingScore);
                SELECT SCOPE_IDENTITY();";

                    using var testCommand = new SqlCommand(testQuery, connection, transaction);
                    testCommand.Parameters.AddWithValue("@LessonId", Convert.ToInt32(lessonId));
                    testCommand.Parameters.AddWithValue("@Title", title);
                    testCommand.Parameters.AddWithValue("@Description", description);
                    testCommand.Parameters.AddWithValue("@TimeLimit", timeLimit);
                    testCommand.Parameters.AddWithValue("@PassingScore", passingScore);

                    var testId = await testCommand.ExecuteScalarAsync();
                    if (testId == null)
                    {
                        transaction.Rollback();
                        return null;
                    }

                    // Добавляем вопросы и ответы
                    foreach (var question in questions)
                    {
                        var questionQuery = @"
                    INSERT INTO Questions (TestId, QuestionText, QuestionType, Score, QuestionOrder)
                    VALUES (@TestId, @QuestionText, @QuestionType, @Score, @QuestionOrder);
                    SELECT SCOPE_IDENTITY();";

                        using var questionCommand = new SqlCommand(questionQuery, connection, transaction);
                        questionCommand.Parameters.AddWithValue("@TestId", Convert.ToInt32(testId));
                        questionCommand.Parameters.AddWithValue("@QuestionText", question.QuestionText);
                        questionCommand.Parameters.AddWithValue("@QuestionType", question.QuestionType);
                        questionCommand.Parameters.AddWithValue("@Score", question.Score);
                        questionCommand.Parameters.AddWithValue("@QuestionOrder", questions.IndexOf(question) + 1);

                        var questionId = await questionCommand.ExecuteScalarAsync();
                        if (questionId == null)
                        {
                            transaction.Rollback();
                            return null;
                        }

                        // Добавляем варианты ответов
                        foreach (var answer in question.AnswerOptions)
                        {
                            var answerQuery = @"
                        INSERT INTO AnswerOptions (QuestionId, AnswerText, IsCorrect)
                        VALUES (@QuestionId, @AnswerText, @IsCorrect)";

                            using var answerCommand = new SqlCommand(answerQuery, connection, transaction);
                            answerCommand.Parameters.AddWithValue("@QuestionId", Convert.ToInt32(questionId));
                            answerCommand.Parameters.AddWithValue("@AnswerText", answer.AnswerText);
                            answerCommand.Parameters.AddWithValue("@IsCorrect", answer.IsCorrect);

                            await answerCommand.ExecuteNonQueryAsync();
                        }
                    }

                    transaction.Commit();
                    return Convert.ToInt32(lessonId);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания теста с вопросами: {ex.Message}");
                return null;
            }
        }

        // УДАЛЕНИЕ контента
        public async Task<bool> DeleteLessonAsync(int lessonId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Сначала проверяем тип урока
                var typeQuery = "SELECT LessonType FROM Lessons WHERE LessonId = @LessonId";
                using var typeCommand = new SqlCommand(typeQuery, connection);
                typeCommand.Parameters.AddWithValue("@LessonId", lessonId);
                var lessonType = await typeCommand.ExecuteScalarAsync() as string;

                using var transaction = connection.BeginTransaction();

                try
                {
                    // Удаляем в зависимости от типа урока
                    if (lessonType == "practice")
                    {
                        var deletePracticeQuery = "DELETE FROM PracticeExercises WHERE LessonId = @LessonId";
                        using var practiceCommand = new SqlCommand(deletePracticeQuery, connection, transaction);
                        practiceCommand.Parameters.AddWithValue("@LessonId", lessonId);
                        await practiceCommand.ExecuteNonQueryAsync();
                    }
                    else if (lessonType == "test")
                    {
                        // Получаем ID теста
                        var testIdQuery = "SELECT TestId FROM Tests WHERE LessonId = @LessonId";
                        using var testIdCommand = new SqlCommand(testIdQuery, connection, transaction);
                        testIdCommand.Parameters.AddWithValue("@LessonId", lessonId);
                        var testId = await testIdCommand.ExecuteScalarAsync();

                        if (testId != null)
                        {
                            // Удаляем ответы студентов
                            var deleteAnswersQuery = @"
                        DELETE sa FROM StudentAnswers sa
                        INNER JOIN TestAttempts ta ON sa.AttemptId = ta.AttemptId
                        WHERE ta.TestId = @TestId";
                            using var answersCommand = new SqlCommand(deleteAnswersQuery, connection, transaction);
                            answersCommand.Parameters.AddWithValue("@TestId", testId);
                            await answersCommand.ExecuteNonQueryAsync();

                            // Удаляем попытки
                            var deleteAttemptsQuery = "DELETE FROM TestAttempts WHERE TestId = @TestId";
                            using var attemptsCommand = new SqlCommand(deleteAttemptsQuery, connection, transaction);
                            attemptsCommand.Parameters.AddWithValue("@TestId", testId);
                            await attemptsCommand.ExecuteNonQueryAsync();

                            // Удаляем варианты ответов
                            var deleteOptionsQuery = @"
                        DELETE ao FROM AnswerOptions ao
                        INNER JOIN Questions q ON ao.QuestionId = q.QuestionId
                        WHERE q.TestId = @TestId";
                            using var optionsCommand = new SqlCommand(deleteOptionsQuery, connection, transaction);
                            optionsCommand.Parameters.AddWithValue("@TestId", testId);
                            await optionsCommand.ExecuteNonQueryAsync();

                            // Удаляем вопросы
                            var deleteQuestionsQuery = "DELETE FROM Questions WHERE TestId = @TestId";
                            using var questionsCommand = new SqlCommand(deleteQuestionsQuery, connection, transaction);
                            questionsCommand.Parameters.AddWithValue("@TestId", testId);
                            await questionsCommand.ExecuteNonQueryAsync();

                            // Удаляем тест
                            var deleteTestQuery = "DELETE FROM Tests WHERE TestId = @TestId";
                            using var testCommand = new SqlCommand(deleteTestQuery, connection, transaction);
                            testCommand.Parameters.AddWithValue("@TestId", testId);
                            await testCommand.ExecuteNonQueryAsync();
                        }
                    }

                    // Удаляем прогресс студентов
                    var deleteProgressQuery = "DELETE FROM StudentProgress WHERE LessonId = @LessonId";
                    using var progressCommand = new SqlCommand(deleteProgressQuery, connection, transaction);
                    progressCommand.Parameters.AddWithValue("@LessonId", lessonId);
                    await progressCommand.ExecuteNonQueryAsync();

                    // Удаляем урок
                    var deleteLessonQuery = "DELETE FROM Lessons WHERE LessonId = @LessonId";
                    using var lessonCommand = new SqlCommand(deleteLessonQuery, connection, transaction);
                    lessonCommand.Parameters.AddWithValue("@LessonId", lessonId);
                    var result = await lessonCommand.ExecuteNonQueryAsync();

                    transaction.Commit();
                    return result > 0;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления урока: {ex.Message}");
                return false;
            }
        }

        

        // Вспомогательный метод для получения/создания модуля
        private async Task<int> GetOrCreateModuleAsync(SqlConnection connection, int courseId, SqlTransaction? transaction = null)
        {
            var moduleQuery = @"
        SELECT TOP 1 ModuleId FROM CourseModules 
        WHERE CourseId = @CourseId 
        ORDER BY ModuleOrder";

            using var moduleCommand = transaction == null
                ? new SqlCommand(moduleQuery, connection)
                : new SqlCommand(moduleQuery, connection, transaction);

            moduleCommand.Parameters.AddWithValue("@CourseId", courseId);
            var moduleId = await moduleCommand.ExecuteScalarAsync();

            if (moduleId == null)
            {
                var createModuleQuery = @"
            INSERT INTO CourseModules (CourseId, ModuleName, ModuleOrder)
            VALUES (@CourseId, 'Основной модуль', 1);
            SELECT SCOPE_IDENTITY();";

                using var createModuleCommand = transaction == null
                    ? new SqlCommand(createModuleQuery, connection)
                    : new SqlCommand(createModuleQuery, connection, transaction);

                createModuleCommand.Parameters.AddWithValue("@CourseId", courseId);
                moduleId = await createModuleCommand.ExecuteScalarAsync();
            }

            return Convert.ToInt32(moduleId);
        }

        public async Task<string?> GetLessonContentAsync(int lessonId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT Content FROM Lessons WHERE LessonId = @LessonId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LessonId", lessonId);

                var result = await command.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения содержания урока: {ex.Message}");
                return null;
            }
        }

        public async Task<int?> GetCourseIdByLessonAsync(int lessonId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT c.CourseId 
            FROM Courses c
            INNER JOIN CourseModules m ON c.CourseId = m.CourseId
            INNER JOIN Lessons l ON m.ModuleId = l.ModuleId
            WHERE l.LessonId = @LessonId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LessonId", lessonId);

                var result = await command.ExecuteScalarAsync();
                return result == null ? null : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения ID курса: {ex.Message}");
                return null;
            }
        }

        private async Task<List<AnswerOption>> GetAnswerOptionsAsync(int questionId)
        {
            var options = new List<AnswerOption>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT AnswerId, AnswerText, IsCorrect
            FROM AnswerOptions
            WHERE QuestionId = @QuestionId
            ORDER BY AnswerId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@QuestionId", questionId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    options.Add(new AnswerOption
                    {
                        AnswerId = reader.GetInt32("AnswerId"),
                        AnswerText = reader.GetString("AnswerText"),
                        IsCorrect = reader.GetBoolean("IsCorrect")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки вариантов ответов: {ex.Message}");
            }
            return options;
        }
        public async Task<bool> SaveStudentAnswerAsync(int attemptId, int questionId, object userAnswer, bool isCorrect)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
            INSERT INTO StudentAnswers (AttemptId, QuestionId, SelectedAnswerId, CodeAnswer, IsCorrect)
            VALUES (@AttemptId, @QuestionId, @SelectedAnswerId, @CodeAnswer, @IsCorrect)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AttemptId", attemptId);
                command.Parameters.AddWithValue("@QuestionId", questionId);
                command.Parameters.AddWithValue("@IsCorrect", isCorrect);

                // Обрабатываем разные типы ответов
                if (userAnswer is int selectedAnswerId)
                {
                    command.Parameters.AddWithValue("@SelectedAnswerId", selectedAnswerId);
                    command.Parameters.AddWithValue("@CodeAnswer", DBNull.Value);
                }
                else if (userAnswer is List<int> selectedAnswers)
                {
                    // Для множественного выбора сохраняем первый выбранный ответ (можно улучшить)
                    command.Parameters.AddWithValue("@SelectedAnswerId", selectedAnswers.FirstOrDefault());
                    command.Parameters.AddWithValue("@CodeAnswer", DBNull.Value);
                }
                else if (userAnswer is string textAnswer)
                {
                    command.Parameters.AddWithValue("@SelectedAnswerId", DBNull.Value);
                    command.Parameters.AddWithValue("@CodeAnswer", textAnswer);
                }
                else
                {
                    command.Parameters.AddWithValue("@SelectedAnswerId", DBNull.Value);
                    command.Parameters.AddWithValue("@CodeAnswer", DBNull.Value);
                }

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения ответа студента: {ex.Message}");
                return false;
            }
        }

        public async Task<PracticeDto?> GetPracticeExerciseAsync(int lessonId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"SELECT LessonId, StarterCode, ExpectedOutput, TestCases, Hint, Description 
                      FROM PracticeExercises WHERE LessonId=@LessonId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LessonId", lessonId);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new PracticeDto
                    {
                        LessonId = reader.GetInt32("LessonId"),
                        StarterCode = reader.IsDBNull("StarterCode") ? null : reader.GetString("StarterCode"),
                        ExpectedOutput = reader.IsDBNull("ExpectedOutput") ? null : reader.GetString("ExpectedOutput"),
                        TestCasesJson = reader.IsDBNull("TestCases") ? null : reader.GetString("TestCases"),
                        Hint = reader.IsDBNull("Hint") ? null : reader.GetString("Hint"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки практики: {ex.Message}");
            }
            return null;
        }


        public async Task<int?> AddPracticeWithAnswerTypeAsync(
            int courseId,
            string title,
            string description,
            string answerType,
            string starterCode,
            string expectedAnswer,
            string hint,
            int maxFileSize = 10,
            string allowedFileTypes = "")
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var moduleId = await GetOrCreateModuleAsync(connection, courseId);

                // Создаем урок
                var lessonQuery = @"
            INSERT INTO Lessons (ModuleId, LessonType, Title, Content, LessonOrder, IsActive)
            VALUES (@ModuleId, 'practice', @Title, @Description, 1, 1);
            SELECT SCOPE_IDENTITY();";

                using var lessonCommand = new SqlCommand(lessonQuery, connection);
                lessonCommand.Parameters.AddWithValue("@ModuleId", moduleId);
                lessonCommand.Parameters.AddWithValue("@Title", title);
                lessonCommand.Parameters.AddWithValue("@Description", description);

                var lessonId = await lessonCommand.ExecuteScalarAsync();
                if (lessonId == null) return null;

                // Создаем практическое задание с новыми полями
                var practiceQuery = @"
            INSERT INTO PracticeExercises (LessonId, StarterCode, ExpectedOutput, TestCases, Hint, AnswerType, MaxFileSize, AllowedFileTypes)
            VALUES (@LessonId, @StarterCode, @ExpectedAnswer, @AnswerType, @Hint, @AnswerType, @MaxFileSize, @AllowedFileTypes)";

                using var practiceCommand = new SqlCommand(practiceQuery, connection);
                practiceCommand.Parameters.AddWithValue("@LessonId", Convert.ToInt32(lessonId));
                practiceCommand.Parameters.AddWithValue("@StarterCode", (object?)starterCode ?? DBNull.Value);
                practiceCommand.Parameters.AddWithValue("@ExpectedAnswer", (object?)expectedAnswer ?? DBNull.Value);
                practiceCommand.Parameters.AddWithValue("@AnswerType", answerType);
                practiceCommand.Parameters.AddWithValue("@Hint", (object?)hint ?? DBNull.Value);
                practiceCommand.Parameters.AddWithValue("@MaxFileSize", maxFileSize);
                practiceCommand.Parameters.AddWithValue("@AllowedFileTypes", (object?)allowedFileTypes ?? DBNull.Value);

                await practiceCommand.ExecuteNonQueryAsync();
                return Convert.ToInt32(lessonId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления практики: {ex.Message}");
                return null;
            }
        }

        public async Task<List<CourseLesson>> GetCourseLessonsAsync(int courseId)
        {
            var lessons = new List<CourseLesson>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT l.LessonId, l.ModuleId, l.LessonType, l.Title, l.Content, l.LessonOrder, l.IsActive
            FROM Lessons l
            INNER JOIN CourseModules m ON l.ModuleId = m.ModuleId
            WHERE m.CourseId = @CourseId
            ORDER BY l.LessonOrder";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    lessons.Add(new CourseLesson
                    {
                        LessonId = reader.GetInt32("LessonId"),
                        ModuleId = reader.GetInt32("ModuleId"),
                        LessonType = reader.GetString("LessonType"),
                        Title = reader.GetString("Title"),
                        Content = reader.IsDBNull("Content") ? null : reader.GetString("Content"),
                        LessonOrder = reader.GetInt32("LessonOrder"),
                        IsActive = reader.GetBoolean("IsActive")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки уроков курса: {ex.Message}");
            }
            return lessons;
        }

        // Методы для работы с теорией
        public async Task<bool> UpdateTheoryLessonAsync(int lessonId, string title, string content, int order)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE Lessons 
            SET Title = @Title, 
                Content = @Content,
                LessonOrder = @Order
            WHERE LessonId = @LessonId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LessonId", lessonId);
                command.Parameters.AddWithValue("@Title", title ?? "");
                command.Parameters.AddWithValue("@Content", content ?? "");
                command.Parameters.AddWithValue("@Order", order);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления теории: {ex.Message}");
                return false;
            }
        }

        // Методы для работы с практическими заданиями
        public async Task<bool> UpdatePracticeExerciseAsync(PracticeDto practice)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Сначала обновляем урок
                var lessonQuery = @"
            UPDATE Lessons 
            SET Title = @Title, 
                Content = @Description
            WHERE LessonId = @LessonId";

                using var lessonCommand = new SqlCommand(lessonQuery, connection);
                lessonCommand.Parameters.AddWithValue("@LessonId", practice.LessonId);
                lessonCommand.Parameters.AddWithValue("@Title", practice.Title ?? "");
                lessonCommand.Parameters.AddWithValue("@Description", practice.Description ?? "");
                await lessonCommand.ExecuteNonQueryAsync();

                // Проверяем существование практического задания
                var checkPracticeQuery = "SELECT COUNT(*) FROM PracticeExercises WHERE LessonId = @LessonId";
                using var checkCommand = new SqlCommand(checkPracticeQuery, connection);
                checkCommand.Parameters.AddWithValue("@LessonId", practice.LessonId);
                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                string practiceQuery;
                if (exists)
                {
                    // Обновляем существующее задание
                    practiceQuery = @"
                UPDATE PracticeExercises 
                SET StarterCode = @StarterCode,
                    ExpectedOutput = @ExpectedOutput,
                    TestCases = @TestCases,
                    Hint = @Hint
                WHERE LessonId = @LessonId";
                }
                else
                {
                    // Создаем новое задание
                    practiceQuery = @"
                INSERT INTO PracticeExercises (LessonId, StarterCode, ExpectedOutput, TestCases, Hint)
                VALUES (@LessonId, @StarterCode, @ExpectedOutput, @TestCases, @Hint)";
                }

                using var practiceCommand = new SqlCommand(practiceQuery, connection);
                practiceCommand.Parameters.AddWithValue("@LessonId", practice.LessonId);
                practiceCommand.Parameters.AddWithValue("@StarterCode", (object?)practice.StarterCode ?? DBNull.Value);
                practiceCommand.Parameters.AddWithValue("@ExpectedOutput", (object?)practice.ExpectedOutput ?? DBNull.Value);
                practiceCommand.Parameters.AddWithValue("@TestCases", (object?)practice.TestCasesJson ?? DBNull.Value);
                practiceCommand.Parameters.AddWithValue("@Hint", (object?)practice.Hint ?? DBNull.Value);

                var result = await practiceCommand.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления практики: {ex.Message}");
                return false;
            }
        }

        public async Task<PracticeDto?> GetPracticeExerciseWithLessonDataAsync(int lessonId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT l.LessonId, l.Title, l.Content as Description,
                   pe.StarterCode, pe.ExpectedOutput, pe.TestCases, 
                   pe.Hint
            FROM Lessons l
            LEFT JOIN PracticeExercises pe ON l.LessonId = pe.LessonId
            WHERE l.LessonId = @LessonId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LessonId", lessonId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new PracticeDto
                    {
                        LessonId = reader.GetInt32("LessonId"),
                        Title = reader.IsDBNull("Title") ? null : reader.GetString("Title"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        StarterCode = reader.IsDBNull("StarterCode") ? null : reader.GetString("StarterCode"),
                        ExpectedOutput = reader.IsDBNull("ExpectedOutput") ? null : reader.GetString("ExpectedOutput"),
                        TestCasesJson = reader.IsDBNull("TestCases") ? null : reader.GetString("TestCases"),
                        Hint = reader.IsDBNull("Hint") ? null : reader.GetString("Hint")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки практики с данными урока: {ex.Message}");
            }
            return null;
        }

        // Методы для работы с тестами
        public async Task<bool> UpdateTestMetaAsync(int testId, string title, string description, int timeLimit, int passingScore)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE Tests 
            SET Title = @Title,
                Description = @Description,
                TimeLimitMinutes = @TimeLimit,
                PassingScore = @PassingScore
            WHERE TestId = @TestId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@TestId", testId);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Description", description ?? "");
                command.Parameters.AddWithValue("@TimeLimit", timeLimit);
                command.Parameters.AddWithValue("@PassingScore", passingScore);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления теста: {ex.Message}");
                return false;
            }
        }

        public async Task<TestMeta?> GetTestMetaByLessonAsync(int lessonId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT t.TestId, t.Title, t.Description, t.TimeLimitMinutes, t.PassingScore, t.MaxScore
            FROM Tests t
            WHERE t.LessonId = @LessonId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LessonId", lessonId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new TestMeta
                    {
                        TestId = reader.GetInt32("TestId"),
                        Title = reader.GetString("Title"),
                        Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                        TimeLimitMinutes = reader.GetInt32("TimeLimitMinutes"),
                        PassingScore = reader.GetInt32("PassingScore"),
                        MaxScore = reader.GetInt32("MaxScore")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения метаданных теста: {ex.Message}");
            }
            return null;
        }

        public async Task<List<Question>> GetTestQuestionsAsync(int testId)
        {
            var questions = new List<Question>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT q.QuestionId, q.QuestionText, q.QuestionType, q.Score, q.QuestionOrder
            FROM Questions q
            WHERE q.TestId = @TestId
            ORDER BY q.QuestionOrder";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@TestId", testId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var question = new Question
                    {
                        QuestionId = reader.GetInt32("QuestionId"),
                        QuestionText = reader.GetString("QuestionText"),
                        QuestionType = reader.GetString("QuestionType"),
                        Score = reader.GetInt32("Score"),
                        QuestionOrder = reader.GetInt32("QuestionOrder")
                    };

                    // Загружаем варианты ответов для этого вопроса
                    question.AnswerOptions = await GetAnswerOptionsAsync(question.QuestionId);
                    questions.Add(question);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки вопросов теста: {ex.Message}");
            }
            return questions;
        }


        public async Task<int?> AddQuestionAsync(int testId, string questionText, string questionType, int score, int order)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO Questions (TestId, QuestionText, QuestionType, Score, QuestionOrder)
            VALUES (@TestId, @QuestionText, @QuestionType, @Score, @Order);
            SELECT SCOPE_IDENTITY();";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@TestId", testId);
                command.Parameters.AddWithValue("@QuestionText", questionText);
                command.Parameters.AddWithValue("@QuestionType", questionType);
                command.Parameters.AddWithValue("@Score", score);
                command.Parameters.AddWithValue("@Order", order);

                var result = await command.ExecuteScalarAsync();
                return result == null ? null : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления вопроса: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> AddAnswerOptionAsync(int questionId, string answerText, bool isCorrect)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO AnswerOptions (QuestionId, AnswerText, IsCorrect)
            VALUES (@QuestionId, @AnswerText, @IsCorrect)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@QuestionId", questionId);
                command.Parameters.AddWithValue("@AnswerText", answerText);
                command.Parameters.AddWithValue("@IsCorrect", isCorrect);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления варианта ответа: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteQuestionAsync(int questionId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();

                try
                {
                    // Сначала удаляем варианты ответов
                    var deleteOptionsQuery = "DELETE FROM AnswerOptions WHERE QuestionId = @QuestionId";
                    using var optionsCommand = new SqlCommand(deleteOptionsQuery, connection, transaction);
                    optionsCommand.Parameters.AddWithValue("@QuestionId", questionId);
                    await optionsCommand.ExecuteNonQueryAsync();

                    // Затем удаляем вопрос
                    var deleteQuestionQuery = "DELETE FROM Questions WHERE QuestionId = @QuestionId";
                    using var questionCommand = new SqlCommand(deleteQuestionQuery, connection, transaction);
                    questionCommand.Parameters.AddWithValue("@QuestionId", questionId);
                    var result = await questionCommand.ExecuteNonQueryAsync();

                    transaction.Commit();
                    return result > 0;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления вопроса: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateQuestionAsync(int questionId, string questionText, string questionType, int score)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE Questions 
            SET QuestionText = @QuestionText,
                QuestionType = @QuestionType,
                Score = @Score
            WHERE QuestionId = @QuestionId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@QuestionId", questionId);
                command.Parameters.AddWithValue("@QuestionText", questionText);
                command.Parameters.AddWithValue("@QuestionType", questionType);
                command.Parameters.AddWithValue("@Score", score);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления вопроса: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveStudentFromGroupAsync(int groupId, int studentId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"UPDATE GroupEnrollments SET Status='dropped' 
                      WHERE GroupId=@GroupId AND StudentId=@StudentId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@StudentId", studentId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления студента из группы: {ex.Message}");
                return false;
            }
        }

        // Получение всех студентов (для выбора)
        public async Task<List<User>> GetAllStudentsAsync()
        {
            var students = new List<User>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT UserId, Username, FirstName, LastName, Email, RoleId
            FROM Users 
            WHERE RoleId = 1 AND IsActive = 1
            ORDER BY Username";

                using var command = new SqlCommand(query, connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    students.Add(new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        FirstName = reader.IsDBNull("FirstName") ? null : reader.GetString("FirstName"),
                        LastName = reader.IsDBNull("LastName") ? null : reader.GetString("LastName"),
                        Email = reader.GetString("Email"),
                        RoleId = reader.GetInt32("RoleId")
                    });
                }

                Console.WriteLine($"✅ Загружено студентов из БД: {students.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки студентов: {ex.Message}");
            }
            return students;
        }

        public async Task DebugGroupState(int groupId)
        {
            try
            {
                Console.WriteLine($"\n🔍 ===== ДИАГНОСТИКА ГРУППЫ {groupId} =====");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. Информация о группе
                var groupQuery = @"
        SELECT sg.GroupName, c.CourseName, sg.TeacherId
        FROM StudyGroups sg
        INNER JOIN Courses c ON sg.CourseId = c.CourseId
        WHERE sg.GroupId = @GroupId";

                using var groupCmd = new SqlCommand(groupQuery, connection);
                groupCmd.Parameters.AddWithValue("@GroupId", groupId);

                using var groupReader = await groupCmd.ExecuteReaderAsync();
                if (await groupReader.ReadAsync())
                {
                    Console.WriteLine($"📋 Группа: {groupReader.GetString("GroupName")}");
                    Console.WriteLine($"📚 Курс: {groupReader.GetString("CourseName")}");
                    Console.WriteLine($"👨‍🏫 Преподаватель ID: {groupReader.GetInt32("TeacherId")}");
                }
                await groupReader.CloseAsync();

                // 2. Студенты в группе
                var studentsQuery = @"
        SELECT COUNT(*) as StudentCount 
        FROM GroupEnrollments 
        WHERE GroupId = @GroupId AND Status = 'active'";

                using var studentsCmd = new SqlCommand(studentsQuery, connection);
                studentsCmd.Parameters.AddWithValue("@GroupId", groupId);
                var studentCount = Convert.ToInt32(await studentsCmd.ExecuteScalarAsync());
                Console.WriteLine($"🎓 Студентов в GroupEnrollments: {studentCount}");

                // 3. Участники чата
                var chatMembersQuery = @"
        SELECT COUNT(*) as ChatMemberCount 
        FROM GroupChatMembers 
        WHERE GroupId = @GroupId";

                using var chatCmd = new SqlCommand(chatMembersQuery, connection);
                chatCmd.Parameters.AddWithValue("@GroupId", groupId);
                var chatMemberCount = Convert.ToInt32(await chatCmd.ExecuteScalarAsync());
                Console.WriteLine($"💬 Участников в GroupChatMembers: {chatMemberCount}");

                // 4. Сообщения в чате
                var messagesQuery = @"
        SELECT COUNT(*) as MessageCount 
        FROM GroupChatMessages 
        WHERE GroupId = @GroupId";

                using var messagesCmd = new SqlCommand(messagesQuery, connection);
                messagesCmd.Parameters.AddWithValue("@GroupId", groupId);
                var messageCount = Convert.ToInt32(await messagesCmd.ExecuteScalarAsync());
                Console.WriteLine($"💌 Сообщений в GroupChatMessages: {messageCount}");

                // 5. Детальный список участников чата
                var detailedChatQuery = @"
        SELECT gcm.UserId, u.Username 
        FROM GroupChatMembers gcm
        INNER JOIN Users u ON gcm.UserId = u.UserId
        WHERE gcm.GroupId = @GroupId";

                using var detailedCmd = new SqlCommand(detailedChatQuery, connection);
                detailedCmd.Parameters.AddWithValue("@GroupId", groupId);

                using var detailedReader = await detailedCmd.ExecuteReaderAsync();
                Console.WriteLine($"👥 Детальный список участников чата:");
                while (await detailedReader.ReadAsync())
                {
                    Console.WriteLine($"   - {detailedReader.GetString("Username")} (ID: {detailedReader.GetInt32("UserId")})");
                }
                await detailedReader.CloseAsync();

                Console.WriteLine($"🔍 ===== ДИАГНОСТИКА ЗАВЕРШЕНА =====\n");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка диагностики: {ex.Message}");
            }
        }

        public async Task<bool> AddStudentsToGroupAsync(int groupId, List<User> students)
        {
            try
            {
                Console.WriteLine($"🔧 УНИВЕРСАЛЬНЫЙ МЕТОД: добавляем {students.Count} студентов в группу {groupId}");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();



                using var transaction = connection.BeginTransaction();

                try
                {
                    int addedCount = 0;

                    foreach (var student in students)
                    {
                        Console.WriteLine($"👤 Обрабатываем студента: {student.Username} (ID: {student.UserId})");

                        // Шаг 1: Проверяем и добавляем в GroupEnrollments
                        var enrollQuery = @"
                IF NOT EXISTS (SELECT 1 FROM GroupEnrollments WHERE GroupId = @GroupId AND StudentId = @StudentId AND Status = 'active')
                BEGIN
                    INSERT INTO GroupEnrollments (GroupId, StudentId, Status, EnrolledDate)
                    VALUES (@GroupId, @StudentId, 'active', GETDATE())
                    PRINT '✅ Добавлен в GroupEnrollments'
                END
                ELSE
                BEGIN
                    PRINT 'ℹ️ Уже в GroupEnrollments'
                END";

                        using var enrollCmd = new SqlCommand(enrollQuery, connection, transaction);
                        enrollCmd.Parameters.AddWithValue("@GroupId", groupId);
                        enrollCmd.Parameters.AddWithValue("@StudentId", student.UserId);
                        var enrollResult = await enrollCmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"📝 GroupEnrollments результат: {enrollResult}");

                        // Шаг 2: Проверяем и добавляем в GroupChatMembers
                        var chatQuery = @"
                IF NOT EXISTS (SELECT 1 FROM GroupChatMembers WHERE GroupId = @GroupId AND UserId = @UserId)
                BEGIN
                    INSERT INTO GroupChatMembers (GroupId, UserId)
                    VALUES (@GroupId, @UserId)
                    PRINT '✅ Добавлен в GroupChatMembers'
                END
                ELSE
                BEGIN
                    PRINT 'ℹ️ Уже в GroupChatMembers'
                END";

                        using var chatCmd = new SqlCommand(chatQuery, connection, transaction);
                        chatCmd.Parameters.AddWithValue("@GroupId", groupId);
                        chatCmd.Parameters.AddWithValue("@UserId", student.UserId);
                        var chatResult = await chatCmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"📝 GroupChatMembers результат: {chatResult}");

                        addedCount++;
                        Console.WriteLine($"🎯 Студент {student.Username} полностью обработан");
                    }

                    // Шаг 3: Добавляем системное сообщение
                    if (addedCount > 0)
                    {
                        var messageQuery = @"
                INSERT INTO GroupChatMessages (GroupId, SenderId, MessageText, SentAt, IsRead, IsSystemMessage)
                VALUES (@GroupId, 0, @MessageText, GETDATE(), 0, 1)";

                        using var msgCmd = new SqlCommand(messageQuery, connection, transaction);
                        msgCmd.Parameters.AddWithValue("@GroupId", groupId);
                        msgCmd.Parameters.AddWithValue("@MessageText", $"🎉 В группу добавлено {addedCount} новых студентов!");
                        await msgCmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"📢 Системное сообщение добавлено");
                    }

                    transaction.Commit();
                    Console.WriteLine($"✅ Транзакция завершена успешно. Добавлено: {addedCount} студентов");
                    return addedCount > 0;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"❌ ОШИБКА ТРАНЗАКЦИИ: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
                Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // Автоматическая запись всей группы на курс
        public async Task<bool> EnrollGroupToCourseAsync(int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Получаем ID курса группы
                var courseQuery = "SELECT CourseId FROM StudyGroups WHERE GroupId = @GroupId";
                using var courseCommand = new SqlCommand(courseQuery, connection);
                courseCommand.Parameters.AddWithValue("@GroupId", groupId);
                var courseId = await courseCommand.ExecuteScalarAsync();

                if (courseId == null)
                {
                    Console.WriteLine("Группа не найдена или не привязана к курсу");
                    return false;
                }

                // Получаем всех активных студентов группы
                var studentsQuery = @"
            SELECT StudentId 
            FROM GroupEnrollments 
            WHERE GroupId = @GroupId AND Status = 'active'";

                using var studentsCommand = new SqlCommand(studentsQuery, connection);
                studentsCommand.Parameters.AddWithValue("@GroupId", groupId);

                var studentIds = new List<int>();
                using (var reader = await studentsCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        studentIds.Add(reader.GetInt32("StudentId"));
                    }
                }

                // Записываем каждого студента на курс
                using var transaction = connection.BeginTransaction();
                try
                {
                    foreach (var studentId in studentIds)
                    {
                        // Проверяем, не записан ли студент уже на курс
                        var checkProgressQuery = @"
                    SELECT COUNT(*) 
                    FROM StudentProgress 
                    WHERE StudentId = @StudentId AND CourseId = @CourseId";

                        using var checkCommand = new SqlCommand(checkProgressQuery, connection, transaction);
                        checkCommand.Parameters.AddWithValue("@StudentId", studentId);
                        checkCommand.Parameters.AddWithValue("@CourseId", courseId);

                        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                        if (!exists)
                        {
                            var enrollQuery = @"
                        INSERT INTO StudentProgress (StudentId, CourseId, Status, StartDate)
                        VALUES (@StudentId, @CourseId, 'not_started', GETDATE())";

                            using var enrollCommand = new SqlCommand(enrollQuery, connection, transaction);
                            enrollCommand.Parameters.AddWithValue("@StudentId", studentId);
                            enrollCommand.Parameters.AddWithValue("@CourseId", courseId);
                            await enrollCommand.ExecuteNonQueryAsync();
                        }
                    }

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи группы на курс: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkMessagesAsReadAsync(int groupId, int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE GroupChatMessages 
            SET IsRead = 1 
            WHERE GroupId = @GroupId 
            AND SenderId != @UserId 
            AND IsRead = 0";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отметки сообщений как прочитанных: {ex.Message}");
                return false;
            }
        }

        // Метод для получения количества непрочитанных сообщений
        public async Task<int> GetUnreadMessagesCountAsync(int groupId, int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT COUNT(*) 
            FROM GroupChatMessages 
            WHERE GroupId = @GroupId 
            AND SenderId != @UserId 
            AND IsRead = 0";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                return result == null ? 0 : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения непрочитанных сообщений: {ex.Message}");
                return 0;
            }
        }


        public async Task<List<StudentChatItem>> GetStudentAllChatsAsync(int studentId)
        {
            var chats = new List<StudentChatItem>();

            try
            {
                Console.WriteLine($"🔍 Загружаем чаты для студента {studentId} через таблицу участников");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
    SELECT DISTINCT
        sg.GroupId,
        sg.GroupName,
        c.CourseName,
        u.FirstName + ' ' + u.LastName as TeacherName,
        (SELECT COUNT(DISTINCT UserId) FROM GroupChatMembers WHERE GroupId = sg.GroupId) as ParticipantCount,
        
        -- Последнее сообщение
        (SELECT TOP 1 MessageText FROM GroupChatMessages WHERE GroupId = sg.GroupId ORDER BY SentAt DESC) as LastMessage,
        (SELECT TOP 1 SentAt FROM GroupChatMessages WHERE GroupId = sg.GroupId ORDER BY SentAt DESC) as LastMessageDate,
        
        -- Непрочитанные сообщения (только для текущего пользователя)
        (SELECT COUNT(*) FROM GroupChatMessages 
         WHERE GroupId = sg.GroupId 
         AND SenderId != @StudentId 
         AND IsRead = 0) as UnreadCount

    FROM StudyGroups sg
    INNER JOIN GroupChatMembers gcm ON sg.GroupId = gcm.GroupId
    INNER JOIN Courses c ON sg.CourseId = c.CourseId
    INNER JOIN Users u ON sg.TeacherId = u.UserId
    
    WHERE gcm.UserId = @StudentId
        AND sg.IsActive = 1
        
    ORDER BY LastMessageDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentId", studentId);

                using var reader = await command.ExecuteReaderAsync();

                int chatCount = 0;
                while (await reader.ReadAsync())
                {
                    var chat = new StudentChatItem
                    {
                        ChatId = reader.GetInt32("GroupId"),
                        ChatName = reader.GetString("GroupName"),
                        ChatType = "group",
                        Description = reader.GetString("CourseName"),
                        ParticipantCount = reader.GetInt32("ParticipantCount"),
                        TeacherName = reader.GetString("TeacherName"),
                        CourseName = reader.GetString("CourseName"),
                        GroupId = reader.GetInt32("GroupId"),
                        Avatar = "default_avatar.png",
                        LastMessage = reader.IsDBNull("LastMessage") ? "Чат создан" : reader.GetString("LastMessage"),
                        LastMessageTime = reader.IsDBNull("LastMessageDate") ? DateTime.Now : reader.GetDateTime("LastMessageDate"),
                        UnreadMessages = reader.GetInt32("UnreadCount")
                    };

                    chats.Add(chat);
                    chatCount++;

                    Console.WriteLine($"💬 Найден чат: {chat.ChatName}, участников: {chat.ParticipantCount}, непрочитанных: {chat.UnreadMessages}");
                }

                Console.WriteLine($"✅ Загружено {chatCount} чатов для студента {studentId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки чатов: {ex.Message}");
                Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");
            }

            return chats;
        }


        public async Task DebugGroupChatState(int groupId)
        {
            try
            {
                Console.WriteLine($"🔍 ДИАГНОСТИКА ЧАТА ГРУППЫ {groupId}");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. Проверяем существование группы
                var groupQuery = "SELECT GroupName FROM StudyGroups WHERE GroupId = @GroupId";
                using var groupCmd = new SqlCommand(groupQuery, connection);
                groupCmd.Parameters.AddWithValue("@GroupId", groupId);
                var groupName = await groupCmd.ExecuteScalarAsync();
                Console.WriteLine($"📋 Группа: {(groupName?.ToString() ?? "НЕ НАЙДЕНА")}");

                // 2. Проверяем таблицу GroupChatMembers
                var membersQuery = "SELECT COUNT(*) FROM GroupChatMembers WHERE GroupId = @GroupId";
                using var membersCmd = new SqlCommand(membersQuery, connection);
                membersCmd.Parameters.AddWithValue("@GroupId", groupId);
                var membersCount = Convert.ToInt32(await membersCmd.ExecuteScalarAsync());
                Console.WriteLine($"👥 Участников в чате: {membersCount}");

                // 3. Проверяем сообщения в чате
                var messagesQuery = "SELECT COUNT(*) FROM GroupChatMessages WHERE GroupId = @GroupId";
                using var messagesCmd = new SqlCommand(messagesQuery, connection);
                messagesCmd.Parameters.AddWithValue("@GroupId", groupId);
                var messagesCount = Convert.ToInt32(await messagesCmd.ExecuteScalarAsync());
                Console.WriteLine($"💬 Сообщений в чате: {messagesCount}");

                // 4. Проверяем студентов в группе
                var studentsQuery = @"SELECT COUNT(*) FROM GroupEnrollments 
                            WHERE GroupId = @GroupId AND Status = 'active'";
                using var studentsCmd = new SqlCommand(studentsQuery, connection);
                studentsCmd.Parameters.AddWithValue("@GroupId", groupId);
                var studentsCount = Convert.ToInt32(await studentsCmd.ExecuteScalarAsync());
                Console.WriteLine($"🎓 Студентов в группе: {studentsCount}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка диагностики: {ex.Message}");
            }
        }

        // Для учителя: получение количества непрочитанных сообщений в группах
        public async Task<Dictionary<int, int>> GetTeacherUnreadMessagesCountAsync(int teacherId)
        {
            var unreadCounts = new Dictionary<int, int>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT sg.GroupId, COUNT(gcm.MessageId) as UnreadCount
            FROM StudyGroups sg
            LEFT JOIN GroupChatMessages gcm ON sg.GroupId = gcm.GroupId 
                AND gcm.SenderId != @TeacherId 
                AND gcm.IsRead = 0
            WHERE sg.TeacherId = @TeacherId AND sg.IsActive = 1
            GROUP BY sg.GroupId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@TeacherId", teacherId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var groupId = reader.GetInt32("GroupId");
                    var unreadCount = reader.GetInt32("UnreadCount");
                    unreadCounts[groupId] = unreadCount;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения непрочитанных сообщений учителя: {ex.Message}");
            }
            return unreadCounts;
        }

        // Для студента: получение количества непрочитанных сообщений во всех чатах
        public async Task<Dictionary<(int ChatId, string ChatType), int>> GetStudentUnreadMessagesCountAsync(int studentId)
        {
            var unreadCounts = new Dictionary<(int, string), int>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Групповые чаты
                var groupQuery = @"
            SELECT sg.GroupId, COUNT(gcm.MessageId) as UnreadCount
            FROM StudyGroups sg
            INNER JOIN GroupChatMembers gcmem ON sg.GroupId = gcmem.GroupId
            LEFT JOIN GroupChatMessages gcm ON sg.GroupId = gcm.GroupId 
                AND gcm.SenderId != @StudentId 
                AND gcm.IsRead = 0
            WHERE gcmem.UserId = @StudentId AND sg.IsActive = 1
            GROUP BY sg.GroupId";

                using var groupCommand = new SqlCommand(groupQuery, connection);
                groupCommand.Parameters.AddWithValue("@StudentId", studentId);

                using var groupReader = await groupCommand.ExecuteReaderAsync();
                while (await groupReader.ReadAsync())
                {
                    var groupId = groupReader.GetInt32("GroupId");
                    var unreadCount = groupReader.GetInt32("UnreadCount");
                    unreadCounts[(groupId, "group")] = unreadCount;
                }
                await groupReader.CloseAsync();

                // Приватные чаты с учителями
                var privateQuery = @"
            SELECT DISTINCT pc.SenderId as TeacherId, COUNT(pc.MessageId) as UnreadCount
            FROM PrivateChats pc
            WHERE pc.ReceiverId = @StudentId 
                AND pc.IsRead = 0
            GROUP BY pc.SenderId";

                using var privateCommand = new SqlCommand(privateQuery, connection);
                privateCommand.Parameters.AddWithValue("@StudentId", studentId);

                using var privateReader = await privateCommand.ExecuteReaderAsync();
                while (await privateReader.ReadAsync())
                {
                    var teacherId = privateReader.GetInt32("TeacherId");
                    var unreadCount = privateReader.GetInt32("UnreadCount");
                    unreadCounts[(teacherId, "teacher")] = unreadCount;
                }
                await privateReader.CloseAsync();

                // Чат поддержки
                var supportQuery = @"
            SELECT COUNT(*) as UnreadCount
            FROM SupportChats 
            WHERE UserId = @StudentId 
                AND SenderId != @StudentId 
                AND IsRead = 0";

                using var supportCommand = new SqlCommand(supportQuery, connection);
                supportCommand.Parameters.AddWithValue("@StudentId", studentId);

                var supportUnread = Convert.ToInt32(await supportCommand.ExecuteScalarAsync());
                unreadCounts[(1, "support")] = supportUnread; // ID 1 для поддержки

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения непрочитанных сообщений студента: {ex.Message}");
            }
            return unreadCounts;
        }

        // Метод для отметки сообщений как прочитанных в групповом чате
        public async Task<bool> MarkGroupMessagesAsReadAsync(int groupId, int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE GroupChatMessages 
            SET IsRead = 1 
            WHERE GroupId = @GroupId 
            AND SenderId != @UserId 
            AND IsRead = 0";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteNonQueryAsync();
                Console.WriteLine($"✅ Отмечено {result} сообщений как прочитанных в группе {groupId}");
                return result >= 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отметки групповых сообщений: {ex.Message}");
                return false;
            }
        }

        // Метод для отметки приватных сообщений как прочитанных
        public async Task<bool> MarkPrivateMessagesAsReadAsync(int userId, int teacherId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE PrivateChats 
            SET IsRead = 1 
            WHERE SenderId = @TeacherId 
            AND ReceiverId = @UserId 
            AND IsRead = 0";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@TeacherId", teacherId);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteNonQueryAsync();
                Console.WriteLine($"✅ Отмечено {result} приватных сообщений как прочитанных");
                return result >= 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отметки приватных сообщений: {ex.Message}");
                return false;
            }
        }

        // Метод для отметки сообщений поддержки как прочитанных
        public async Task<bool> MarkSupportMessagesAsReadAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            UPDATE SupportChats 
            SET IsRead = 1 
            WHERE UserId = @UserId 
            AND SenderId != @UserId 
            AND IsRead = 0";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteNonQueryAsync();
                Console.WriteLine($"✅ Отмечено {result} сообщений поддержки как прочитанных");
                return result >= 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отметки сообщений поддержки: {ex.Message}");
                return false;
            }
        }

        public async Task CheckTableStructure()
        {
            try
            {
                Console.WriteLine("🔍 ПРОВЕРКА СТРУКТУРЫ ТАБЛИЦЫ GroupChatMembers");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT 
                COLUMN_NAME,
                DATA_TYPE,
                IS_NULLABLE,
                COLUMN_DEFAULT
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = 'GroupChatMembers'
            ORDER BY ORDINAL_POSITION";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"📊 {reader.GetString("COLUMN_NAME")} - {reader.GetString("DATA_TYPE")} - NULL: {reader.GetString("IS_NULLABLE")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка проверки структуры: {ex.Message}");
            }
        }

        public async Task InitializeChatMembersTable()
        {
            try
            {
                Console.WriteLine("🛠️ Проверяем и создаем таблицу GroupChatMembers...");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var createTableQuery = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GroupChatMembers')
            BEGIN
                CREATE TABLE GroupChatMembers (
                    GroupId INT NOT NULL FOREIGN KEY REFERENCES StudyGroups(GroupId),
                    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
                    PRIMARY KEY (GroupId, UserId)
                );
                PRINT '✅ Таблица GroupChatMembers создана';
            END
            ELSE
            BEGIN
                PRINT '✅ Таблица GroupChatMembers уже существует';
            END";

                using var command = new SqlCommand(createTableQuery, connection);
                await command.ExecuteNonQueryAsync();

                Console.WriteLine("✅ Таблица GroupChatMembers готова к работе");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка инициализации таблицы участников: {ex.Message}");
            }
        }

        public async Task<bool> DebugStudentChatAccess(int studentId)
        {
            try
            {
                Console.WriteLine($"🔧 ДИАГНОСТИКА: проверяем доступ студента {studentId} к чатам");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. Проверяем группы студента
                var groupsQuery = @"
            SELECT sg.GroupId, sg.GroupName, c.CourseName
            FROM GroupEnrollments ge
            INNER JOIN StudyGroups sg ON ge.GroupId = sg.GroupId
            INNER JOIN Courses c ON sg.CourseId = c.CourseId
            WHERE ge.StudentId = @StudentId 
                AND ge.Status = 'active'
                AND sg.IsActive = 1";

                using var groupsCommand = new SqlCommand(groupsQuery, connection);
                groupsCommand.Parameters.AddWithValue("@StudentId", studentId);

                var groups = new List<string>();
                using (var reader = await groupsCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        groups.Add($"{reader.GetString("GroupName")} (ID: {reader.GetInt32("GroupId")}, Курс: {reader.GetString("CourseName")})");
                    }
                }

                Console.WriteLine($"👥 Студент состоит в {groups.Count} группах:");
                foreach (var group in groups)
                {
                    Console.WriteLine($"   - {group}");
                }

                // 2. Проверяем сообщения в чатах
                foreach (var group in groups)
                {
                    var groupId = int.Parse(group.Split("ID: ")[1].Split(")")[0]);

                    var messagesQuery = @"
                SELECT COUNT(*) as MessageCount
                FROM GroupChatMessages 
                WHERE GroupId = @GroupId";

                    using var messagesCommand = new SqlCommand(messagesQuery, connection);
                    messagesCommand.Parameters.AddWithValue("@GroupId", groupId);

                    var messageCount = Convert.ToInt32(await messagesCommand.ExecuteScalarAsync());
                    Console.WriteLine($"💬 В группе {groupId} сообщений: {messageCount}");
                }

                return groups.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка диагностики: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSupportMessageAsync(int userId, string message)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO SupportChats (UserId, SenderId, MessageText, SentAt, IsRead)
            VALUES (@UserId, @UserId, @MessageText, GETDATE(), 1)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@MessageText", message);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения в поддержку: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ChatMessage>> GetSupportChatMessagesAsync(int userId)
        {
            var messages = new List<ChatMessage>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            WITH EquippedEmoji AS (
                SELECT ui.UserId, MAX(si.Icon) AS EmojiIcon
                FROM UserInventory ui
                JOIN ShopItems si ON si.ItemId = ui.ItemId AND si.ItemType = 'emoji'
                WHERE ui.IsEquipped = 1
                GROUP BY ui.UserId
            )
            SELECT MessageId, UserId, SenderId, MessageText, SentAt, IsRead,
                   CASE 
                       WHEN SenderId = @UserId THEN u.FirstName + ' ' + u.LastName
                       ELSE 'Поддержка'
                   END as SenderName,
                   ISNULL(u.AvatarUrl, 'default_avatar.png') as SenderAvatar,
                   ee.EmojiIcon
            FROM SupportChats sc
            LEFT JOIN Users u ON sc.SenderId = u.UserId
            LEFT JOIN EquippedEmoji ee ON ee.UserId = sc.SenderId
            WHERE sc.UserId = @UserId
            ORDER BY SentAt ASC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                // Получаем московский часовой пояс
                var moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                if (moscowTimeZone == null)
                {
                    try
                    {
                        moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                    }
                    catch
                    {
                        moscowTimeZone = TimeZoneInfo.Utc;
                    }
                }

                while (await reader.ReadAsync())
                {
                    var sentAt = reader.GetDateTime("SentAt");
                    // Конвертируем в московское время
                    if (sentAt.Kind == DateTimeKind.Unspecified)
                    {
                        sentAt = DateTime.SpecifyKind(sentAt, DateTimeKind.Utc);
                    }
                    
                    if (sentAt.Kind == DateTimeKind.Utc)
                    {
                        sentAt = TimeZoneInfo.ConvertTimeFromUtc(sentAt, moscowTimeZone);
                    }

                    messages.Add(new ChatMessage
                    {
                        MessageId = reader.GetInt32("MessageId"),
                        SenderId = reader.GetInt32("SenderId"),
                        MessageText = reader.GetString("MessageText"),
                        SentAt = sentAt,
                        IsRead = reader.GetBoolean("IsRead"),
                        SenderName = reader.GetString("SenderName"),
                        SenderAvatar = reader.GetString("SenderAvatar"),
                        UserEmoji = reader.IsDBNull("EmojiIcon") ? null : reader.GetString("EmojiIcon")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки сообщений поддержки: {ex.Message}");
            }
            return messages;
        }

        public async Task<List<User>> GetAvailableStudentsForGroupAsync(int courseId)
        {
            var students = new List<User>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT DISTINCT u.UserId, u.Username, u.FirstName, u.LastName, u.Email
            FROM Users u
            WHERE u.RoleId = 1 -- студенты
            AND NOT EXISTS (
                SELECT 1 FROM GroupEnrollments ge 
                INNER JOIN StudyGroups sg ON ge.GroupId = sg.GroupId 
                WHERE ge.StudentId = u.UserId AND sg.CourseId = @CourseId
            )";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    students.Add(new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        FirstName = reader.GetString("FirstName"),
                        LastName = reader.GetString("LastName"),
                        Email = reader.GetString("Email")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения студентов: {ex.Message}");
            }
            return students;
        }

        // Метод для добавления студентов в групповой чат
        public async Task<bool> AddStudentsToGroupChatAsync(int groupId, List<User> students)
        {
            try
            {
                Console.WriteLine($"🔧 Начинаем добавление {students.Count} студентов в чат группы {groupId}");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                int addedToChatCount = 0;

                foreach (var student in students)
                {
                    Console.WriteLine($"👤 Обрабатываем студента: {student.Username} (ID: {student.UserId})");

                    // Простая проверка - студент в группе?
                    var checkEnrollmentQuery = @"
                SELECT COUNT(*) 
                FROM GroupEnrollments 
                WHERE GroupId = @GroupId 
                AND StudentId = @StudentId 
                AND Status = 'active'";

                    using var checkEnrollmentCommand = new SqlCommand(checkEnrollmentQuery, connection);
                    checkEnrollmentCommand.Parameters.AddWithValue("@GroupId", groupId);
                    checkEnrollmentCommand.Parameters.AddWithValue("@StudentId", student.UserId);

                    var isEnrolled = Convert.ToInt32(await checkEnrollmentCommand.ExecuteScalarAsync()) > 0;

                    if (!isEnrolled)
                    {
                        Console.WriteLine($"⚠️ Студент {student.Username} не записан в группу {groupId}, пропускаем");
                        continue;
                    }

                    // Добавляем в участники чата
                    var addToChatQuery = @"
                IF NOT EXISTS (SELECT 1 FROM GroupChatMembers WHERE GroupId = @GroupId AND UserId = @UserId)
                BEGIN
                    INSERT INTO GroupChatMembers (GroupId, UserId) 
                    VALUES (@GroupId, @UserId)
                END";

                    using var addToChatCommand = new SqlCommand(addToChatQuery, connection);
                    addToChatCommand.Parameters.AddWithValue("@GroupId", groupId);
                    addToChatCommand.Parameters.AddWithValue("@UserId", student.UserId);

                    var result = await addToChatCommand.ExecuteNonQueryAsync();

                    if (result > 0)
                    {
                        addedToChatCount++;
                        Console.WriteLine($"✅ Студент {student.Username} добавлен в участники чата");
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️ Студент {student.Username} уже в участниках чата");
                    }
                }

                // Отправляем системное сообщение
                if (addedToChatCount > 0)
                {
                    var welcomeMessage = $"🎉 В группу добавлено {students.Count} новых студентов! Добро пожаловать в общий чат!";

                    var messageQuery = @"
                INSERT INTO GroupChatMessages (GroupId, SenderId, MessageText, SentAt, IsRead, IsSystemMessage)
                VALUES (@GroupId, 0, @MessageText, GETDATE(), 0, 1)";

                    using var messageCommand = new SqlCommand(messageQuery, connection);
                    messageCommand.Parameters.AddWithValue("@GroupId", groupId);
                    messageCommand.Parameters.AddWithValue("@MessageText", welcomeMessage);

                    await messageCommand.ExecuteNonQueryAsync();
                    Console.WriteLine($"📢 Отправлено приветственное сообщение: {welcomeMessage}");
                }

                Console.WriteLine($"✅ Успешно добавлено {addedToChatCount} студентов в чат группы {groupId}");
                return addedToChatCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка добавления в чат: {ex.Message}");
                return false;
            }
        }


        public async Task<bool> AddChatMembersAsync(int groupId, List<User> students)
        {
            try
            {
                Console.WriteLine($"🔧 Добавляем {students.Count} участников в чат группы {groupId}");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();

                try
                {
                    int addedCount = 0;

                    foreach (var student in students)
                    {
                        Console.WriteLine($"👤 Добавляем студента {student.Username} в участники чата");

                        // Проверяем, не добавлен ли уже
                        var checkQuery = @"
                    SELECT COUNT(*) 
                    FROM GroupChatMembers 
                    WHERE GroupId = @GroupId AND UserId = @UserId";

                        using var checkCommand = new SqlCommand(checkQuery, connection, transaction);
                        checkCommand.Parameters.AddWithValue("@GroupId", groupId);
                        checkCommand.Parameters.AddWithValue("@UserId", student.UserId);

                        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                        if (!exists)
                        {
                            var insertQuery = @"
                        INSERT INTO GroupChatMembers (GroupId, UserId)
                        VALUES (@GroupId, @UserId)";

                            using var insertCommand = new SqlCommand(insertQuery, connection, transaction);
                            insertCommand.Parameters.AddWithValue("@GroupId", groupId);
                            insertCommand.Parameters.AddWithValue("@UserId", student.UserId);

                            var result = await insertCommand.ExecuteNonQueryAsync();

                            if (result > 0)
                            {
                                addedCount++;
                                Console.WriteLine($"✅ Студент {student.Username} добавлен в участники чата");
                            }
                            else
                            {
                                Console.WriteLine($"❌ Не удалось добавить студента {student.Username} в участники чата");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"ℹ️ Студент {student.Username} уже в участниках чата");
                        }
                    }

                    transaction.Commit();
                    Console.WriteLine($"✅ Успешно добавлено {addedCount} участников в чат");
                    return addedCount > 0;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"❌ Ошибка при добавлении участников: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка: {ex.Message}");
                return false;
            }
        }

        // Метод для проверки доступа к чату
        public async Task<bool> HasChatAccessAsync(int userId, int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT COUNT(*) 
            FROM GroupChatMembers 
            WHERE GroupId = @GroupId AND UserId = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@UserId", userId);

                var hasAccess = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;

                Console.WriteLine($"🔍 Проверка доступа: пользователь {userId} к чату {groupId} = {hasAccess}");
                return hasAccess;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка проверки доступа: {ex.Message}");
                return false;
            }
        }


        public async Task<bool> CheckStudentGroupMembership(int studentId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT COUNT(*) 
            FROM GroupEnrollments ge
            JOIN StudyGroups sg ON ge.GroupId = sg.GroupId
            JOIN Courses c ON sg.CourseId = c.CourseId
            WHERE ge.StudentId = @StudentId 
            AND ge.Status = 'active'
            AND sg.IsActive = 1
            AND c.IsPublished = 1";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentId", studentId);

                var result = await command.ExecuteScalarAsync();
                var count = Convert.ToInt32(result);

                Console.WriteLine($"🔍 Студент {studentId} состоит в {count} активных группах");
                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка проверки членства: {ex.Message}");
                return false;
            }
        }


        // В DatabaseService добавьте этот метод
        public async Task CheckAndCreateMissingTables()
        {
            try
            {
                Console.WriteLine("🔍 Проверяем наличие необходимых таблиц...");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Проверяем основные таблицы для чатов
                var tablesToCheck = new[]
                {
            "GroupChats",
            "PrivateChats",
            "SupportChats",
            "StudyGroups",
            "GroupEnrollments",
            "Courses",
            "Users"
        };

                foreach (var table in tablesToCheck)
                {
                    var checkQuery = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{table}'";
                    using var checkCommand = new SqlCommand(checkQuery, connection);
                    var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                    Console.WriteLine($"{(exists ? "✅" : "❌")} Таблица {table}: {(exists ? "существует" : "ОТСУТСТВУЕТ")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка проверки таблиц: {ex.Message}");
            }
        }

        public async Task CreateMissingChatTables()
        {
            try
            {
                // Сначала создаем таблицу участников чата
                await InitializeChatMembersTable();
              
                Console.WriteLine("🛠️ Создаем недостающие таблицы для чатов...");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Таблица для групповых чатов
                var createGroupChatsTable = @"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GroupChatMessages')
    BEGIN
        CREATE TABLE GroupChatMessages (
            MessageId INT PRIMARY KEY IDENTITY(1,1),
            GroupId INT NOT NULL FOREIGN KEY REFERENCES StudyGroups(GroupId),
            SenderId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
            MessageText NVARCHAR(MAX) NOT NULL,
            SentAt DATETIME2 DEFAULT GETDATE(),
            IsRead BIT DEFAULT 0,
            IsSystemMessage BIT DEFAULT 0
        );
        CREATE INDEX IX_GroupChatMessages_GroupId ON GroupChatMessages(GroupId);
        CREATE INDEX IX_GroupChatMessages_SentAt ON GroupChatMessages(SentAt);
        PRINT '✅ Таблица GroupChatMessages создана';
    END
    ELSE
    BEGIN
        PRINT '✅ Таблица GroupChatMessages уже существует';
    END";
                // Таблица для приватных чатов
                var createPrivateChatsTable = @"
        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PrivateChats')
        BEGIN
            CREATE TABLE PrivateChats (
                MessageId INT PRIMARY KEY IDENTITY(1,1),
                SenderId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
                ReceiverId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
                MessageText NVARCHAR(MAX) NOT NULL,
                SentAt DATETIME2 DEFAULT GETDATE(),
                IsRead BIT DEFAULT 0
            );
            PRINT '✅ Таблица PrivateChats создана';
        END";

                // Таблица для чатов поддержки
                var createSupportChatsTable = @"
        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SupportChats')
        BEGIN
            CREATE TABLE SupportChats (
                MessageId INT PRIMARY KEY IDENTITY(1,1),
                UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
                SenderId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
                MessageText NVARCHAR(MAX) NOT NULL,
                SentAt DATETIME2 DEFAULT GETDATE(),
                IsRead BIT DEFAULT 0
            );
            PRINT '✅ Таблица SupportChats создана';
        END";

                using var transaction = connection.BeginTransaction();

                try
                {
                    using var cmd1 = new SqlCommand(createGroupChatsTable, connection, transaction);
                    await cmd1.ExecuteNonQueryAsync();

                    using var cmd2 = new SqlCommand(createPrivateChatsTable, connection, transaction);
                    await cmd2.ExecuteNonQueryAsync();

                    using var cmd3 = new SqlCommand(createSupportChatsTable, connection, transaction);
                    await cmd3.ExecuteNonQueryAsync();

                    transaction.Commit();
                    Console.WriteLine("✅ Все таблицы чатов успешно созданы/проверены");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка создания таблиц: {ex.Message}");
            }
        }


        public async Task ForceRefreshStudentChats(int studentId)
        {
            try
            {
                // Очищаем кэш или принудительно обновляем данные
                await GetStudentAllChatsAsync(studentId);
                Console.WriteLine($"✅ Чаты студента {studentId} принудительно обновлены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка принудительного обновления чатов: {ex.Message}");
            }
        }

        public async Task CheckChatTableStructure()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Console.WriteLine("🔍 Проверяем структуру таблиц чатов...");

                // Проверяем существование таблицы GroupChats
                var checkTableQuery = @"
            SELECT 
                TABLE_NAME,
                COLUMN_NAME,
                DATA_TYPE,
                IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME IN ('GroupChats', 'StudyGroups', 'GroupEnrollments')
            ORDER BY TABLE_NAME, ORDINAL_POSITION";

                using var command = new SqlCommand(checkTableQuery, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"📋 {reader.GetString("TABLE_NAME")}.{reader.GetString("COLUMN_NAME")} ({reader.GetString("DATA_TYPE")})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка проверки структуры: {ex.Message}");
            }
        }

        public async Task<bool> CheckStudentChatAccess(int studentId, int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT COUNT(*) 
            FROM GroupEnrollments ge
            INNER JOIN StudyGroups sg ON ge.GroupId = sg.GroupId
            WHERE ge.StudentId = @StudentId 
                AND ge.GroupId = @GroupId
                AND ge.Status = 'active'
                AND sg.IsActive = 1";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentId", studentId);
                command.Parameters.AddWithValue("@GroupId", groupId);

                var hasAccess = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;

                Console.WriteLine($"🔍 Студент {studentId} имеет доступ к чату {groupId}: {hasAccess}");
                return hasAccess;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка проверки доступа: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsUserInGroupChatAsync(int userId, int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT COUNT(*) 
            FROM GroupChatMembers 
            WHERE GroupId = @GroupId AND UserId = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки участника чата: {ex.Message}");
                return false;
            }
        }

        // Добавление участника в чат
        public async Task<bool> AddUserToGroupChatAsync(int userId, int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Сначала проверяем, не добавлен ли уже
                if (await IsUserInGroupChatAsync(userId, groupId))
                {
                    Console.WriteLine($"ℹ️ Пользователь {userId} уже в чате группы {groupId}");
                    return true;
                }

                var query = @"
            INSERT INTO GroupChatMembers (GroupId, UserId)
            VALUES (@GroupId, @UserId)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteNonQueryAsync();

                if (result > 0)
                {
                    Console.WriteLine($"✅ Пользователь {userId} добавлен в чат группы {groupId}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка добавления в чат: {ex.Message}");
                return false;
            }
        }


        // Проверка состоит ли студент в группе
        public async Task<bool> IsStudentInGroupAsync(int studentId, int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT COUNT(*) 
            FROM GroupEnrollments 
            WHERE StudentId = @StudentId 
            AND GroupId = @GroupId 
            AND Status = 'active'";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentId", studentId);
                command.Parameters.AddWithValue("@GroupId", groupId);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки членства студента: {ex.Message}");
                return false;
            }
        }

        // Простое добавление в группу
        public async Task<bool> EnrollStudentToGroupAsync(int groupId, int studentId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            IF NOT EXISTS (SELECT 1 FROM GroupEnrollments WHERE GroupId=@GroupId AND StudentId=@StudentId AND Status='active')
            BEGIN
                INSERT INTO GroupEnrollments (GroupId, StudentId, EnrollmentDate, Status) 
                VALUES (@GroupId, @StudentId, GETDATE(), 'active')
            END";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@StudentId", studentId);

                var result = await command.ExecuteNonQueryAsync();
                Console.WriteLine($"📝 Добавление в группу: {result} строк изменено");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка добавления студента в группу: {ex.Message}");
                return false;
            }
        }

        // Простое добавление в чат
        public async Task<bool> SimpleAddToGroupChat(int groupId, int userId)
        {
            try
            {
                Console.WriteLine($"🔧 Добавляем пользователя {userId} в чат группы {groupId}");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Сначала убедимся, что таблица существует
                await InitializeChatMembersTable();

                var query = @"
            IF NOT EXISTS (SELECT 1 FROM GroupChatMembers WHERE GroupId = @GroupId AND UserId = @UserId)
            BEGIN
                INSERT INTO GroupChatMembers (GroupId, UserId) 
                VALUES (@GroupId, @UserId)
            END";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteNonQueryAsync();
                Console.WriteLine($"💬 Добавление в чат: {result} строк изменено");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Ошибка добавления в чат: {ex.Message}");
                return false;
            }
        }
        public async Task<List<GroupChatMember>> GetGroupChatMembersAsync(int groupId)
        {
            var members = new List<GroupChatMember>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT gcm.GroupId, gcm.UserId, gcm.JoinedDate,
                   u.Username, u.FirstName, u.LastName, u.AvatarUrl
            FROM GroupChatMembers gcm
            INNER JOIN Users u ON gcm.UserId = u.UserId
            WHERE gcm.GroupId = @GroupId
            ORDER BY gcm.JoinedDate";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    members.Add(new GroupChatMember
                    {
                        GroupId = reader.GetInt32("GroupId"),
                        UserId = reader.GetInt32("UserId"),
                        JoinedDate = reader.GetDateTime("JoinedDate"),
                        User = new User
                        {
                            UserId = reader.GetInt32("UserId"),
                            Username = reader.GetString("Username"),
                            FirstName = reader.IsDBNull("FirstName") ? null : reader.GetString("FirstName"),
                            LastName = reader.IsDBNull("LastName") ? null : reader.GetString("LastName"),
                            AvatarUrl = reader.IsDBNull("AvatarUrl") ? null : reader.GetString("AvatarUrl")
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки участников чата: {ex.Message}");
            }
            return members;
        } 

    }

}
