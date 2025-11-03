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
                // Сначала проверяем локальные настройки
                bool hasLocalConsent = Preferences.Get($"PrivacyConsent_{userId}", false);
                if (hasLocalConsent)
                {
                    return true;
                }

                // Затем проверяем базу данных
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT COUNT(*) 
            FROM PrivacyConsent 
            WHERE UserId = @UserId AND IsActive = 1";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking privacy consent: {ex.Message}");

                // Если ошибка БД, проверяем только настройки
                return Preferences.Get($"PrivacyConsent_{userId}", false);
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
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        (SELECT COUNT(*) FROM StudentProgress WHERE StudentId = @UserId) as TotalCourses,
                        (SELECT COUNT(*) FROM StudentProgress WHERE StudentId = @UserId AND Status = 'completed') as CompletedCourses,
                        (SELECT ISNULL(AVG(Score), 0) FROM TestAttempts WHERE StudentId = @UserId AND Status = 'completed') as AverageScore,
                        (SELECT StreakDays FROM Users WHERE UserId = @UserId) as CurrentStreak,
                        (SELECT DATEDIFF(day, RegistrationDate, GETDATE()) FROM Users WHERE UserId = @UserId) as TotalDays";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var totalCourses = reader.GetInt32("TotalCourses");
                    var completedCourses = reader.GetInt32("CompletedCourses");

                    return new UserStatistics
                    {
                        TotalCourses = totalCourses,
                        CompletedCourses = completedCourses,
                        TotalTimeSpent = totalCourses * 2,
                        AverageScore = reader.GetDouble("AverageScore"),
                        CompletionRate = totalCourses > 0 ? (double)completedCourses / totalCourses : 0,
                        CurrentStreak = reader.GetInt32("CurrentStreak"),
                        LongestStreak = reader.GetInt32("CurrentStreak"),
                        TotalDays = reader.GetInt32("TotalDays")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки статистики: {ex.Message}");
            }

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
            try
            {
                // Сохраняем файл локально
                var filePath = await SaveAvatarAsync(imageStream, fileName, userId);

                if (string.IsNullOrEmpty(filePath))
                    return null;

                // Обновляем путь в базе данных
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "UPDATE Users SET AvatarUrl = @AvatarUrl WHERE UserId = @UserId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AvatarUrl", filePath);
                command.Parameters.AddWithValue("@UserId", userId);

                await command.ExecuteNonQueryAsync();

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки аватара: {ex.Message}");
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
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT AvatarUrl FROM Users WHERE UserId = @UserId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                var avatarPath = result?.ToString();

                // Проверяем, существует ли файл
                if (!string.IsNullOrEmpty(avatarPath) && File.Exists(avatarPath))
                {
                    return avatarPath;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения аватара: {ex.Message}");
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

                // Получаем вчерашнюю дату и текущую серию
                var getQuery = @"SELECT StreakDays, LastLoginDate FROM Users WHERE UserId = @UserId";
                using var getCmd = new SqlCommand(getQuery, connection);
                getCmd.Parameters.AddWithValue("@UserId", userId);
                int currentStreak = 0;
                DateTime? lastLogin = null;
                using (var r = await getCmd.ExecuteReaderAsync())
                {
                    if (await r.ReadAsync())
                    {
                        currentStreak = r.IsDBNull(r.GetOrdinal("StreakDays")) ? 0 : r.GetInt32(r.GetOrdinal("StreakDays"));
                        lastLogin = r.IsDBNull(r.GetOrdinal("LastLoginDate")) ? null : r.GetDateTime(r.GetOrdinal("LastLoginDate"));
                    }
                }

                bool increment = lastLogin.HasValue && lastLogin.Value.Date == DateTime.UtcNow.Date.AddDays(-1).Date;

                // Обновляем серию
                var updateQuery = @"
                    UPDATE Users 
                    SET StreakDays = @NewStreak,
                        LastLoginDate = CAST(GETDATE() AS DATE)
                    WHERE UserId = @UserId";
                using var updCmd = new SqlCommand(updateQuery, connection);
                updCmd.Parameters.AddWithValue("@NewStreak", increment ? currentStreak + 1 : 1);
                updCmd.Parameters.AddWithValue("@UserId", userId);
                await updCmd.ExecuteNonQueryAsync();

                // Начисляем монеты только при увеличении серии
                if (increment)
                {
                    int reward = 10;
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

        public async Task<List<StudyGroup>> GetTeacherStudyGroupsAsync(int teacherId)
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
                    WHERE sg.TeacherId = @TeacherId
                    GROUP BY sg.GroupId, sg.GroupName, sg.StartDate, sg.EndDate, sg.IsActive";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@TeacherId", teacherId);

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

        public async Task<List<LessonDto>> GetCourseLessonsAsync(int courseId)
        {
            var lessons = new List<LessonDto>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT l.LessonId, l.ModuleId, l.LessonType, l.Title, l.Content, l.LessonOrder
                    FROM Lessons l
                    JOIN CourseModules m ON l.ModuleId = m.ModuleId
                    WHERE m.CourseId = @CourseId AND l.IsActive = 1
                    ORDER BY m.ModuleOrder, l.LessonOrder";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    lessons.Add(new LessonDto
                    {
                        LessonId = reader.GetInt32("LessonId"),
                        ModuleId = reader.GetInt32("ModuleId"),
                        LessonType = reader.GetString("LessonType"),
                        Title = reader.GetString("Title"),
                        Content = reader.IsDBNull("Content") ? null : reader.GetString("Content"),
                        LessonOrder = reader.GetInt32("LessonOrder")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки уроков: {ex.Message}");
            }
            return lessons;
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

        // ДОБАВЛЕНИЕ КОНТЕНТА КУРСА
        public async Task<int?> AddTheoryLessonAsync(int courseId, string title, string? htmlContent, int order = 1)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    INSERT INTO Lessons (ModuleId, LessonType, Title, Content, LessonOrder, IsActive)
                    VALUES ((SELECT TOP 1 ModuleId FROM CourseModules WHERE CourseId = @CourseId ORDER BY ModuleOrder),
                            'theory', @Title, @Content, @Order, 1);
                    SELECT SCOPE_IDENTITY();";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
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

        public async Task<int?> AddPracticeLessonAsync(int courseId, string title, string? starterCode, string? expectedOutput, string? testCasesJson, string? hint, int order = 1)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var lessonQuery = @"
                    INSERT INTO Lessons (ModuleId, LessonType, Title, LessonOrder, IsActive)
                    VALUES ((SELECT TOP 1 ModuleId FROM CourseModules WHERE CourseId = @CourseId ORDER BY ModuleOrder),
                            'practice', @Title, @Order, 1);
                    SELECT SCOPE_IDENTITY();";

                using var lcmd = new SqlCommand(lessonQuery, connection);
                lcmd.Parameters.AddWithValue("@CourseId", courseId);
                lcmd.Parameters.AddWithValue("@Title", title ?? "");
                lcmd.Parameters.AddWithValue("@Order", order);
                var lessonIdObj = await lcmd.ExecuteScalarAsync();
                if (lessonIdObj == null) return null;
                int lessonId = Convert.ToInt32(lessonIdObj);

                var practiceQuery = @"
                    INSERT INTO PracticeExercises (LessonId, StarterCode, ExpectedOutput, TestCases, Hint)
                    VALUES (@LessonId, @StarterCode, @ExpectedOutput, @TestCases, @Hint)";

                using var pcmd = new SqlCommand(practiceQuery, connection);
                pcmd.Parameters.AddWithValue("@LessonId", lessonId);
                pcmd.Parameters.AddWithValue("@StarterCode", (object?)starterCode ?? DBNull.Value);
                pcmd.Parameters.AddWithValue("@ExpectedOutput", (object?)expectedOutput ?? DBNull.Value);
                pcmd.Parameters.AddWithValue("@TestCases", (object?)testCasesJson ?? DBNull.Value);
                pcmd.Parameters.AddWithValue("@Hint", (object?)hint ?? DBNull.Value);
                await pcmd.ExecuteNonQueryAsync();

                return lessonId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления практики: {ex.Message}");
                return null;
            }
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
                    INSERT INTO StudyGroups (GroupName, CourseId, StartDate, EndDate, IsActive, CreatedByUserId)
                    VALUES (@GroupName, @CourseId, @StartDate, @EndDate, 1, @TeacherId)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupName", groupName ?? "");
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);
                command.Parameters.AddWithValue("@TeacherId", teacherId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания группы: {ex.Message}");
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
                        Student = new User { FirstName = reader.GetString("StudentName") }
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

        public async Task<bool> EnrollStudentToGroupAsync(int groupId, int studentId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"
            IF NOT EXISTS (SELECT 1 FROM GroupEnrollments WHERE GroupId=@GroupId AND StudentId=@StudentId)
                INSERT INTO GroupEnrollments (GroupId, StudentId, EnrollmentDate, Status) VALUES (@GroupId, @StudentId, GETDATE(), 'active')";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@StudentId", studentId);
                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления студента в группу: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveStudentFromGroupAsync(int groupId, int studentId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"UPDATE GroupEnrollments SET Status='dropped' WHERE GroupId=@GroupId AND StudentId=@StudentId";
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

        // ПРАКТИКА
        public class PracticeDto
        {
            public int LessonId { get; set; }
            public string? StarterCode { get; set; }
            public string? ExpectedOutput { get; set; }
            public string? TestCasesJson { get; set; }
            public string? Hint { get; set; }
        }

        public async Task<PracticeDto?> GetPracticeExerciseAsync(int lessonId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"SELECT LessonId, StarterCode, ExpectedOutput, TestCases, Hint FROM PracticeExercises WHERE LessonId=@LessonId";
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
                        Hint = reader.IsDBNull("Hint") ? null : reader.GetString("Hint")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки практики: {ex.Message}");
            }
            return null;
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

        // ТЕСТЫ: мета и попытки
        public class TestMeta
        {
            public int TestId { get; set; }
            public string Title { get; set; } = string.Empty;
            public int PassingScore { get; set; }
            public int MaxScore { get; set; }
        }

        public async Task<TestMeta?> GetTestMetaByLessonAsync(int lessonId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"SELECT TOP 1 TestId, Title, PassingScore, MaxScore FROM Tests WHERE LessonId=@LessonId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@LessonId", lessonId);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    return new TestMeta
                    {
                        TestId = r.GetInt32("TestId"),
                        Title = r.GetString("Title"),
                        PassingScore = r.GetInt32("PassingScore"),
                        MaxScore = r.IsDBNull("MaxScore") ? 100 : r.GetInt32("MaxScore")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения мета теста: {ex.Message}");
            }
            return null;
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
                var query = @"SELECT sg.GroupId, sg.GroupName, c.CourseName, COUNT(sgm.StudentId) as StudentCount
                      FROM StudyGroups sg
                      LEFT JOIN Courses c ON sg.CourseId = c.CourseId
                      LEFT JOIN StudentGroupMemberships sgm ON sg.GroupId = sgm.GroupId
                      WHERE sgm.StudentId = @StudentId AND sg.IsActive = 1
                      GROUP BY sg.GroupId, sg.GroupName, c.CourseName";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@StudentId", studentId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    groups.Add(new StudentGroupChat
                    {
                        GroupId = r.GetInt32("GroupId"),
                        GroupName = r.GetString("GroupName"),
                        CourseName = r.IsDBNull("CourseName") ? "Без курса" : r.GetString("CourseName"),
                        StudentCount = r.GetInt32("StudentCount")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения групп студента: {ex.Message}");
            }
            return groups;
        }

        public class StudentGroupChat
        {
            public int GroupId { get; set; }
            public string GroupName { get; set; } = string.Empty;
            public string CourseName { get; set; } = string.Empty;
            public int StudentCount { get; set; }
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
        public async Task<bool> GradeContestSubmissionAsync(int submissionId, int teacherId, int score, string feedback)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = @"UPDATE ContestSubmissions 
                      SET TeacherScore = @Score, TeacherFeedback = @Feedback, 
                          GradedBy = @TeacherId, GradedAt = GETDATE()
                      WHERE SubmissionId = @SubmissionId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@SubmissionId", submissionId);
                cmd.Parameters.AddWithValue("@Score", score);
                cmd.Parameters.AddWithValue("@Feedback", feedback);
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
            SELECT sg.GroupId, sg.GroupName, sg.StartDate, sg.EndDate, sg.IsActive,
                   c.CourseName, u.FirstName + ' ' + u.LastName as TeacherName
            FROM StudyGroups sg
            JOIN Courses c ON sg.CourseId = c.CourseId
            JOIN Users u ON sg.TeacherId = u.UserId
            JOIN GroupEnrollments ge ON sg.GroupId = ge.GroupId
            WHERE ge.StudentId = @UserId AND ge.Status = 'active'
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
                        IsActive = reader.GetBoolean("IsActive")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки групп пользователя: {ex.Message}");
            }
            return groups;
        }

        public async Task<List<GroupChatMessage>> GetGroupChatMessagesAsync(int groupId, int count = 50)
        {
            var messages = new List<GroupChatMessage>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT TOP (@Count) m.MessageId, m.GroupId, m.SenderId, m.MessageText, m.SentDate,
                   u.FirstName + ' ' + u.LastName as SenderName
            FROM GroupChats m
            JOIN Users u ON m.SenderId = u.UserId
            WHERE m.GroupId = @GroupId
            ORDER BY m.SentDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@Count", count);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    messages.Add(new GroupChatMessage
                    {
                        MessageId = reader.GetInt32("MessageId"),
                        GroupId = reader.GetInt32("GroupId"),
                        SenderId = reader.GetInt32("SenderId"),
                        MessageText = reader.GetString("MessageText"),
                        SentDate = reader.GetDateTime("SentDate"),
                        SenderName = reader.GetString("SenderName")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки сообщений чата: {ex.Message}");
            }
            return messages;
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

        public async Task<bool> SendGroupChatMessageAsync(int groupId, int senderId, string messageText)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO GroupChats (GroupId, SenderId, MessageText, SentDate)
            VALUES (@GroupId, @SenderId, @MessageText, GETDATE())";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);
                command.Parameters.AddWithValue("@SenderId", senderId);
                command.Parameters.AddWithValue("@MessageText", messageText);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
                return false;
            }
        }
    }
}