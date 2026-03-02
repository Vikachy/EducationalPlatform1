using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class TeacherReportsPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        // Свойства для привязки данных
        private int _totalStudents;
        public int TotalStudents
        {
            get => _totalStudents;
            set
            {
                _totalStudents = value;
                OnPropertyChanged();
            }
        }

        private int _totalCourses;
        public int TotalCourses
        {
            get => _totalCourses;
            set
            {
                _totalCourses = value;
                OnPropertyChanged();
            }
        }

        private string _completionRate;
        public string CompletionRate
        {
            get => _completionRate;
            set
            {
                _completionRate = value;
                OnPropertyChanged();
            }
        }

        private int _activeToday;
        public int ActiveToday
        {
            get => _activeToday;
            set
            {
                _activeToday = value;
                OnPropertyChanged();
            }
        }

        private int _activeThisWeek;
        public int ActiveThisWeek
        {
            get => _activeThisWeek;
            set
            {
                _activeThisWeek = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<CourseReportItem> CourseStats { get; set; } = new();

        public TeacherReportsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            BindingContext = this;
            PeriodPicker.SelectedIndex = 1; // За месяц по умолчанию

            LoadReportsData();
        }

        private async void LoadReportsData()
        {
            try
            {
                // Загружаем курсы преподавателя
                var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                TotalCourses = courses?.Count ?? 0;

                CourseStats.Clear();
                int totalStudentsAll = 0;
                int totalCompleted = 0;
                int totalScore = 0;
                int coursesWithData = 0;

                foreach (var course in courses ?? Enumerable.Empty<TeacherCourse>())
                {
                    // Получаем студентов курса
                    var students = await GetCourseStudentsCount(course.CourseId);
                    int studentCount = students;

                    // Получаем завершивших курс
                    int completedCount = await GetCourseCompletedCount(course.CourseId, _currentUser.UserId);

                    // Получаем средний балл
                    double avgScore = await GetCourseAverageScore(course.CourseId);

                    var stats = new CourseReportItem
                    {
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        IsPublished = course.IsPublished,
                        StudentCount = studentCount,
                        CompletedCount = completedCount,
                        AverageScore = avgScore.ToString("0"),
                        CompletionRate = studentCount > 0 ? (double)completedCount / studentCount : 0,
                        CompletionRatePercent = studentCount > 0 ? $"{(completedCount * 100 / studentCount)}%" : "0%"
                    };

                    CourseStats.Add(stats);

                    totalStudentsAll += studentCount;
                    totalCompleted += completedCount;
                    if (avgScore > 0)
                    {
                        totalScore += (int)avgScore;
                        coursesWithData++;
                    }
                }

                TotalStudents = totalStudentsAll;

                // Общий процент завершения
                if (totalStudentsAll > 0)
                {
                    int percent = (totalCompleted * 100 / totalStudentsAll);
                    CompletionRate = $"{percent}%";
                }
                else
                {
                    CompletionRate = "0%";
                }

                // Активность студентов
                await LoadStudentActivity();

                // Применяем фильтр
                ApplyFilter();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить отчеты: {ex.Message}", "OK");
            }
        }

        private async Task<int> GetCourseStudentsCount(int courseId)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT COUNT(DISTINCT sp.StudentId)
                    FROM StudentProgress sp
                    WHERE sp.CourseId = @CourseId";

                return await connection.ExecuteScalarAsync<int>(query, new { CourseId = courseId });
            }
            catch
            {
                return 0;
            }
        }

        private async Task<int> GetCourseCompletedCount(int courseId, int teacherId)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT COUNT(DISTINCT sp.StudentId)
                    FROM StudentProgress sp
                    WHERE sp.CourseId = @CourseId AND sp.Status = 'completed'";

                return await connection.ExecuteScalarAsync<int>(query, new { CourseId = courseId });
            }
            catch
            {
                return 0;
            }
        }

        private async Task<double> GetCourseAverageScore(int courseId)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT ISNULL(AVG(CAST(sp.Score AS FLOAT)), 0)
                    FROM StudentProgress sp
                    WHERE sp.CourseId = @CourseId AND sp.Score IS NOT NULL";

                return await connection.ExecuteScalarAsync<double>(query, new { CourseId = courseId });
            }
            catch
            {
                return 0;
            }
        }

        private async Task LoadStudentActivity()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                // Активные сегодня
                var todayQuery = @"
                    SELECT COUNT(DISTINCT UserId)
                    FROM LearningSession
                    WHERE CAST(StartTime AS DATE) = CAST(GETDATE() AS DATE)";

                ActiveToday = await connection.ExecuteScalarAsync<int>(todayQuery);

                // Активные на этой неделе
                var weekQuery = @"
                    SELECT COUNT(DISTINCT UserId)
                    FROM LearningSession
                    WHERE StartTime >= DATEADD(day, -7, GETDATE())";

                ActiveThisWeek = await connection.ExecuteScalarAsync<int>(weekQuery);
            }
            catch
            {
                ActiveToday = 0;
                ActiveThisWeek = 0;
            }
        }

        private void ApplyFilter()
        {
            var period = PeriodPicker.SelectedItem as string;

            // Здесь можно добавить логику фильтрации по периоду
            // Пока просто обновляем отображение
            OnPropertyChanged(nameof(CourseStats));
        }

        private void OnApplyFilterClicked(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class CourseReportItem
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public int StudentCount { get; set; }
        public int CompletedCount { get; set; }
        public string AverageScore { get; set; } = "0";
        public double CompletionRate { get; set; }
        public string CompletionRatePercent { get; set; } = "0%";
    }
}