using EducationalPlatform.Models;
using EducationalPlatform.Services;
using EducationalPlatform.Converters;
using System.ComponentModel;

namespace EducationalPlatform.Views
{
    public partial class TeacherDashboardPage : ContentPage, INotifyPropertyChanged
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;

        private int _totalCourses;
        private int _totalStudents;
        private double _averageRating;

        public int TotalCourses
        {
            get => _totalCourses;
            set
            {
                _totalCourses = value;
                OnPropertyChanged();
            }
        }

        public int TotalStudents
        {
            get => _totalStudents;
            set
            {
                _totalStudents = value;
                OnPropertyChanged();
            }
        }

        public double AverageRating
        {
            get => _averageRating;
            set
            {
                _averageRating = value;
                OnPropertyChanged();
            }
        }

        public TeacherDashboardPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            // ������������ ����������
            Resources.Add("PublishedConverter", new PublishedToTextConverter());
            Resources.Add("StatusColorConverter", new StatusColorConverter());

            BindingContext = this;
            LoadTeacherData();
        }

        private async void LoadTeacherData()
        {
            try
            {
                var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);

                TotalCourses = courses.Count;
                TotalStudents = courses.Sum(c => c.StudentCount);
                AverageRating = courses.Any() ? courses.Average(c => c.AverageRating) : 0;

                CoursesCollectionView.ItemsSource = courses;
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"�� ������� ��������� ������: {ex.Message}", "OK");
            }
        }

        private async void OnCreateCourseClicked(object sender, EventArgs e)
        {
            await DisplayAlert("�������� �����", "������� � ����������", "OK");
        }

        private async void OnReportsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("������", "������� � ����������", "OK");
        }

        private async void OnManageCourseClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                await DisplayAlert("���������� ������", $"���������� ������: {course.CourseName}", "OK");
            }
        }

        private async void OnManageGroupsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                await DisplayAlert("���������� ��������", $"������ �����: {course.CourseName}", "OK");
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}