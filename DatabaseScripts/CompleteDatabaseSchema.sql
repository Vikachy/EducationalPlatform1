-- =============================================
-- Полная схема базы данных EducationalPlatform
-- Платформа для изучения языков программирования
-- =============================================

-- 1. МОДУЛЬ: АУТЕНТИФИКАЦИЯ И ПОЛЬЗОВАТЕЛИ
-- =============================================

-- Таблица ролей
CREATE TABLE Roles (
    RoleId INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL UNIQUE, -- Student, Teacher, Admin, ContentManager
    Permissions NVARCHAR(MAX), -- JSON с правами доступа
    Description NVARCHAR(255)
);

-- Вставка базовых ролей
INSERT INTO Roles (RoleName, Description) VALUES 
('Student', 'Студент'),
('Teacher', 'Преподаватель'),
('Admin', 'Администратор'),
('ContentManager', 'Контент-менеджер');

-- Таблица пользователей
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    AvatarUrl NVARCHAR(500),
    LanguagePref NVARCHAR(10) DEFAULT 'ru', -- ru, en
    InterfaceStyle NVARCHAR(20) DEFAULT 'standard', -- standard, teen
    RegistrationDate DATETIME2 DEFAULT GETDATE(),
    RoleId INT FOREIGN KEY REFERENCES Roles(RoleId),
    IsActive BIT DEFAULT 1,
    GameCurrency INT DEFAULT 0, -- Игровая валюта 💰
    StreakDays INT DEFAULT 0, -- Серия дней входа 🔥
    LastLoginDate DATE, -- Для отслеживания серии
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    UpdatedDate DATETIME2 DEFAULT GETDATE()
);

-- Таблица приветствий при входе
CREATE TABLE LoginGreetings (
    GreetingId INT PRIMARY KEY IDENTITY(1,1),
    MessageText NVARCHAR(500) NOT NULL,
    LanguageCode NVARCHAR(10) DEFAULT 'ru',
    ForTeens BIT DEFAULT 0, -- Упрощенный язык для подростков
    IsActive BIT DEFAULT 1
);

-- Вставка приветствий
INSERT INTO LoginGreetings (MessageText, LanguageCode, ForTeens) VALUES 
('Спасибо, что не забыл про меня! 💕', 'ru', 0),
('Ты вернулся! Мы так ждали! 🎉', 'ru', 0),
('Привет! Твоя серия продолжается! 🔥', 'ru', 0),
('С возвращением! Готов к новым вызовам? ⚡', 'ru', 0),
('Мы скучали по тебе! 💙', 'ru', 0),
('Привет! Время стать лучше! 🌟', 'ru', 0),
('С возвращением! Твои навыки ждут! 💻', 'ru', 0),
('Thanks for not forgetting me! 💕', 'en', 0),
('You''re back! We''ve been waiting! 🎉', 'en', 0),
('Hello! Your streak continues! 🔥', 'en', 0),
('Welcome back! Ready for new challenges? ⚡', 'en', 0),
('We missed you! 💙', 'en', 0),
('Hello! Time to get better! 🌟', 'en', 0),
('Welcome back! Your skills are waiting! 💻', 'en', 0);

-- Таблица согласий на обработку персональных данных
CREATE TABLE PrivacyConsents (
    ConsentId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    ConsentText NVARCHAR(MAX) NOT NULL,
    Version NVARCHAR(10) NOT NULL DEFAULT '1.0',
    ConsentDate DATETIME2 DEFAULT GETDATE(),
    IPAddress NVARCHAR(50),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- 2. МОДУЛЬ: ЯЗЫКИ ПРОГРАММИРОВАНИЯ И КУРСЫ
-- =============================================

-- Таблица языков программирования
CREATE TABLE ProgrammingLanguages (
    LanguageId INT PRIMARY KEY IDENTITY(1,1),
    LanguageName NVARCHAR(100) NOT NULL UNIQUE,
    IconUrl NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    Description NVARCHAR(500)
);

-- Вставка популярных языков
INSERT INTO ProgrammingLanguages (LanguageName, Description) VALUES 
('C#', 'Объектно-ориентированный язык программирования от Microsoft'),
('Python', 'Высокоуровневый язык программирования общего назначения'),
('JavaScript', 'Язык программирования для веб-разработки'),
('Java', 'Популярный объектно-ориентированный язык программирования'),
('C++', 'Мощный язык программирования для системного программирования'),
('PHP', 'Серверный язык программирования для веб-разработки'),
('Swift', 'Язык программирования для разработки iOS приложений'),
('Kotlin', 'Современный язык программирования для Android');

-- Таблица сложности курсов
CREATE TABLE CourseDifficulties (
    DifficultyId INT PRIMARY KEY IDENTITY(1,1),
    DifficultyName NVARCHAR(50) NOT NULL, -- Легкий, Средний, Сложный
    Description NVARCHAR(500),
    HasTheory BIT DEFAULT 1,
    HasPractice BIT DEFAULT 1,
    ColorCode NVARCHAR(7) -- HEX код цвета для UI
);

-- Вставка уровней сложности
INSERT INTO CourseDifficulties (DifficultyName, Description, HasTheory, HasPractice, ColorCode) VALUES 
('Легкий', 'Только теория, базовые понятия', 1, 0, '#4CAF50'),
('Средний', 'Теория + практика, средний уровень', 1, 1, '#FF9800'),
('Сложный', 'Только практика, продвинутый уровень', 0, 1, '#F44336');

-- Таблица курсов
CREATE TABLE Courses (
    CourseId INT PRIMARY KEY IDENTITY(1,1),
    CourseName NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    LanguageId INT FOREIGN KEY REFERENCES ProgrammingLanguages(LanguageId),
    DifficultyId INT FOREIGN KEY REFERENCES CourseDifficulties(DifficultyId),
    CreatedByUserId INT FOREIGN KEY REFERENCES Users(UserId),
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    IsPublished BIT DEFAULT 0,
    IsGroupCourse BIT DEFAULT 0, -- Для учебных групп или индивидуальный
    Price DECIMAL(10,2) DEFAULT 0,
    EstimatedHours INT,
    Tags NVARCHAR(500), -- Для поиска
    Rating DECIMAL(3,2) DEFAULT 0, -- Средняя оценка курса
    StudentCount INT DEFAULT 0 -- Количество студентов
);

-- Таблица модулей курса
CREATE TABLE CourseModules (
    ModuleId INT PRIMARY KEY IDENTITY(1,1),
    CourseId INT FOREIGN KEY REFERENCES Courses(CourseId),
    ModuleName NVARCHAR(255) NOT NULL,
    ModuleOrder INT NOT NULL,
    Description NVARCHAR(MAX),
    IsActive BIT DEFAULT 1
);

-- Таблица уроков
CREATE TABLE Lessons (
    LessonId INT PRIMARY KEY IDENTITY(1,1),
    ModuleId INT FOREIGN KEY REFERENCES CourseModules(ModuleId),
    LessonType NVARCHAR(20) NOT NULL, -- theory, practice, test, video
    Title NVARCHAR(500) NOT NULL,
    Content NVARCHAR(MAX), -- Теория в HTML/Markdown
    LessonOrder INT NOT NULL,
    IsActive BIT DEFAULT 1,
    EstimatedTime INT -- Время в минутах
);

-- Таблица практических упражнений
CREATE TABLE PracticeExercises (
    ExerciseId INT PRIMARY KEY IDENTITY(1,1),
    LessonId INT FOREIGN KEY REFERENCES Lessons(LessonId),
    StarterCode NVARCHAR(MAX),
    ExpectedOutput NVARCHAR(MAX),
    TestCases NVARCHAR(MAX), -- JSON с тест-кейсами
    Hint NVARCHAR(500),
    Difficulty INT DEFAULT 1 -- 1-5
);

-- 3. МОДУЛЬ: ГРУППЫ И НАЗНАЧЕНИЯ
-- =============================================

-- Таблица учебных групп
CREATE TABLE StudyGroups (
    GroupId INT PRIMARY KEY IDENTITY(1,1),
    GroupName NVARCHAR(255) NOT NULL,
    TeacherId INT FOREIGN KEY REFERENCES Users(UserId),
    CourseId INT FOREIGN KEY REFERENCES Courses(CourseId),
    StartDate DATE,
    EndDate DATE,
    IsActive BIT DEFAULT 1,
    MaxStudents INT DEFAULT 30,
    CreatedDate DATETIME2 DEFAULT GETDATE()
);

-- Таблица записи в группы
CREATE TABLE GroupEnrollments (
    EnrollmentId INT PRIMARY KEY IDENTITY(1,1),
    GroupId INT FOREIGN KEY REFERENCES StudyGroups(GroupId),
    StudentId INT FOREIGN KEY REFERENCES Users(UserId),
    EnrollmentDate DATETIME2 DEFAULT GETDATE(),
    Status NVARCHAR(20) DEFAULT 'active' -- active, completed, dropped
);

-- 4. МОДУЛЬ: ТЕСТИРОВАНИЕ И ОЦЕНКИ
-- =============================================

-- Таблица тестов
CREATE TABLE Tests (
    TestId INT PRIMARY KEY IDENTITY(1,1),
    LessonId INT FOREIGN KEY REFERENCES Lessons(LessonId),
    Title NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX),
    TimeLimitMinutes INT,
    MaxScore INT DEFAULT 100,
    PassingScore INT DEFAULT 60,
    IsActive BIT DEFAULT 1
);

-- Таблица вопросов
CREATE TABLE Questions (
    QuestionId INT PRIMARY KEY IDENTITY(1,1),
    TestId INT FOREIGN KEY REFERENCES Tests(TestId),
    QuestionText NVARCHAR(MAX) NOT NULL,
    QuestionType NVARCHAR(20) NOT NULL, -- single, multiple, code, text
    Score INT DEFAULT 1,
    QuestionOrder INT NOT NULL,
    Explanation NVARCHAR(MAX) -- Объяснение правильного ответа
);

-- Таблица вариантов ответов
CREATE TABLE AnswerOptions (
    AnswerId INT PRIMARY KEY IDENTITY(1,1),
    QuestionId INT FOREIGN KEY REFERENCES Questions(QuestionId),
    AnswerText NVARCHAR(MAX),
    IsCorrect BIT DEFAULT 0,
    OrderIndex INT DEFAULT 0
);

-- Таблица попыток прохождения тестов
CREATE TABLE TestAttempts (
    AttemptId INT PRIMARY KEY IDENTITY(1,1),
    TestId INT FOREIGN KEY REFERENCES Tests(TestId),
    StudentId INT FOREIGN KEY REFERENCES Users(UserId),
    GroupId INT FOREIGN KEY REFERENCES StudyGroups(GroupId), -- NULL если индивидуальный курс
    StartTime DATETIME2 DEFAULT GETDATE(),
    EndTime DATETIME2,
    Score INT,
    AutoScore INT, -- Автоматическая оценка
    TeacherScore INT, -- Оценка преподавателя
    Status NVARCHAR(20) DEFAULT 'in_progress', -- in_progress, completed, under_review
    IsDisputed BIT DEFAULT 0, -- Оспорена студентом
    TeacherComment NVARCHAR(MAX)
);

-- Таблица ответов студентов
CREATE TABLE StudentAnswers (
    AnswerId INT PRIMARY KEY IDENTITY(1,1),
    AttemptId INT FOREIGN KEY REFERENCES TestAttempts(AttemptId),
    QuestionId INT FOREIGN KEY REFERENCES Questions(QuestionId),
    SelectedAnswerId INT FOREIGN KEY REFERENCES AnswerOptions(AnswerId), -- NULL для кода
    CodeAnswer NVARCHAR(MAX),
    TextAnswer NVARCHAR(MAX),
    IsCorrect BIT, -- Результат автоматической проверки
    TeacherComment NVARCHAR(MAX),
    PointsEarned INT DEFAULT 0
);

-- 5. МОДУЛЬ: ПРОГРЕСС И ГЕЙМИФИКАЦИЯ
-- =============================================

-- Таблица прогресса студентов
CREATE TABLE StudentProgress (
    ProgressId INT PRIMARY KEY IDENTITY(1,1),
    StudentId INT FOREIGN KEY REFERENCES Users(UserId),
    CourseId INT FOREIGN KEY REFERENCES Courses(CourseId),
    LessonId INT FOREIGN KEY REFERENCES Lessons(LessonId),
    Status NVARCHAR(20) DEFAULT 'not_started', -- not_started, in_progress, completed
    StartDate DATETIME2,
    CompletionDate DATETIME2,
    Score INT,
    AttemptsCount INT DEFAULT 0,
    TimeSpent INT DEFAULT 0 -- Время в минутах
);

-- Таблица достижений
CREATE TABLE Achievements (
    AchievementId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    IconUrl NVARCHAR(500),
    ConditionType NVARCHAR(50), -- courses_completed, perfect_scores, streak, currency
    ConditionValue INT, -- Например, 5 курсов
    RewardCurrency INT DEFAULT 0,
    IsActive BIT DEFAULT 1
);

-- Вставка базовых достижений
INSERT INTO Achievements (Name, Description, IconUrl, ConditionType, ConditionValue, RewardCurrency) VALUES 
('Первый шаг', 'Завершил первый курс', '🎯', 'courses_completed', 1, 10),
('Начинающий программист', 'Завершил 3 курса', '🚀', 'courses_completed', 3, 50),
('Опытный кодер', 'Завершил 10 курсов', '💻', 'courses_completed', 10, 100),
('Бог программирования', 'Завершил 25 курсов', '🏆', 'courses_completed', 25, 500),
('Серийный ученик', 'Входил 7 дней подряд', '🔥', 'streak', 7, 25),
('Непрерывное обучение', 'Входил 30 дней подряд', '⭐', 'streak', 30, 100),
('Отличник', 'Получил 5 отличных оценок', '🌟', 'perfect_scores', 5, 50),
('Богач', 'Накопил 1000 монет', '💰', 'currency', 1000, 0);

-- Таблица достижений студентов
CREATE TABLE StudentAchievements (
    StudentAchievementId INT PRIMARY KEY IDENTITY(1,1),
    StudentId INT FOREIGN KEY REFERENCES Users(UserId),
    AchievementId INT FOREIGN KEY REFERENCES Achievements(AchievementId),
    EarnedDate DATETIME2 DEFAULT GETDATE(),
    IsVisible BIT DEFAULT 1
);

-- Таблица персонализации профиля
CREATE TABLE ProfileCustomizations (
    CustomizationId INT PRIMARY KEY IDENTITY(1,1),
    StudentId INT FOREIGN KEY REFERENCES Users(UserId),
    ItemType NVARCHAR(50), -- avatar_frame, emoji, theme, badge
    ItemValue NVARCHAR(500),
    IsActive BIT DEFAULT 0,
    PurchasedWithCurrency BIT DEFAULT 0,
    PurchaseDate DATETIME2
);

-- 6. МОДУЛЬ: КОММУНИКАЦИЯ И ОТЗЫВЫ
-- =============================================

-- Таблица отзывов о курсах
CREATE TABLE CourseReviews (
    ReviewId INT PRIMARY KEY IDENTITY(1,1),
    CourseId INT FOREIGN KEY REFERENCES Courses(CourseId),
    StudentId INT FOREIGN KEY REFERENCES Users(UserId),
    Rating INT CHECK (Rating >= 0 AND Rating <= 5),
    Comment NVARCHAR(MAX),
    ReviewDate DATETIME2 DEFAULT GETDATE(),
    IsApproved BIT DEFAULT 1,
    HelpfulCount INT DEFAULT 0 -- Количество "полезно"
);

-- Таблица групповых чатов
CREATE TABLE GroupChats (
    MessageId INT PRIMARY KEY IDENTITY(1,1),
    GroupId INT FOREIGN KEY REFERENCES StudyGroups(GroupId),
    SenderId INT FOREIGN KEY REFERENCES Users(UserId),
    MessageText NVARCHAR(MAX) NOT NULL,
    SentDate DATETIME2 DEFAULT GETDATE(),
    IsRead BIT DEFAULT 0,
    MessageType NVARCHAR(20) DEFAULT 'text' -- text, file, image
);

-- Таблица обращений в поддержку
CREATE TABLE SupportTickets (
    TicketId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT FOREIGN KEY REFERENCES Users(UserId),
    Subject NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    TicketType NVARCHAR(50) DEFAULT 'bug', -- bug, question, feature, complaint
    Status NVARCHAR(20) DEFAULT 'open', -- open, in_progress, resolved, closed
    Priority INT DEFAULT 3, -- 1: high, 2: medium, 3: low
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    ResolvedDate DATETIME2,
    AdminComment NVARCHAR(MAX),
    AssignedTo INT FOREIGN KEY REFERENCES Users(UserId)
);

-- 7. МОДУЛЬ: КОНКУРСЫ И НОВОСТИ
-- =============================================

-- Таблица конкурсов
CREATE TABLE Contests (
    ContestId INT PRIMARY KEY IDENTITY(1,1),
    ContestName NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    LanguageId INT FOREIGN KEY REFERENCES ProgrammingLanguages(LanguageId),
    StartDate DATETIME2,
    EndDate DATETIME2,
    MaxParticipants INT,
    PrizeCurrency INT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    OnlyForGroups BIT DEFAULT 1, -- Только для учебных групп
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- Таблица заявок на конкурсы
CREATE TABLE ContestSubmissions (
    SubmissionId INT PRIMARY KEY IDENTITY(1,1),
    ContestId INT FOREIGN KEY REFERENCES Contests(ContestId),
    StudentId INT FOREIGN KEY REFERENCES Users(UserId),
    ProjectName NVARCHAR(255),
    ProjectFileUrl NVARCHAR(500), -- Ссылка на ZIP-файл
    Description NVARCHAR(MAX),
    SubmissionDate DATETIME2 DEFAULT GETDATE(),
    TeacherScore INT,
    TeacherComment NVARCHAR(MAX),
    IsWinner BIT DEFAULT 0
);

-- Таблица новостей
CREATE TABLE News (
    NewsId INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(500) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    AuthorId INT FOREIGN KEY REFERENCES Users(UserId),
    PublishedDate DATETIME2 DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1,
    LanguageCode NVARCHAR(10) DEFAULT 'ru',
    ForTeens BIT DEFAULT 0,
    ImageUrl NVARCHAR(500),
    ViewsCount INT DEFAULT 0
);

-- 8. МОДУЛЬ: МАГАЗИН И ВАЛЮТА
-- =============================================

-- Таблица товаров в магазине
CREATE TABLE ShopItems (
    ItemId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    Price INT NOT NULL, -- Цена в игровой валюте
    ItemType NVARCHAR(50) NOT NULL, -- avatar_frame, emoji, theme, badge
    Icon NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME2 DEFAULT GETDATE()
);

-- Вставка базовых товаров
INSERT INTO ShopItems (Name, Description, Price, ItemType, Icon) VALUES 
('Золотая рамка', 'Золотая рамка для аватара', 100, 'avatar_frame', '🖼️'),
('Серебряная рамка', 'Серебряная рамка для аватара', 50, 'avatar_frame', '🖼️'),
('Эмодзи программиста', 'Эмодзи 💻 для профиля', 25, 'emoji', '💻'),
('Эмодзи огня', 'Эмодзи 🔥 для профиля', 25, 'emoji', '🔥'),
('Тема "Темная"', 'Темная тема интерфейса', 200, 'theme', '🌙'),
('Значок "Отличник"', 'Значок для профиля', 150, 'badge', '🏆');

-- Таблица инвентаря пользователей
CREATE TABLE UserInventory (
    InventoryId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT FOREIGN KEY REFERENCES Users(UserId),
    ItemId INT FOREIGN KEY REFERENCES ShopItems(ItemId),
    PurchaseDate DATETIME2 DEFAULT GETDATE(),
    IsEquipped BIT DEFAULT 0
);

-- Таблица транзакций валюты
CREATE TABLE CurrencyTransactions (
    TransactionId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT FOREIGN KEY REFERENCES Users(UserId),
    Amount INT NOT NULL, -- Может быть отрицательным
    TransactionType NVARCHAR(20) NOT NULL, -- income, expense, reward, purchase
    Reason NVARCHAR(255),
    TransactionDate DATETIME2 DEFAULT GETDATE(),
    RelatedItemId INT -- Связанный товар или достижение
);

-- 9. СОЗДАНИЕ ИНДЕКСОВ ДЛЯ ПРОИЗВОДИТЕЛЬНОСТИ
-- =============================================

-- Индексы для быстрого поиска
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_RoleId ON Users(RoleId);
CREATE INDEX IX_PrivacyConsents_UserId ON PrivacyConsents(UserId);
CREATE INDEX IX_StudentProgress_StudentId ON StudentProgress(StudentId);
CREATE INDEX IX_StudentProgress_CourseId ON StudentProgress(CourseId);
CREATE INDEX IX_TestAttempts_StudentId ON TestAttempts(StudentId);
CREATE INDEX IX_TestAttempts_TestId ON TestAttempts(TestId);
CREATE INDEX IX_CourseReviews_CourseId ON CourseReviews(CourseId);
CREATE INDEX IX_GroupChats_GroupId ON GroupChats(GroupId);
CREATE INDEX IX_SupportTickets_UserId ON SupportTickets(UserId);
CREATE INDEX IX_SupportTickets_Status ON SupportTickets(Status);

-- 10. СОЗДАНИЕ ПРЕДСТАВЛЕНИЙ ДЛЯ АНАЛИТИКИ
-- =============================================

-- Представление статистики пользователей
CREATE VIEW UserStatistics AS
SELECT 
    u.UserId,
    u.Username,
    u.FirstName,
    u.LastName,
    u.GameCurrency,
    u.StreakDays,
    COUNT(DISTINCT sp.CourseId) as TotalCourses,
    COUNT(DISTINCT CASE WHEN sp.Status = 'completed' THEN sp.CourseId END) as CompletedCourses,
    AVG(CAST(sp.Score AS FLOAT)) as AverageScore,
    COUNT(DISTINCT sa.AchievementId) as AchievementsCount
FROM Users u
LEFT JOIN StudentProgress sp ON u.UserId = sp.StudentId
LEFT JOIN StudentAchievements sa ON u.UserId = sa.StudentId
GROUP BY u.UserId, u.Username, u.FirstName, u.LastName, u.GameCurrency, u.StreakDays;

-- Представление статистики курсов
CREATE VIEW CourseStatistics AS
SELECT 
    c.CourseId,
    c.CourseName,
    c.Rating,
    c.StudentCount,
    COUNT(DISTINCT sp.StudentId) as EnrolledStudents,
    COUNT(DISTINCT CASE WHEN sp.Status = 'completed' THEN sp.StudentId END) as CompletedStudents,
    AVG(CAST(sp.Score AS FLOAT)) as AverageScore
FROM Courses c
LEFT JOIN StudentProgress sp ON c.CourseId = sp.CourseId
GROUP BY c.CourseId, c.CourseName, c.Rating, c.StudentCount;

PRINT 'База данных EducationalPlatform создана успешно! 🎉';
PRINT 'Все таблицы, индексы и представления настроены.';
PRINT 'Готово к использованию! 💻';

