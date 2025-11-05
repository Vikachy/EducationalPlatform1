using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class CourseDetailPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly string _courseName;
        private int _courseId;

        public CourseDetailPage(User user, DatabaseService dbService, SettingsService settingsService, string courseName)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseName = courseName;
            CourseTitle.Text = courseName;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCourseContent();
        }

        private async Task LoadCourseContent()
        {
            try
            {
                // получаем идентификатор курса по названию
                var all = await _dbService.GetAvailableCoursesAsync();
                var course = all.FirstOrDefault(c => c.CourseName == _courseName);
                if (course == null)
                {
                    await DisplayAlert("Ошибка", "Курс не найден", "OK");
                    return;
                }
                _courseId = course.CourseId;

                var lessons = await _dbService.GetCourseLessonsAsync(_courseId);

                // Обновляем коллекции
                TheoryCollection.ItemsSource = lessons.Where(l => l.LessonType == "theory").ToList();
                PracticeCollection.ItemsSource = lessons.Where(l => l.LessonType == "practice").ToList();
                TestsCollection.ItemsSource = lessons.Where(l => l.LessonType == "test").ToList();

                UpdateSectionTitles(lessons);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить содержимое курса: {ex.Message}", "OK");
            }
        }

        private void UpdateSectionTitles(IEnumerable<CourseLesson> lessons)
        {
            var theoryCount = lessons.Count(l => l.LessonType == "theory");
            var practiceCount = lessons.Count(l => l.LessonType == "practice");
            var testCount = lessons.Count(l => l.LessonType == "test");

            // Обновляем заголовки секций
            TheoryLabel.Text = $"Теория ({theoryCount})";
            PracticeLabel.Text = $"Практика ({practiceCount})";
            TestsLabel.Text = $"Тесты ({testCount})";
        }

        // Обработчик для теории
        private async void OnTheorySelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is CourseLesson lesson)
            {
                await Navigation.PushAsync(new TheoryStudyPage(
                    _currentUser,
                    _dbService,
                    _settingsService,
                    lesson.LessonId
                ));
            }
            ((CollectionView)sender).SelectedItem = null;
        }

        private async void OnPracticeSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is CourseLesson lesson)
            {
                await Navigation.PushAsync(new PracticePage(
                    _currentUser,
                    _dbService,
                    _settingsService,
                    _courseId,
                    lesson.LessonId,
                    lesson.Title
                ));
            }
            ((CollectionView)sender).SelectedItem = null;
        }

        private async void OnTestSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is CourseLesson lesson)
            {
                await Navigation.PushAsync(new TestPage(
                    _currentUser,
                    _dbService,
                    _settingsService,
                    _courseId,
                    lesson.LessonId
                ));
            }
            ((CollectionView)sender).SelectedItem = null;
        }

        private async void OnTeacherChatClicked(object sender, EventArgs e)
        {
            try
            {
                var groups = await _dbService.GetTeacherStudyGroupsAsync(_currentUser.UserId);
                var group = groups.FirstOrDefault();
                if (group != null)
                    await Navigation.PushAsync(new ChatPage(group, _currentUser, _dbService, _settingsService));
                else
                    await DisplayAlert("Информация", "Группа преподавателя не найдена", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть чат: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}