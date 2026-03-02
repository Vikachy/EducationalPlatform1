using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class AdminCoursesPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        private Entry? _searchEntry;
        private Picker? _statusFilter;
        private CollectionView? _coursesCollectionView;

        private ObservableCollection<CourseModel> _allCourses = new();
        private ObservableCollection<CourseModel> _filteredCourses = new();

        public ObservableCollection<CourseModel> FilteredCourses
        {
            get => _filteredCourses;
            set
            {
                _filteredCourses = value;
                OnPropertyChanged();
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminCoursesPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminCoursesPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            InitializeControls();
            BindingContext = this;

            Task.Run(async () => await LoadCoursesAsync());
        }

        private void InitializeControls()
        {
            _searchEntry = this.FindByName<Entry>("SearchEntry");
            _statusFilter = this.FindByName<Picker>("StatusFilter");
            _coursesCollectionView = this.FindByName<CollectionView>("CoursesCollectionView");

            if (_statusFilter != null)
                _statusFilter.SelectedIndex = 0;

            if (_coursesCollectionView != null)
                _coursesCollectionView.ItemsSource = FilteredCourses;
        }

        private async Task LoadCoursesAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var courses = await connection.QueryAsync<CourseModel>(@"
                    SELECT 
                        c.CourseId,
                        c.CourseName,
                        c.Description,
                        ISNULL(l.LanguageName, 'Не указан') as LanguageName,
                        ISNULL(d.DifficultyName, 'Не указана') as DifficultyName,
                        c.IsPublished,
                        c.CreatedDate,
                        u.FirstName + ' ' + u.LastName as TeacherName,
                        (SELECT COUNT(*) FROM Lessons l2 
                         JOIN CourseModules m ON l2.ModuleId = m.ModuleId 
                         WHERE m.CourseId = c.CourseId) as LessonCount,
                        (SELECT COUNT(*) FROM StudentProgress WHERE CourseId = c.CourseId) as StudentCount
                    FROM Courses c
                    LEFT JOIN ProgrammingLanguages l ON c.LanguageId = l.LanguageId
                    LEFT JOIN Difficulties d ON c.DifficultyId = d.DifficultyId
                    LEFT JOIN Users u ON c.CreatedByUserId = u.UserId
                    ORDER BY c.CreatedDate DESC
                ");

                var courseList = courses.ToList();
                Console.WriteLine($"📊 Загружено курсов: {courseList.Count}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _allCourses.Clear();
                    foreach (var course in courseList)
                    {
                        _allCourses.Add(course);
                    }

                    ApplyFilter();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки курсов: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить курсы: {ex.Message}", "OK");
                });
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allCourses.AsEnumerable();

            // Поиск по названию
            if (_searchEntry != null && !string.IsNullOrWhiteSpace(_searchEntry.Text))
            {
                var searchText = _searchEntry.Text.ToLower();
                filtered = filtered.Where(c =>
                    c.CourseName.ToLower().Contains(searchText) ||
                    (c.Description?.ToLower().Contains(searchText) == true) ||
                    c.LanguageName.ToLower().Contains(searchText) ||
                    c.DifficultyName.ToLower().Contains(searchText));
            }

            // Фильтр по статусу
            if (_statusFilter != null && _statusFilter.SelectedIndex > 0)
            {
                bool isPublished = _statusFilter.SelectedIndex == 1;
                filtered = filtered.Where(c => c.IsPublished == isPublished);
            }

            FilteredCourses = new ObservableCollection<CourseModel>(filtered);

            if (_coursesCollectionView != null)
            {
                _coursesCollectionView.ItemsSource = null;
                _coursesCollectionView.ItemsSource = FilteredCourses;
            }

            Console.WriteLine($"📊 Отфильтровано курсов: {FilteredCourses.Count}");
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private async void OnViewLessonsClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is CourseModel course)
            {
                // Переход на страницу уроков курса
                await Navigation.PushAsync(new CourseLessonsPage(_currentUser, _dbService, _settingsService, course.CourseId, course.CourseName));
            }
        }

        private async void OnCourseStatsClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is CourseModel course)
            {
                await DisplayAlert("Статистика курса",
                    $"📊 Статистика курса '{course.CourseName}'\n\n" +
                    $"👥 Студентов: {course.StudentCount}\n" +
                    $"📚 Уроков: {course.LessonCount}\n" +
                    $"📅 Создан: {course.CreatedDate:dd.MM.yyyy}\n" +
                    $"👨‍🏫 Преподаватель: {course.TeacherName}",
                    "OK");
            }
        }

        private async void OnEditCourseClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is CourseModel course)
            {
                await Navigation.PushAsync(new CreateCoursePage(_currentUser, _dbService, _settingsService, course.CourseId));
            }
        }

        private async void OnDeleteCourseClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is CourseModel course)
            {
                bool confirm = await DisplayAlert("Подтверждение",
                    $"Вы уверены, что хотите удалить курс '{course.CourseName}'?\nЭто действие необратимо.",
                    "Да", "Нет");

                if (confirm)
                {
                    try
                    {
                        using var connection = new SqlConnection(_dbService.ConnectionString);
                        await connection.OpenAsync();

                        await connection.ExecuteAsync("DELETE FROM Courses WHERE CourseId = @CourseId",
                            new { CourseId = course.CourseId });

                        _allCourses.Remove(course);
                        ApplyFilter();

                        await DisplayAlert("Успех", "Курс удален", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", ex.Message, "OK");
                    }
                }
            }
        }

        private async void OnAddCourseClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateCoursePage(_currentUser, _dbService, _settingsService));
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class CourseModel
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string DifficultyName { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public int StudentCount { get; set; }
        public int LessonCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public string TeacherName { get; set; } = string.Empty;
    }
}