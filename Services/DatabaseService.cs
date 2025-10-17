using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Data;
using EducationalPlatform.Models;

namespace EducationalPlatform.Services
{
    public class DatabaseService
    {
        private string _connectionString;

        public DatabaseService()
        {
            _connectionString = "Server=EducationalPlatform.mssql.somee.com;Database=EducationalPlatform;User Id=yyullechkaaa_SQLLogin_1;Password=xtbnfhvyqu;TrustServerCertificate=true;";
        }

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
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Не удалось подключиться: {ex.Message}", "OK");
                return false;
            }
        }

        public async Task<User> LoginAsync(string username, string password)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT UserId, Username, Email, FirstName, LastName, RoleId, 
                           LanguagePref, GameCurrency, StreakDays, RegistrationDate, IsActive
                    FROM Users 
                    WHERE Username = @Username AND PasswordHash = @PasswordHash AND IsActive = 1";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@PasswordHash", HashPassword(password));

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        Email = reader.GetString("Email"),
                        FirstName = reader.GetString("FirstName"),
                        LastName = reader.GetString("LastName"),
                        RoleId = reader.GetInt32("RoleId"),
                        LanguagePref = reader.GetString("LanguagePref"),
                        GameCurrency = reader.GetInt32("GameCurrency"),
                        StreakDays = reader.GetInt32("StreakDays"),
                        RegistrationDate = reader.GetDateTime("RegistrationDate"),
                        IsActive = reader.GetBoolean("IsActive")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Ошибка входа: {ex.Message}", "OK");
                return null;
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
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Email", email);

                var count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Ошибка проверки пользователя: {ex.Message}", "OK");
                return true;
            }
        }


        public async Task<bool> RegisterUserAsync(string username, string email, string password,
                                        string firstName, string lastName, int roleId,
                                        string languagePref, string interfaceStyle)
        {
            try
            {
                // ПРОВЕРЯЕМ УНИКАЛЬНОСТЬ
                if (await CheckUserExistsAsync(username, email))
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Пользователь с таким логином или email уже существует", "OK");
                    return false;
                }

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, 
                             RoleId, LanguagePref, InterfaceStyle, RegistrationDate)
            VALUES (@Username, @Email, @PasswordHash, @FirstName, @LastName, 
                    @RoleId, @LanguagePref, @InterfaceStyle, GETDATE())";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
                command.Parameters.AddWithValue("@FirstName", firstName);
                command.Parameters.AddWithValue("@LastName", lastName);
                command.Parameters.AddWithValue("@RoleId", roleId);
                command.Parameters.AddWithValue("@LanguagePref", languagePref);
                command.Parameters.AddWithValue("@InterfaceStyle", interfaceStyle);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Ошибка регистрации: {ex.Message}", "OK");
                return false;
            }
        }

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
                        CourseName = reader.GetString("CourseName"),
                        Description = reader.GetString("Description"),
                        LanguageName = reader.GetString("LanguageName"),
                        DifficultyName = reader.GetString("DifficultyName"),
                        IsPublished = reader.GetBoolean("IsPublished")
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Ошибка загрузки курсов: {ex.Message}", "OK");
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
                        UPDATE StudentProgress 
                        SET Status = @Status, 
                            CompletionDate = CASE WHEN @Status = 'completed' THEN GETDATE() ELSE NULL END
                        WHERE StudentId = @UserId AND CourseId = @CourseId
                    ELSE
                        INSERT INTO StudentProgress (StudentId, CourseId, Status, StartDate)
                        VALUES (@UserId, @CourseId, @Status, GETDATE())";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@Status", status);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Ошибка сохранения прогресса: {ex.Message}", "OK");
                return false;
            }
        }


        public async Task<List<Role>> GetRolesAsync()
        {
            var roles = new List<Role>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Сначала проверяем, есть ли роли в базе
                var checkQuery = "SELECT COUNT(*) FROM Roles";
                using var checkCommand = new SqlCommand(checkQuery, connection);
                var roleCount = (int)await checkCommand.ExecuteScalarAsync();

                // Если ролей нет, создаем основные роли
                if (roleCount == 0)
                {
                    await InitializeRolesAsync(connection);
                }

                // Теперь загружаем роли
                var query = "SELECT RoleId, RoleName FROM Roles WHERE RoleName != 'Admin' ORDER BY RoleId";
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    roles.Add(new Role
                    {
                        RoleId = reader.GetInt32("RoleId"),
                        RoleName = reader.GetString("RoleName")
                    });
                }

                // Если все равно нет ролей, создаем временный список
                if (roles.Count == 0)
                {
                    roles = new List<Role>
            {
                new Role { RoleId = 2, RoleName = "Teacher" },
                new Role { RoleId = 3, RoleName = "Student" },
                new Role { RoleId = 4, RoleName = "ContentManager" }
            };
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Ошибка загрузки ролей: {ex.Message}", "OK");

                // Возвращаем временный список в случае ошибки
                roles = new List<Role>
        {
            new Role { RoleId = 2, RoleName = "Teacher" },
            new Role { RoleId = 3, RoleName = "Student" },
            new Role { RoleId = 4, RoleName = "ContentManager" }
        };
            }

            return roles;
        }

        // ИНИЦИАЛИЗАЦИЯ РОЛЕЙ В БАЗЕ
        private async Task InitializeRolesAsync(SqlConnection connection)
        {
            try
            {
                var query = @"
            IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
                INSERT INTO Roles (RoleName) VALUES ('Admin');
            IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Teacher')
                INSERT INTO Roles (RoleName) VALUES ('Teacher');
            IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Student')
                INSERT INTO Roles (RoleName) VALUES ('Student');
            IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'ContentManager')
                INSERT INTO Roles (RoleName) VALUES ('ContentManager');";

                using var command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации ролей: {ex.Message}");
            }
        }


        public async Task UpdateLoginStreakAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    UPDATE Users 
                    SET StreakDays = CASE 
                        WHEN LastLoginDate = CAST(GETDATE() - 1 AS DATE) THEN StreakDays + 1
                        ELSE 1
                    END,
                    LastLoginDate = CAST(GETDATE() AS DATE)
                    WHERE UserId = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Ошибка обновления серии: {ex.Message}", "OK");
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
