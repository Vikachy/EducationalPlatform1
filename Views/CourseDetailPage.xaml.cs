using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class CourseDetailPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly string _courseName;
        private int _courseId;
        private Course _currentCourse;

        public ObservableCollection<StudentGroupChatItem> StudentGroups { get; set; }

        public CourseDetailPage(User user, DatabaseService dbService, SettingsService settingsService, string courseName)
        {
            InitializeComponent();

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseName = courseName;

            StudentGroups = new ObservableCollection<StudentGroupChatItem>();
            BindingContext = this;

            InitializePage();
        }

        private void InitializePage()
        {
            CourseTitle.Text = _courseName;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCourseData();
        }

        private async Task LoadCourseData()
        {
            try
            {
                await LoadCourseId();
                await Task.WhenAll(
                    LoadCourseContent(),
                    LoadStudentGroups()
                );
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные курса: {ex.Message}", "OK");
            }
        }

        private async Task LoadCourseId()
        {
            var allCourses = await _dbService.GetAvailableCoursesAsync();
            _currentCourse = allCourses.FirstOrDefault(c => c.CourseName == _courseName);

            if (_currentCourse == null)
            {
                await DisplayAlert("Ошибка", "Курс не найден", "OK");
                return;
            }

            _courseId = _currentCourse.CourseId;
        }

        private async Task LoadCourseContent()
        {
            var lessons = await _dbService.GetCourseLessonsAsync(_courseId);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TheoryCollection.ItemsSource = lessons.Where(l => l.LessonType == "theory").ToList();
                PracticeCollection.ItemsSource = lessons.Where(l => l.LessonType == "practice").ToList();
                TestsCollection.ItemsSource = lessons.Where(l => l.LessonType == "test").ToList();

                UpdateSectionTitles(lessons);
            });
        }

        private async Task LoadStudentGroups()
        {
            try
            {
                if (_currentUser.RoleId == 1) 
                {
                    var groups = await _dbService.GetStudentGroupChatsAsync(_currentUser.UserId);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StudentGroups.Clear();

                        var courseGroups = groups.Where(g =>
                            g.CourseName.Contains(_courseName) ||
                            g.GroupName.Contains(_courseName)
                        ).ToList();

                        foreach (var group in courseGroups)
                        {
                            StudentGroups.Add(new StudentGroupChatItem
                            {
                                GroupId = group.GroupId,
                                GroupName = group.GroupName,
                                CourseName = group.CourseName,
                                StudentCount = group.StudentCount
                            });
                        }

                        ChatsSection.IsVisible = StudentGroups.Any();
                    });
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ChatsSection.IsVisible = false;
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки групповых чатов: {ex.Message}");
            }
        }

        private void UpdateSectionTitles(IEnumerable<CourseLesson> lessons)
        {
            var theoryCount = lessons.Count(l => l.LessonType == "theory");
            var practiceCount = lessons.Count(l => l.LessonType == "practice");
            var testCount = lessons.Count(l => l.LessonType == "test");

            TheoryLabel.Text = $"📖 Теория ({theoryCount})";
            PracticeLabel.Text = $"💻 Практика ({practiceCount})";
            TestsLabel.Text = $"📝 Тесты ({testCount})";
        }

        private async void OnTheorySelected(object sender, SelectionChangedEventArgs e)
        {
            await HandleLessonSelection(sender, e, (lesson) =>
                new TheoryStudyPage(_currentUser, _dbService, _settingsService, lesson.LessonId));
        }

        private async void OnPracticeSelected(object sender, SelectionChangedEventArgs e)
        {
            await HandleLessonSelection(sender, e, (lesson) =>
                new PracticePage(_currentUser, _dbService, _settingsService, _courseId, lesson.LessonId, lesson.Title));
        }

        private async void OnTestSelected(object sender, SelectionChangedEventArgs e)
        {
            await HandleLessonSelection(sender, e, (lesson) =>
                new TestStudyPage(_currentUser, _dbService, _settingsService, lesson.LessonId));
        }

        private async Task HandleLessonSelection(object sender, SelectionChangedEventArgs e, Func<CourseLesson, Page> pageCreator)
        {
            if (e.CurrentSelection.FirstOrDefault() is CourseLesson lesson)
            {
                try
                {
                    var page = pageCreator(lesson);
                    await Navigation.PushAsync(page);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть урок: {ex.Message}", "OK");
                }
            }

            if (e.CurrentSelection.FirstOrDefault() != null && sender is CollectionView collectionView)
            {
                collectionView.SelectedItem = null;
            }
        }

        private async void OnGroupChatSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is StudentGroupChatItem group)
            {
                try
                {
                    var studyGroup = new StudyGroup
                    {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName
                    };

                    if (IsStudent)
                    {
                        await Navigation.PushAsync(new GroupChatPage(studyGroup, _currentUser, _dbService, _settingsService));
                    }
                    else if (IsTeacher)
                    {
                        await Navigation.PushAsync(new TeacherGroupsPage(_currentUser, _dbService, _settingsService));
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть групповой чат: {ex.Message}", "OK");
                }
            }

            if (e.CurrentSelection.FirstOrDefault() != null && sender is CollectionView collectionView)
            {
                collectionView.SelectedItem = null;
            }
        }

        private async void OnTeacherChatClicked(object sender, EventArgs e)
        {
            try
            {
                if (IsStudent) 
                {
                    await OpenStudentChatsPage();
                }
                else if (IsTeacher) 
                {
                    await OpenTeacherChatsPage();
                }
                else
                {
                    await DisplayAlert("Информация", "Чат недоступен для вашей роли", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть чат: {ex.Message}", "OK");
            }
        }

        private async Task OpenStudentChatsPage()
        {
            try
            {
                await Navigation.PushAsync(new StudentChatsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть чаты: {ex.Message}", "OK");
            }
        }

        private async Task OpenTeacherChatsPage()
        {
            try
            {
                await Navigation.PushAsync(new TeacherGroupsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть группы: {ex.Message}", "OK");
            }
        }

        private int GetTeacherIdForCourse()
        {
            return _currentCourse?.CreatedByUserId ?? 0;
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private bool IsStudent => _currentUser.RoleId == 1;
        private bool IsTeacher => _currentUser.RoleId == 2;
    }

   
}