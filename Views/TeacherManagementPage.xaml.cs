using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class TeacherManagementPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        public ObservableCollection<TeacherCourse> MyCourses { get; set; } = new();
        public ObservableCollection<PracticeSubmission> PendingReviews { get; set; } = new();

        public TeacherManagementPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();

            _currentUser = user ?? throw new ArgumentNullException(nameof(user));
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            // Конвертеры для UI
            Resources.Add("PublishedConverter", new PublishedToTextConverter());
            Resources.Add("PublishButtonConverter", new PublishButtonTextConverter());
            Resources.Add("PublishButtonColorConverter", new PublishButtonColorConverter());

            BindingContext = this;

            _ = LoadTeacherDataAsync();
        }

        private async Task LoadTeacherDataAsync()
        {
            try
            {
                // 1. Курсы преподавателя
                var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                MyCourses.Clear();
                foreach (var course in courses ?? Enumerable.Empty<TeacherCourse>())
                {
                    MyCourses.Add(course);
                }

                // 2. Реальные работы на проверку
                await LoadPendingReviewsAsync();

                // 3. Списки для создания курса
                LanguagePicker.ItemsSource = await _dbService.GetProgrammingLanguagesAsync();
                DifficultyPicker.ItemsSource = await _dbService.GetCourseDifficultiesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка загрузки", ex.Message, "OK");
            }
        }

        private async Task LoadPendingReviewsAsync()
        {
            PendingReviews.Clear();

            var pending = await _dbService.GetPendingPracticeSubmissionsForTeacherAsync(_currentUser.UserId);

            if (pending == null || !pending.Any())
            {
                PendingReviews.Add(new PracticeSubmission
                {
                    StudentName = "Нет работ на проверку",
                    CourseName = "",
                    LessonTitle = "Пока никто не отправил практику",
                    SubmissionDate = DateTime.Now
                });
            }
            else
            {
                foreach (var submission in pending)
                {
                    PendingReviews.Add(submission);
                }
            }

            PendingReviewsCollectionView.ItemsSource = PendingReviews;
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
                await DisplayAlert("Ошибка", "Выберите язык и сложность", "OK");
                return;
            }

            try
            {
                var lang = (ProgrammingLanguage)LanguagePicker.SelectedItem;
                var diff = (CourseDifficulty)DifficultyPicker.SelectedItem;

                bool success = await _dbService.CreateCourseAsync(
                    NewCourseNameEntry.Text.Trim(),
                    NewCourseDescriptionEditor.Text?.Trim() ?? "",
                    lang.LanguageId,
                    diff.DifficultyId,
                    _currentUser.UserId,
                    IsGroupCourseCheckBox.IsChecked);

                if (success)
                {
                    await DisplayAlert("Успех", "Курс успешно создан!", "OK");
                    NewCourseNameEntry.Text = "";
                    NewCourseDescriptionEditor.Text = "";
                    LanguagePicker.SelectedItem = null;
                    DifficultyPicker.SelectedItem = null;
                    IsGroupCourseCheckBox.IsChecked = false;

                    await LoadTeacherDataAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось создать курс", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка создания курса", ex.Message, "OK");
            }
        }

        private async void OnPublishCourseClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is TeacherCourse course)
            {
                try
                {
                    bool newStatus = !course.IsPublished;
                    bool success = await _dbService.PublishCourseAsync(course.CourseId, newStatus ? 1 : 0);

                    if (success)
                    {
                        course.IsPublished = newStatus;
                        await DisplayAlert("Успех", newStatus ? "Курс опубликован" : "Публикация снята", "OK");

                        MyCoursesCollectionView.ItemsSource = null;
                        MyCoursesCollectionView.ItemsSource = MyCourses;
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось изменить статус", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка публикации", ex.Message, "OK");
                }
            }
        }

        private async void OnReviewWorkClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is int submissionId)
            {
                var submission = PendingReviews.FirstOrDefault(s => s.SubmissionId == submissionId);
                if (submission != null)
                {
                    string preview = submission.SubmissionText;
                    if (string.IsNullOrEmpty(preview) && !string.IsNullOrEmpty(submission.SubmissionFileUrl))
                    {
                        preview = $"Файл: {Path.GetFileName(submission.SubmissionFileUrl)}";
                    }
                    else if (string.IsNullOrEmpty(preview))
                    {
                        preview = "Нет ответа";
                    }

                    await DisplayAlert("Проверка работы",
                        $"Студент: {submission.StudentName}\n" +
                        $"Курс: {submission.CourseName}\n" +
                        $"Урок: {submission.LessonTitle}\n" +
                        $"Дата: {submission.SubmissionDate:dd.MM.yy HH:mm}\n\n" +
                        $"Ответ:\n{preview}",
                        "Закрыть");
                }
            }
        }

        private async void OnManageGroupsClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is TeacherCourse course)
            {
                await Navigation.PushAsync(new TeacherGroupsPage(_currentUser, _dbService, _settingsService, course));
            }
        }

        private async void OnCourseStatsClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is int courseId)
            {
                var course = MyCourses.FirstOrDefault(c => c.CourseId == courseId);
                if (course != null)
                {
                    await Navigation.PushAsync(new CourseStudentsPage(_currentUser, _dbService, _settingsService, courseId, course.CourseName));
                }
            }
        }

        private async void OnManageContentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is TeacherCourse course)
            {
                await Navigation.PushAsync(new TeacherContentManagementPage(_currentUser, _dbService, _settingsService, course.CourseId, course.CourseName));
            }
        }

        private async void OnReportsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Отчёты", "Функция в разработке", "OK");
        }

        private async void OnTestsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TeacherTestsPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnGroupsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TeacherGroupsPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnChatsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TeacherChatsPage(_currentUser, _dbService, _settingsService));
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

    // Конвертеры
    public class PublishedToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool p && p ? "Опубликован" : "Черновик";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class PublishButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool p && p ? "Снять публикацию" : "Опубликовать";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class PublishButtonColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool p && p ? Colors.Orange : Colors.Green;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}