using System.Collections.ObjectModel;
using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class CourseStudentsPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _courseId;

        public ObservableCollection<CourseStudentItem> Students { get; set; } // Изменили на CourseStudentItem
        public string CourseName { get; set; } = string.Empty;
        public int StudentCount { get; set; }

        public CourseStudentsPage(User user, DatabaseService dbService, SettingsService settingsService, int courseId, string courseName)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseId = courseId;

            Students = new ObservableCollection<CourseStudentItem>(); // Изменили на CourseStudentItem
            CourseName = courseName;
            BindingContext = this;

            LoadCourseStudents();
        }

        private async void LoadCourseStudents()
        {
            try
            {
                var students = await _dbService.GetCourseStudentsAsync(_courseId);
                Students.Clear();
                foreach (var student in students)
                {
                    // Создаем новый объект с правильным типом
                    Students.Add(new CourseStudentItem
                    {
                        StudentId = student.StudentId,
                        StudentName = student.StudentName,
                        Username = student.Username,
                        Progress = student.Progress,
                        LastActivity = student.LastActivity
                    });
                }
                StudentCount = Students.Count;
                OnPropertyChanged(nameof(StudentCount));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить студентов: {ex.Message}", "OK");
            }
        }

        private async void OnViewStatsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is CourseStudentItem student)
            {
                await DisplayAlert("Статистика", $"Статистика студента: {student.StudentName}", "OK");
            }
        }

        private async void OnChatClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is CourseStudentItem student)
            {
                try
                {
                    // Создаем группу для приватного чата со студентом
                    var privateGroup = new StudyGroup
                    {
                        GroupId = -student.StudentId, // Отрицательный ID для приватных чатов
                        GroupName = $"Чат с {student.StudentName}",
                        StudentCount = 2
                    };
                    await Navigation.PushAsync(new ChatPage(privateGroup, _currentUser, _dbService, _settingsService));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть чат: {ex.Message}", "OK");
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    // Переименовали класс чтобы избежать конфликта
    public class CourseStudentItem
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int Progress { get; set; }
        public DateTime LastActivity { get; set; }
    }
}










