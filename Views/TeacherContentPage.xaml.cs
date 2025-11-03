using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class TeacherContentPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        public TeacherContentPage(User user, DatabaseService dbService, SettingsService settingsService)
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

        private async void OnAddTheoryClicked(object sender, EventArgs e)
        {
            if (CoursePicker.SelectedItem is not TeacherCourse course)
            {
                await DisplayAlert("Ошибка", "Выберите курс", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(TheoryTitleEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите заголовок", "OK");
                return;
            }
            var id = await _dbService.AddTheoryLessonAsync(course.CourseId, TheoryTitleEntry.Text!, TheoryContentEditor.Text);
            await DisplayAlert(id.HasValue ? "Успех" : "Ошибка", id.HasValue ? "Теория добавлена" : "Не удалось добавить теорию", "OK");
        }

        private async void OnAddPracticeClicked(object sender, EventArgs e)
        {
            if (CoursePicker.SelectedItem is not TeacherCourse course)
            {
                await DisplayAlert("Ошибка", "Выберите курс", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(PracticeTitleEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите заголовок", "OK");
                return;
            }
            var id = await _dbService.AddPracticeLessonAsync(
                course.CourseId,
                PracticeTitleEntry.Text!,
                StarterCodeEditor.Text,
                ExpectedOutputEditor.Text,
                TestCasesEditor.Text,
                HintEntry.Text);

            await DisplayAlert(id.HasValue ? "Успех" : "Ошибка", id.HasValue ? "Практика добавлена" : "Не удалось добавить практику", "OK");
        }

        private async void OnAddTestClicked(object sender, EventArgs e)
        {
            if (CoursePicker.SelectedItem is not TeacherCourse course)
            {
                await DisplayAlert("Ошибка", "Выберите курс", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(TestTitleEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите название теста", "OK");
                return;
            }
            int timeLimit = int.TryParse(TestTimeLimitEntry.Text, out var t) ? t : 30;
            int passing = int.TryParse(TestPassingScoreEntry.Text, out var p) ? p : 60;

            var ok = await _dbService.CreateSimpleTestAsync(course.CourseId, TestTitleEntry.Text!, TestDescriptionEditor.Text, timeLimit, passing);
            await DisplayAlert(ok ? "Успех" : "Ошибка", ok ? "Тест добавлен" : "Не удалось добавить тест", "OK");
        }
    }
}










