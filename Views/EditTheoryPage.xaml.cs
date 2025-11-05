using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class EditTheoryPage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _lessonId;
        private CourseLesson _lesson;

        public EditTheoryPage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
        {
            InitializeComponent();
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _lessonId = lessonId;

            LoadLessonData();
        }

        private async void LoadLessonData()
        {
            try
            {
                // Получаем данные урока
                var courseId = await _dbService.GetCourseIdByLessonAsync(_lessonId);
                if (courseId.HasValue)
                {
                    var lessons = await _dbService.GetCourseLessonsAsync(courseId.Value);
                    _lesson = lessons.FirstOrDefault(l => l.LessonId == _lessonId);

                    if (_lesson != null)
                    {
                        TitleEntry.Text = _lesson.Title;
                        OrderEntry.Text = _lesson.LessonOrder.ToString();

                        var content = await _dbService.GetLessonContentAsync(_lessonId);
                        ContentEditor.Text = content ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные урока: {ex.Message}", "OK");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TitleEntry.Text))
                {
                    await DisplayAlert("Ошибка", "Введите название урока", "OK");
                    return;
                }

                // TODO: Реализовать метод обновления теории в DatabaseService
                await DisplayAlert("Информация", "Функция редактирования теории будет реализована позже", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка сохранения: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}