using System.Collections.ObjectModel;
using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class MyCoursesPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        public ObservableCollection<MyEnrolledCourse> EnrolledCourses { get; set; } = new();

        public MyCoursesPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadEnrolledCourses();
        }

        private async void LoadEnrolledCourses()
        {
            try
            {
                EnrolledCourses.Clear();
                var progressList = await _dbService.GetStudentProgressAsync(_currentUser.UserId);
                foreach (var p in progressList)
                {
                    EnrolledCourses.Add(new MyEnrolledCourse
                    {
                        CourseName = p.CourseName,
                        Status = p.Status,
                        Progress = p.Score ?? 0
                    });
                }

                EnrolledCoursesCollection.ItemsSource = EnrolledCourses;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить мои курсы: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnOpenCourseClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is MyEnrolledCourse item)
            {
                await Navigation.PushAsync(new CourseDetailPage(_currentUser, _dbService, _settingsService, item.CourseName));
            }
        }

        private async void OnContinueCourseClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is MyEnrolledCourse item)
            {
                await DisplayAlert("Курс", $"Продолжить: {item.CourseName}", "OK");
            }
        }
    }

    public class MyEnrolledCourse
    {
        public string CourseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Progress { get; set; }
        public double ProgressDecimal => Progress / 100.0;
    }
}




