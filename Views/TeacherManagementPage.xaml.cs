using EducationalPlatform.Models;
using EducationalPlatform.Services;
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

            // Инициализация конвертеров
            Resources.Add("PublishedConverter", new PublishedToTextConverter());
            Resources.Add("PublishButtonConverter", new PublishButtonTextConverter());
            Resources.Add("PublishButtonColorConverter", new PublishButtonColorConverter());

            BindingContext = this;

            // Загружаем данные без await, так как это конструктор
            _ = LoadTeacherDataAsync();
        }

        private async Task LoadTeacherDataAsync()
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

                // Загрузка работ на проверку
                LoadPendingReviews();

                // Загрузка списков
                await LoadPickersDataAsync();
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

                // Заглушка для демонстрации работ на проверку
                PendingReviews.Add(new PendingReview
                {
                    AttemptId = 1,
                    StudentName = "Иван Иванов",
                    CourseName = "C# для начинающих",
                    TestTitle = "Тест по ООП",
                    Score = 75
                });

                PendingReviewsCollectionView.ItemsSource = PendingReviews;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки работ на проверку: {ex.Message}");
            }
        }

        private async Task LoadPickersDataAsync()
        {
            try
            {
                var languages = await _dbService.GetProgrammingLanguagesAsync();
                LanguagePicker.ItemsSource = languages;

                var difficulties = await _dbService.GetCourseDifficultiesAsync();
                DifficultyPicker.ItemsSource = difficulties;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки списков для выбора: {ex.Message}");
            }
        }

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
                    NewCourseDescriptionEditor.Text ?? string.Empty,
                    selectedLanguage.LanguageId,
                    selectedDifficulty.DifficultyId,
                    _currentUser.UserId,
                    IsGroupCourseCheckBox.IsChecked);

                if (success)
                {
                    await DisplayAlert("Успех", "Курс создан успешно!", "OK");

                    // Очистка полей
                    NewCourseNameEntry.Text = "";
                    NewCourseDescriptionEditor.Text = "";
                    LanguagePicker.SelectedItem = null;
                    DifficultyPicker.SelectedItem = null;
                    IsGroupCourseCheckBox.IsChecked = false;

                    // Обновление списка курсов
                    await LoadTeacherDataAsync();
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

        // Публикация курса
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
                        await LoadTeacherDataAsync();
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
                try
                {
                    await Navigation.PushAsync(new TeacherGroupsPage(_currentUser, _dbService, _settingsService, course));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть группы: {ex.Message}", "OK");
                }
            }
        }

        private async void OnCourseStatsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                try
                {
                    await Navigation.PushAsync(new CourseStudentsPage(_currentUser, _dbService, _settingsService, course.CourseId, course.CourseName));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть статистику: {ex.Message}", "OK");
                }
            }
        }

        private async void OnManageContentClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                try
                {
                    await Navigation.PushAsync(new TeacherContentManagementPage(_currentUser, _dbService, _settingsService, course.CourseId, course.CourseName));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть управление контентом: {ex.Message}", "OK");
                }
            }
        }

        // Просмотр работы
        private async void OnReviewWorkClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is PendingReview review)
            {
                await DisplayAlert("Проверка", $"Проверка работы: {review.StudentName}", "OK");
            }
        }

        // Отчеты
        private async void OnReportsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Отчеты", "Экспорт отчетов в Word/Excel", "OK");
        }

        private async void OnGroupsClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new TeacherGroupsManagementPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть группы: {ex.Message}", "OK");
            }
        }

        private async void OnTestsClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new TeacherTestsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть тесты: {ex.Message}", "OK");
            }
        }

        private async void OnChatsClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new TeacherChatsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть чаты: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadTeacherDataAsync();
        }
    }

    // Вспомогательные классы
    public class PendingReview
    {
        public int AttemptId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string TestTitle { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    // Конвертеры (только уникальные для этой страницы)
    public class PublishedToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "Опубликован" : "Черновик";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PublishButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "Снять" : "Опубликовать";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PublishButtonColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Color.FromArgb("#FF9800") : Color.FromArgb("#4CAF50");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}