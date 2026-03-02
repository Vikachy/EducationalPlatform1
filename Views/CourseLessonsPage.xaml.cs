using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class CourseLessonsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _courseId;
        private readonly string _courseName;

        private Label? _courseTitleLabel;
        private Label? _courseNameLabel;
        private Label? _courseInfoLabel;
        private Label? _totalLessonsLabel;
        private Label? _theoryCountLabel;
        private Label? _practiceCountLabel;
        private CollectionView? _lessonsCollectionView;

        public ObservableCollection<LessonModel> Lessons { get; set; } = new();

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CourseLessonsPage(User user, DatabaseService dbService, SettingsService settingsService, int courseId, string courseName)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации CourseLessonsPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseId = courseId;
            _courseName = courseName;

            InitializeControls();
            BindingContext = this;

            Task.Run(async () => await LoadLessonsAsync());
        }

        private void InitializeControls()
        {
            _courseTitleLabel = this.FindByName<Label>("CourseTitleLabel");
            _courseNameLabel = this.FindByName<Label>("CourseNameLabel");
            _courseInfoLabel = this.FindByName<Label>("CourseInfoLabel");
            _totalLessonsLabel = this.FindByName<Label>("TotalLessonsLabel");
            _theoryCountLabel = this.FindByName<Label>("TheoryCountLabel");
            _practiceCountLabel = this.FindByName<Label>("PracticeCountLabel");
            _lessonsCollectionView = this.FindByName<CollectionView>("LessonsCollectionView");

            if (_courseTitleLabel != null)
                _courseTitleLabel.Text = $"Уроки: {_courseName}";

            if (_courseNameLabel != null)
                _courseNameLabel.Text = _courseName;

            if (_lessonsCollectionView != null)
                _lessonsCollectionView.ItemsSource = Lessons;
        }

        private async Task LoadLessonsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                // Загружаем информацию о курсе
                var course = await connection.QueryFirstOrDefaultAsync<CourseInfo>(@"
            SELECT 
                c.CourseName,
                c.Description,
                ISNULL(l.LanguageName, 'Не указан') as LanguageName,
                ISNULL(d.DifficultyName, 'Не указана') as DifficultyName,
                ISNULL(u.FirstName + ' ' + u.LastName, 'Не назначен') as TeacherName
            FROM Courses c
            LEFT JOIN ProgrammingLanguages l ON c.LanguageId = l.LanguageId
            LEFT JOIN Difficulties d ON c.DifficultyId = d.DifficultyId
            LEFT JOIN Users u ON c.CreatedByUserId = u.UserId
            WHERE c.CourseId = @CourseId
        ", new { CourseId = _courseId });

                // Загружаем уроки (без CreatedDate если её нет)
                var lessons = await connection.QueryAsync<LessonModel>(@"
            SELECT 
                l.LessonId,
                l.LessonType,
                l.Title,
                l.Content,
                ISNULL(l.LessonOrder, l.LessonId) as LessonOrder,
                ISNULL(l.IsActive, 1) as IsActive,
                ISNULL(m.ModuleName, 'Основной модуль') as ModuleName,
                GETDATE() as CreatedDate
            FROM Lessons l
            LEFT JOIN CourseModules m ON l.ModuleId = m.ModuleId
            WHERE m.CourseId = @CourseId OR l.ModuleId IN (SELECT ModuleId FROM CourseModules WHERE CourseId = @CourseId)
            ORDER BY ISNULL(l.LessonOrder, l.LessonId)
        ", new { CourseId = _courseId });

                var lessonsList = lessons.ToList();

                // Подсчет статистики
                int theoryCount = lessonsList.Count(l => l.LessonType?.ToLower() == "theory");
                int practiceCount = lessonsList.Count(l => l.LessonType?.ToLower() == "practice");
                int testCount = lessonsList.Count(l => l.LessonType?.ToLower() == "test");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (course != null && _courseInfoLabel != null)
                    {
                        _courseInfoLabel.Text = $"{course.LanguageName} • {course.DifficultyName} • {course.TeacherName}";
                    }

                    if (_totalLessonsLabel != null)
                        _totalLessonsLabel.Text = lessonsList.Count.ToString();

                    if (_theoryCountLabel != null)
                        _theoryCountLabel.Text = theoryCount.ToString();

                    if (_practiceCountLabel != null)
                        _practiceCountLabel.Text = practiceCount.ToString();

                    Lessons.Clear();
                    foreach (var lesson in lessonsList)
                    {
                        lesson.ContentPreview = GetContentPreview(lesson.Content);
                        lesson.LessonTypeDisplay = GetLessonTypeDisplay(lesson.LessonType ?? "theory");
                        Lessons.Add(lesson);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки уроков: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить уроки: {ex.Message}", "OK");
                });
            }
        }

        private string GetContentPreview(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return "Нет содержимого";

            return content.Length > 100 ? content.Substring(0, 100) + "..." : content;
        }

        private string GetLessonTypeDisplay(string lessonType)
        {
            return lessonType.ToLower() switch
            {
                "theory" => "📖 Теоретический урок",
                "practice" => "⚙️ Практическое задание",
                "test" => "📝 Тест",
                _ => "📚 Урок"
            };
        }

        private async void OnViewLessonClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is LessonModel lesson)
            {
                switch (lesson.LessonType.ToLower())
                {
                    case "theory":
                        await Navigation.PushAsync(new TheoryStudyPage(_currentUser, _dbService, _settingsService, lesson.LessonId));
                        break;
                    case "practice":
                        await Navigation.PushAsync(new PracticePage(_currentUser, _dbService, _settingsService, _courseId, lesson.LessonId, lesson.Title));
                        break;
                    case "test":
                        await Navigation.PushAsync(new TestStudyPage(_currentUser, _dbService, _settingsService, lesson.LessonId));
                        break;
                    default:
                        await DisplayAlert("Информация", $"Просмотр урока: {lesson.Title}", "OK");
                        break;
                }
            }
        }

        private async void OnEditLessonClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is LessonModel lesson)
            {
                switch (lesson.LessonType.ToLower())
                {
                    case "theory":
                        await Navigation.PushAsync(new EditTheoryPage(_currentUser, _dbService, _settingsService, lesson.LessonId));
                        break;
                    case "practice":
                        await Navigation.PushAsync(new EditPracticePage(_currentUser, _dbService, _settingsService, lesson.LessonId));
                        break;
                    case "test":
                        await Navigation.PushAsync(new EditTestPage(_currentUser, _dbService, _settingsService, lesson.LessonId));
                        break;
                    default:
                        await DisplayAlert("Информация", $"Редактирование урока: {lesson.Title}", "OK");
                        break;
                }
            }
        }

        private async void OnLessonStatsClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is LessonModel lesson)
            {
                try
                {
                    using var connection = new SqlConnection(_dbService.ConnectionString);
                    await connection.OpenAsync();

                    var stats = await connection.QueryFirstOrDefaultAsync<LessonStats>(@"
                        SELECT 
                            (SELECT COUNT(*) FROM StudentProgress WHERE LessonId = @LessonId AND Status = 'completed') as CompletedCount,
                            (SELECT COUNT(*) FROM StudentProgress WHERE LessonId = @LessonId) as TotalAttempts,
                            ISNULL((SELECT AVG(CAST(Score AS FLOAT)) FROM StudentProgress WHERE LessonId = @LessonId AND Score IS NOT NULL), 0) as AverageScore
                    ", new { LessonId = lesson.LessonId });

                    await DisplayAlert("Статистика урока",
                        $"📊 Статистика урока '{lesson.Title}'\n\n" +
                        $"✅ Завершили: {stats?.CompletedCount ?? 0} студентов\n" +
                        $"📝 Всего попыток: {stats?.TotalAttempts ?? 0}\n" +
                        $"📈 Средний балл: {stats?.AverageScore ?? 0:F1}",
                        "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", ex.Message, "OK");
                }
            }
        }

        private async void OnDeleteLessonClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is LessonModel lesson)
            {
                bool confirm = await DisplayAlert("Подтверждение",
                    $"Вы уверены, что хотите удалить урок '{lesson.Title}'?\nЭто действие необратимо.",
                    "Да", "Нет");

                if (confirm)
                {
                    try
                    {
                        using var connection = new SqlConnection(_dbService.ConnectionString);
                        await connection.OpenAsync();

                        await connection.ExecuteAsync("DELETE FROM Lessons WHERE LessonId = @LessonId",
                            new { LessonId = lesson.LessonId });

                        Lessons.Remove(lesson);

                        // Обновляем статистику
                        if (_totalLessonsLabel != null)
                            _totalLessonsLabel.Text = Lessons.Count.ToString();

                        int theoryCount = Lessons.Count(l => l.LessonType == "theory");
                        int practiceCount = Lessons.Count(l => l.LessonType == "practice");

                        if (_theoryCountLabel != null)
                            _theoryCountLabel.Text = theoryCount.ToString();

                        if (_practiceCountLabel != null)
                            _practiceCountLabel.Text = practiceCount.ToString();

                        await DisplayAlert("Успех", "Урок удален", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", ex.Message, "OK");
                    }
                }
            }
        }

        private async void OnAddLessonClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Выберите тип урока", "Отмена", null,
                "📖 Теория", "⚙️ Практика", "📝 Тест");

            switch (action)
            {
                case "📖 Теория":
                    await Navigation.PushAsync(new CreateTheoryPage(_currentUser, _dbService, _settingsService, _courseId));
                    break;
                case "⚙️ Практика":
                    await Navigation.PushAsync(new CreatePracticePage(_currentUser, _dbService, _settingsService, _courseId));
                    break;
                case "📝 Тест":
                    await Navigation.PushAsync(new CreateTestPage(_currentUser, _dbService, _settingsService, _courseId));
                    break;
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class LessonModel
    {
        public int LessonId { get; set; }
        public string LessonType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public int LessonOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string ContentPreview { get; set; } = string.Empty;
        public string LessonTypeDisplay { get; set; } = string.Empty;
    }

    public class CourseInfo
    {
        public string CourseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string DifficultyName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public int TotalLessons { get; set; }
    }

    public class LessonStats
    {
        public int CompletedCount { get; set; }
        public int TotalAttempts { get; set; }
        public double AverageScore { get; set; }
    }
}