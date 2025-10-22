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

        // ПРОВЕРКА ПОДКЛЮЧЕНИЯ
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
                // Используем текущую страницу вместо Application.Current.MainPage
                Console.WriteLine($"Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        // ПРОВЕРКА УНИКАЛЬНОСТИ EMAIL И USERNAME
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

        // ПОЛУЧЕНИЕ СПИСКА РОЛЕЙ
        public async Task<List<Role>> GetRolesAsync()
        {
            var roles = new List<Role>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки ролей: {ex.Message}");
            }

            return roles;
        }

        // РЕГИСТРАЦИЯ ПОЛЬЗОВАТЕЛЯ
        public async Task<bool> RegisterUserAsync(string username, string email, string password, string firstName, string lastName)
        {
            try
            {
                if (await CheckUserExistsAsync(username, email))
                    return false;

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // УПРОЩЕННЫЙ ЗАПРОС - только основные поля
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

        // АВТОРИЗАЦИЯ
        public async Task<User?> LoginAsync(string? username, string? password)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT UserId, Username, Email, FirstName, LastName, RoleId,
                   LanguagePref, GameCurrency, StreakDays, RegistrationDate, 
                   IsActive, AvatarUrl
            FROM Users
            WHERE Username = @Username AND PasswordHash = @PasswordHash AND IsActive = 1";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username ?? "");
                command.Parameters.AddWithValue("@PasswordHash", HashPassword(password ?? ""));

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? null : reader.GetString("Username"),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString("Email"),
                        FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString("FirstName"),
                        LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString("LastName"),
                        AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl")) ? null : reader.GetString("AvatarUrl"), // Добавьте эту строку
                        RoleId = reader.GetInt32("RoleId"),
                        LanguagePref = reader.IsDBNull(reader.GetOrdinal("LanguagePref")) ? "ru" : reader.GetString("LanguagePref"),
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
                Console.WriteLine($"Ошибка входа: {ex.Message}");
                return null;
            }
        }

        // ОБНОВЛЕНИЕ СЕРИИ ВХОДОВ
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
                Console.WriteLine($"Ошибка обновления серии: {ex.Message}");
            }
        }


        // ПОЛУЧЕНИЕ КУРСОВ
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


        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
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

                    // Загружаем группы для курса
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

        // ДЛЯ УЧЕНИКА - прогресс по курсам
        public async Task<List<StudentProgress>> GetStudentProgressAsync(int studentId)
        {
            var progress = new List<StudentProgress>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT c.CourseName, sp.Status, sp.Score, sp.CompletionDate, sp.AttemptsCount
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

        // Добавляем методы для учителя в DatabaseService class
        public async Task<bool> CreateCourseAsync(string courseName, string description, int languageId, int difficultyId, int teacherId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO Courses (CourseName, Description, LanguageId, DifficultyId, CreatedByUserId, IsPublished)
            VALUES (@CourseName, @Description, @LanguageId, @DifficultyId, @TeacherId, 0)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseName", courseName ?? "");
                command.Parameters.AddWithValue("@Description", description ?? "");
                command.Parameters.AddWithValue("@LanguageId", languageId);
                command.Parameters.AddWithValue("@DifficultyId", difficultyId);
                command.Parameters.AddWithValue("@TeacherId", teacherId);

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

        // Добавляем в DatabaseService класс
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

        public async Task<bool> CreateCourseAsync(string courseName, string description, int languageId, int difficultyId, int teacherId, bool isGroupCourse = false)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            INSERT INTO Courses (CourseName, Description, LanguageId, DifficultyId, CreatedByUserId, IsGroupCourse)
            VALUES (@CourseName, @Description, @LanguageId, @DifficultyId, @TeacherId, @IsGroupCourse)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseName", courseName ?? "");
                command.Parameters.AddWithValue("@Description", description ?? "");
                command.Parameters.AddWithValue("@LanguageId", languageId);
                command.Parameters.AddWithValue("@DifficultyId", difficultyId);
                command.Parameters.AddWithValue("@TeacherId", teacherId);
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

        public async Task<bool> CreateTestAsync(int courseId, string title, string description, int timeLimit, int passingScore)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Сначала создаем урок типа "test", затем тест
                var lessonQuery = @"
            INSERT INTO Lessons (ModuleId, LessonType, Title, LessonOrder)
            VALUES ((SELECT TOP 1 ModuleId FROM CourseModules WHERE CourseId = @CourseId), 'test', @Title, 1)";

                using var lessonCommand = new SqlCommand(lessonQuery, connection);
                lessonCommand.Parameters.AddWithValue("@CourseId", courseId);
                lessonCommand.Parameters.AddWithValue("@Title", title);
                await lessonCommand.ExecuteNonQueryAsync();

                // Получаем ID созданного урока
                var getLessonIdQuery = "SELECT SCOPE_IDENTITY()";
                using var idCommand = new SqlCommand(getLessonIdQuery, connection);
                var lessonId = Convert.ToInt32(await idCommand.ExecuteScalarAsync());

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

        // ОБНОВЛЕНИЕ ПОЛЬЗОВАТЕЛЯ
        public async Task<bool> UpdateUserAsync(int userId, string firstName, string lastName, string username, string email, string avatarUrl = null)
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

        // ПРОВЕРКА УНИКАЛЬНОСТИ USERNAME И EMAIL (исключая текущего пользователя)
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

        // ЗАГРУЗКА АВАТАРА
        public async Task<string> UploadAvatarAsync(Stream imageStream, string fileName, int userId)
        {
            try
            {
                // В реальном приложении здесь была бы загрузка в облачное хранилище
                // Для демонстрации сохраняем локально или генерируем URL

                // Генерируем уникальное имя файла
                var fileExtension = Path.GetExtension(fileName);
                var newFileName = $"avatar_{userId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";

                // В реальном приложении:
                // 1. Загружаем в Azure Blob Storage, AWS S3 и т.д.
                // 2. Возвращаем URL загруженного файла

                // Для демонстрации возвращаем фиктивный URL
                return $"https://example.com/avatars/{newFileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки аватара: {ex.Message}");
                return null;
            }
        }

    }
}