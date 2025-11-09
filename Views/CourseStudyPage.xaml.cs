using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class CourseStudyPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _courseId;

        public ObservableCollection<CourseLesson> Lessons { get; set; } = new();

        public CourseStudyPage(User user, DatabaseService dbService, SettingsService settingsService, int courseId)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseId = courseId;

            BindingContext = this;
            LoadCourseData();
        }

        private async void LoadCourseData()
        {
            try
            {
                // Получаем информацию о курсе
                var courses = await _dbService.GetAvailableCoursesAsync();
                var course = courses.FirstOrDefault(c => c.CourseId == _courseId);

                if (course != null)
                {
                    CourseTitleLabel.Text = course.CourseName;
                }

                // Загружаем уроки
                var lessons = await _dbService.GetCourseLessonsAsync(_courseId);
                Lessons.Clear();
                foreach (var lesson in lessons)
                {
                    Lessons.Add(lesson);
                }
                LessonsCollection.ItemsSource = Lessons;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные курса: {ex.Message}", "OK");
            }
        }

        private async void OnLessonSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is CourseLesson selectedLesson)
            {
                // В зависимости от типа урока открываем соответствующую страницу
                switch (selectedLesson.LessonType.ToLower())
                {
                    case "theory":
                        await Navigation.PushAsync(new TheoryStudyPage(_currentUser, _dbService, _settingsService, selectedLesson.LessonId));
                        break;
                    case "practice":
                        await Navigation.PushAsync(new PracticeStudyPage(_currentUser, _dbService, _settingsService, selectedLesson.LessonId));
                        break;
                    case "test":
                        await Navigation.PushAsync(new TestStudyPage(_currentUser, _dbService, _settingsService, selectedLesson.LessonId));
                        break;
                    default:
                        await DisplayAlert("Информация", $"Тип урока '{selectedLesson.LessonType}' пока не поддерживается", "OK");
                        break;
                }

                // Снимаем выделение
                LessonsCollection.SelectedItem = null;
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    // Конвертер для отображения типов уроков
    public class LessonTypeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            return value?.ToString()?.ToLower() switch
            {
                "theory" => "📖 Теория",
                "practice" => "💻 Практика",
                "test" => "📝 Тест",
                _ => "📄 Урок"
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}