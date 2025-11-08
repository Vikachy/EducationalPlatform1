using System.Collections.ObjectModel;
using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class TeacherContentManagementPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _courseId;

        public ObservableCollection<ContentItem> ContentItems { get; set; }
        public string CourseName { get; set; } = string.Empty;

        public TeacherContentManagementPage(User user, DatabaseService dbService, SettingsService settingsService, int courseId, string courseName)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseId = courseId;

            ContentItems = new ObservableCollection<ContentItem>();
            CourseName = courseName;
            BindingContext = this;

            LoadCourseContent();
        }

        private async void LoadCourseContent()
        {
            try
            {
                var lessons = await _dbService.GetCourseLessonsAsync(_courseId);
                ContentItems.Clear();
                foreach (var lesson in lessons)
                {
                    ContentItems.Add(new ContentItem
                    {
                        LessonId = lesson.LessonId,
                        Title = lesson.Title,
                        Type = lesson.LessonType,
                        TypeIcon = lesson.LessonType switch
                        {
                            "theory" => "📚",
                            "practice" => "💻",
                            "test" => "📝",
                            _ => "📄"
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить контент: {ex.Message}", "OK");
            }
        }

        private async void OnAddTheoryClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TheoryTitleEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите название урока", "OK");
                return;
            }

            try
            {
                var lessonId = await _dbService.AddTheoryLessonAsync(_courseId, TheoryTitleEntry.Text, TheoryContentEditor.Text);
                if (lessonId.HasValue)
                {
                    await DisplayAlert("Успех", "Теория добавлена!", "OK");
                    TheoryTitleEntry.Text = string.Empty;
                    TheoryContentEditor.Text = string.Empty;
                    LoadCourseContent();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось добавить теорию", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
            }
        }

        private async void OnAddPracticeClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PracticeTitleEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите название задания", "OK");
                return;
            }

            try
            {
                var lessonId = await _dbService.AddPracticeLessonAsync(
                    _courseId,
                    PracticeTitleEntry.Text,
                    StarterCodeEditor.Text,
                    ExpectedOutputEntry.Text,
                    null, // testCasesJson
                    PracticeDescriptionEditor.Text // используем description как hint
                );

                if (lessonId.HasValue)
                {
                    await DisplayAlert("Успех", "Практика добавлена!", "OK");
                    PracticeTitleEntry.Text = string.Empty;
                    PracticeDescriptionEditor.Text = string.Empty;
                    StarterCodeEditor.Text = string.Empty;
                    ExpectedOutputEntry.Text = string.Empty;
                    LoadCourseContent();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось добавить практику", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
            }
        }

        private async void OnAddTestClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TestTitleEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите название теста", "OK");
                return;
            }

            if (!int.TryParse(TimeLimitEntry.Text, out int timeLimit))
            {
                await DisplayAlert("Ошибка", "Введите корректное время", "OK");
                return;
            }

            if (!int.TryParse(PassingScoreEntry.Text, out int passingScore))
            {
                await DisplayAlert("Ошибка", "Введите корректный проходной балл", "OK");
                return;
            }

            try
            {
                // Используем упрощенный метод создания теста
                var success = await _dbService.CreateSimpleTestAsync(_courseId, TestTitleEntry.Text,
                    TestDescriptionEditor.Text, timeLimit, passingScore);

                if (success)
                {
                    await DisplayAlert("Успех", "Тест добавлен!", "OK");
                    TestTitleEntry.Text = string.Empty;
                    TestDescriptionEditor.Text = string.Empty;
                    TimeLimitEntry.Text = string.Empty;
                    PassingScoreEntry.Text = string.Empty;
                    LoadCourseContent();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось добавить тест", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
            }
        }

        private async void OnEditContentClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is ContentItem item)
            {
                await DisplayAlert("Редактирование", $"Редактирование: {item.Title}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class ContentItem
    {
        public int LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string TypeIcon { get; set; } = string.Empty;
    }
}









