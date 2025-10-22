using EducationalPlatform.Models;
using EducationalPlatform.Services;
using EducationalPlatform.Converters;
using System.Collections.ObjectModel;
using System.Globalization;

namespace EducationalPlatform.Views
{
    public partial class TeacherManagementPage : ContentPage
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;

        public ObservableCollection<TeacherCourse> MyCourses { get; set; }
        public ObservableCollection<PendingReview> PendingReviews { get; set; }

        public TeacherManagementPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            MyCourses = new ObservableCollection<TeacherCourse>();
            PendingReviews = new ObservableCollection<PendingReview>();

            // Регистрируем конвертеры
            Resources.Add("PublishedConverter", new PublishedToTextConverter());
            Resources.Add("StatusColorConverter", new StatusColorConverter());
            Resources.Add("PublishButtonConverter", new PublishButtonTextConverter());
            Resources.Add("PublishButtonColorConverter", new PublishButtonColorConverter());

            BindingContext = this;

            LoadTeacherData();
        }

        private async void LoadTeacherData()
        {
            try
            {
                var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                MyCourses.Clear();
                foreach (var course in courses)
                {
                    MyCourses.Add(course);
                }

                MyCoursesCollectionView.ItemsSource = MyCourses;

                // Загружаем работы на проверку
                LoadPendingReviews();

                // Заполняем пикеры
                LoadPickersData();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные: {ex.Message}", "OK");
            }
        }

        private void LoadPendingReviews()
        {
            try
            {
                PendingReviews.Clear();

                // Временно добавляем тестовые данные
                PendingReviews.Add(new PendingReview
                {
                    AttemptId = 1,
                    StudentName = "Иван Петров",
                    CourseName = "C# для начинающих",
                    TestTitle = "Основы ООП",
                    Score = 75
                });

                PendingReviewsCollectionView.ItemsSource = PendingReviews;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки работ на проверку: {ex.Message}");
            }
        }

        private void LoadPickersData()
        {
            try
            {
                // Временно добавляем тестовые данные
                var languages = new List<ProgrammingLanguage>
                {
                    new ProgrammingLanguage { LanguageId = 1, LanguageName = "C#" },
                    new ProgrammingLanguage { LanguageId = 2, LanguageName = "Python" },
                    new ProgrammingLanguage { LanguageId = 3, LanguageName = "Java" }
                };
                LanguagePicker.ItemsSource = languages;

                var difficulties = new List<CourseDifficulty>
                {
                    new CourseDifficulty { DifficultyId = 1, DifficultyName = "Легкий" },
                    new CourseDifficulty { DifficultyId = 2, DifficultyName = "Средний" },
                    new CourseDifficulty { DifficultyId = 3, DifficultyName = "Сложный" }
                };
                DifficultyPicker.ItemsSource = difficulties;

                CourseForTestPicker.ItemsSource = MyCourses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки данных для пикеров: {ex.Message}");
            }
        }

        // СОЗДАНИЕ КУРСА
        private async void OnCreateCourseClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewCourseNameEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите название курса", "OK");
                return;
            }

            if (LanguagePicker.SelectedItem == null || DifficultyPicker.SelectedItem == null)
            {
                await DisplayAlert("Ошибка", "Выберите язык программирования и сложность", "OK");
                return;
            }

            try
            {
                var selectedLanguage = (ProgrammingLanguage)LanguagePicker.SelectedItem;
                var selectedDifficulty = (CourseDifficulty)DifficultyPicker.SelectedItem;

                bool success = await _dbService.CreateCourseAsync(
                    NewCourseNameEntry.Text,
                    NewCourseDescriptionEditor.Text,
                    selectedLanguage.LanguageId,
                    selectedDifficulty.DifficultyId,
                    _currentUser.UserId,
                    IsGroupCourseCheckBox.IsChecked);

                if (success)
                {
                    await DisplayAlert("Успех", "Курс успешно создан!", "OK");
                    // Очищаем поля
                    NewCourseNameEntry.Text = "";
                    NewCourseDescriptionEditor.Text = "";
                    LanguagePicker.SelectedItem = null;
                    DifficultyPicker.SelectedItem = null;
                    IsGroupCourseCheckBox.IsChecked = false;

                    // Обновляем список курсов
                    LoadTeacherData();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось создать курс", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка создания курса: {ex.Message}", "OK");
            }
        }

        // СОЗДАНИЕ ТЕСТА
        private async void OnCreateTestClicked(object sender, EventArgs e)
        {
            if (CourseForTestPicker.SelectedItem == null)
            {
                await DisplayAlert("Ошибка", "Выберите курс для теста", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewTestTitleEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите название теста", "OK");
                return;
            }

            try
            {
                var selectedCourse = (TeacherCourse)CourseForTestPicker.SelectedItem;

                bool success = await _dbService.CreateTestAsync(
                    selectedCourse.CourseId,
                    NewTestTitleEntry.Text,
                    NewTestDescriptionEditor.Text,
                    int.TryParse(TimeLimitEntry.Text, out int timeLimit) ? timeLimit : 30,
                    int.TryParse(PassingScoreEntry.Text, out int passingScore) ? passingScore : 60);

                if (success)
                {
                    await DisplayAlert("Успех", "Тест успешно создан!", "OK");
                    // Очищаем поля
                    NewTestTitleEntry.Text = "";
                    NewTestDescriptionEditor.Text = "";
                    TimeLimitEntry.Text = "";
                    PassingScoreEntry.Text = "";
                    CourseForTestPicker.SelectedItem = null;
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось создать тест", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка создания теста: {ex.Message}", "OK");
            }
        }

        // УПРАВЛЕНИЕ КУРСАМИ
        private async void OnPublishCourseClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                try
                {
                    bool success = await _dbService.PublishCourseAsync(course.CourseId, _currentUser.UserId);
                    if (success)
                    {
                        await DisplayAlert("Успех",
                            course.IsPublished ? "Курс снят с публикации!" : "Курс опубликован!",
                            "OK");
                        LoadTeacherData();
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось изменить статус курса", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
                }
            }
        }

        private async void OnManageGroupsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                await DisplayAlert("Группы", $"Управление группами курса: {course.CourseName}", "OK");
            }
        }

        private async void OnCourseStatsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                await DisplayAlert("Статистика", $"Статистика по курсу: {course.CourseName}", "OK");
            }
        }

        // ПРОВЕРКА РАБОТ
        private async void OnReviewWorkClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is PendingReview review)
            {
                await DisplayAlert("Проверка", $"Проверка работы: {review.StudentName}", "OK");
            }
        }

        // БЫСТРЫЙ ДОСТУП
        private async void OnReportsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Отчеты", "Генерация отчетов в Word/Excel", "OK");
        }

        private async void OnGroupsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Группы", "Управление учебными группами", "OK");
        }

        private async void OnTestsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Тесты", "Управление тестами и заданиями", "OK");
        }

        private async void OnChatsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Чаты", "Общение с учениками", "OK");
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    // ДОПОЛНИТЕЛЬНЫЕ МОДЕЛИ
    public class PendingReview
    {
        public int AttemptId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string TestTitle { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    // КОНВЕРТЕРЫ ДЛЯ КНОПОК
    public class PublishButtonTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool published && published ? "Снять" : "Опубликовать";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PublishButtonColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool published && published ? Color.FromArgb("#FF9800") : Color.FromArgb("#4CAF50");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}