using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace EducationalPlatform.Views
{
    public partial class ManageCourseContentPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly Course _course;

        // Статистика
        private int _theoryCount;
        public int TheoryCount
        {
            get => _theoryCount;
            set
            {
                _theoryCount = value;
                OnPropertyChanged(nameof(TheoryCount));
            }
        }

        private int _practiceCount;
        public int PracticeCount
        {
            get => _practiceCount;
            set
            {
                _practiceCount = value;
                OnPropertyChanged(nameof(PracticeCount));
            }
        }

        private int _testCount;
        public int TestCount
        {
            get => _testCount;
            set
            {
                _testCount = value;
                OnPropertyChanged(nameof(TestCount));
            }
        }

        public ManageCourseContentPage(User user, DatabaseService dbService, SettingsService settingsService, Course course)
        {
            InitializeComponent();
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _course = course;

            BindingContext = this;

            // Устанавливаем заголовок курса
            CourseTitleLabel.Text = course.CourseName;

            LoadLessons();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadLessons();
        }

        private async void LoadLessons()
        {
            try
            {
                var lessons = await _dbService.GetCourseLessonsAsync(_course.CourseId);

                // Обновляем коллекции
                TheoryCollection.ItemsSource = lessons.Where(l => l.LessonType == "theory").OrderBy(l => l.LessonOrder).ToList();
                PracticeCollection.ItemsSource = lessons.Where(l => l.LessonType == "practice").OrderBy(l => l.LessonOrder).ToList();
                TestsCollection.ItemsSource = lessons.Where(l => l.LessonType == "test").OrderBy(l => l.LessonOrder).ToList();

                UpdateSectionTitles(lessons);
                UpdateStatistics(lessons);
                UpdateEmptyStates(lessons);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить уроки: {ex.Message}", "OK");
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

        private void UpdateStatistics(IEnumerable<CourseLesson> lessons)
        {
            TheoryCount = lessons.Count(l => l.LessonType == "theory");
            PracticeCount = lessons.Count(l => l.LessonType == "practice");
            TestCount = lessons.Count(l => l.LessonType == "test");
        }

        private void UpdateEmptyStates(IEnumerable<CourseLesson> lessons)
        {
            // Обновляем видимость сообщений о пустых состояниях
            EmptyTheoryLabel.IsVisible = !lessons.Any(l => l.LessonType == "theory");
            EmptyPracticeLabel.IsVisible = !lessons.Any(l => l.LessonType == "practice");
            EmptyTestsLabel.IsVisible = !lessons.Any(l => l.LessonType == "test");
        }

        // Добавление нового контента
        private async void OnAddTheoryClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateTheoryPage(_user, _dbService, _settingsService, _course.CourseId));
        }

        private async void OnAddPracticeClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreatePracticePage(_user, _dbService, _settingsService, _course.CourseId));
        }

        private async void OnAddTestClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateTestPage(_user, _dbService, _settingsService, _course.CourseId));
        }

        // Редактирование урока
        private async void OnEditLessonClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is CourseLesson lesson)
            {
                switch (lesson.LessonType?.ToLower())
                {
                    case "theory":
                        await Navigation.PushAsync(new EditTheoryPage(_user, _dbService, _settingsService, lesson.LessonId));
                        break;
                    case "practice":
                        await Navigation.PushAsync(new EditPracticePage(_user, _dbService, _settingsService, lesson.LessonId));
                        break;
                    case "test":
                        await Navigation.PushAsync(new EditTestPage(_user, _dbService, _settingsService, lesson.LessonId));
                        break;
                    default:
                        await DisplayAlert("Информация", $"Редактирование типа '{lesson.LessonType}' пока не поддерживается", "OK");
                        break;
                }
            }
        }

        // Удаление урока
        private async void OnDeleteLessonClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is CourseLesson lesson)
            {
                bool confirm = await DisplayAlert("Подтверждение удаления",
                    $"Вы уверены, что хотите удалить урок '{lesson.Title}'?\nЭто действие нельзя отменить.",
                    "Удалить", "Отмена");

                if (confirm)
                {
                    try
                    {
                        var success = await _dbService.DeleteLessonAsync(lesson.LessonId);
                        if (success)
                        {
                            await DisplayAlert("Успех", "Урок удален", "OK");
                            LoadLessons(); // Обновляем список
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Не удалось удалить урок", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", $"Ошибка удаления: {ex.Message}", "OK");
                    }
                }
            }
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            LoadLessons();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}