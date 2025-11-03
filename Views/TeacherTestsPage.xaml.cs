using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class TeacherTestsPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        public TeacherTestsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
            CoursePicker.ItemsSource = courses;
            CoursePicker.ItemDisplayBinding = new Binding("CourseName");
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            if (CoursePicker.SelectedItem is not TeacherCourse course)
            {
                await DisplayAlert("Ошибка", "Выберите курс", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(TitleEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите название теста", "OK");
                return;
            }

            int timeLimit = int.TryParse(TimeLimitEntry.Text, out var t) ? t : 30;
            int passing = int.TryParse(PassingScoreEntry.Text, out var p) ? p : 60;

            try
            {
                // Создаем тест с автосозданием модуля при отсутствии
                bool ok = await _dbService.CreateSimpleTestAsync(course.CourseId, TitleEntry.Text!, DescriptionEditor.Text, timeLimit, passing);
                await DisplayAlert(ok ? "Успех" : "Ошибка", ok ? "Тест создан" : "Не удалось создать тест", "OK");
                if (ok)
                {
                    TitleEntry.Text = string.Empty;
                    DescriptionEditor.Text = string.Empty;
                    TimeLimitEntry.Text = string.Empty;
                    PassingScoreEntry.Text = string.Empty;
                    CoursePicker.SelectedItem = null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка создания теста: {ex.Message}", "OK");
            }
        }
    }
}
