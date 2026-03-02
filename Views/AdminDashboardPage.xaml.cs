using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class AdminDashboardPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        private Label? _welcomeLabel;
        private Label? _lastLoginLabel;
        private Label? _usersCountLabel;
        private Label? _newUsersTodayLabel;
        private Label? _coursesCountLabel;
        private Label? _activeCoursesLabel;
        private Label? _groupsCountLabel;
        private Label? _activeGroupsLabel;
        private Label? _teachersCountLabel;
        private Label? _studentsCountLabel;
        private Label? _adminsCountLabel;
        private CollectionView? _recentActivitiesCollection;

        public ObservableCollection<ActivityItem> RecentActivities { get; set; } = new();

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminDashboardPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminDashboardPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            InitializeControls();
            BindingContext = this;

            Task.Run(async () => await LoadStatisticsAsync());
            Task.Run(async () => await LoadRecentActivitiesAsync());
        }

        private void InitializeControls()
        {
            _welcomeLabel = this.FindByName<Label>("WelcomeLabel");
            _lastLoginLabel = this.FindByName<Label>("LastLoginLabel");
            _usersCountLabel = this.FindByName<Label>("UsersCountLabel");
            _newUsersTodayLabel = this.FindByName<Label>("NewUsersTodayLabel");
            _coursesCountLabel = this.FindByName<Label>("CoursesCountLabel");
            _activeCoursesLabel = this.FindByName<Label>("ActiveCoursesLabel");
            _groupsCountLabel = this.FindByName<Label>("GroupsCountLabel");
            _activeGroupsLabel = this.FindByName<Label>("ActiveGroupsLabel");
            _teachersCountLabel = this.FindByName<Label>("TeachersCountLabel");
            _studentsCountLabel = this.FindByName<Label>("StudentsCountLabel");
            _adminsCountLabel = this.FindByName<Label>("AdminsCountLabel");
            _recentActivitiesCollection = this.FindByName<CollectionView>("RecentActivitiesCollection");

            if (_recentActivitiesCollection != null)
                _recentActivitiesCollection.ItemsSource = RecentActivities;

            if (_welcomeLabel != null)
                _welcomeLabel.Text = $"Добро пожаловать, {_currentUser.FirstName}!";
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var stats = await connection.QueryFirstOrDefaultAsync<AdminStats>(@"
                    SELECT 
                        (SELECT COUNT(*) FROM Users) as TotalUsers,
                        (SELECT COUNT(*) FROM Users WHERE CAST(RegistrationDate AS DATE) = CAST(GETDATE() AS DATE)) as NewUsersToday,
                        (SELECT COUNT(*) FROM Courses) as TotalCourses,
                        (SELECT COUNT(*) FROM Courses WHERE IsPublished = 1) as ActiveCourses,
                        (SELECT COUNT(*) FROM StudyGroups) as TotalGroups,
                        (SELECT COUNT(*) FROM StudyGroups WHERE IsActive = 1) as ActiveGroups,
                        (SELECT COUNT(*) FROM Users WHERE RoleId = 2) as Teachers,
                        (SELECT COUNT(*) FROM Users WHERE RoleId = 1) as Students,
                        (SELECT COUNT(*) FROM Users WHERE RoleId = 3) as Admins
                ");

                var lastLogin = await connection.QueryFirstOrDefaultAsync<DateTime?>(@"
                    SELECT MAX(ActionDate) FROM AdminActions WHERE UserId = @UserId AND ActionType = 'login'
                ", new { UserId = _currentUser.UserId });

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_usersCountLabel != null) _usersCountLabel.Text = stats?.TotalUsers.ToString() ?? "0";
                    if (_newUsersTodayLabel != null) _newUsersTodayLabel.Text = $"+{stats?.NewUsersToday ?? 0} сегодня";
                    if (_coursesCountLabel != null) _coursesCountLabel.Text = stats?.TotalCourses.ToString() ?? "0";
                    if (_activeCoursesLabel != null) _activeCoursesLabel.Text = $"{stats?.ActiveCourses ?? 0} активных";
                    if (_groupsCountLabel != null) _groupsCountLabel.Text = stats?.TotalGroups.ToString() ?? "0";
                    if (_activeGroupsLabel != null) _activeGroupsLabel.Text = $"{stats?.ActiveGroups ?? 0} активных";
                    if (_teachersCountLabel != null) _teachersCountLabel.Text = stats?.Teachers.ToString() ?? "0";
                    if (_studentsCountLabel != null) _studentsCountLabel.Text = stats?.Students.ToString() ?? "0";
                    if (_adminsCountLabel != null) _adminsCountLabel.Text = stats?.Admins.ToString() ?? "0";

                    if (_lastLoginLabel != null)
                    {
                        if (lastLogin.HasValue)
                            _lastLoginLabel.Text = $"Последний вход: {GetRelativeTime(lastLogin.Value)}";
                        else
                            _lastLoginLabel.Text = "Последний вход: сегодня";
                    }
                });

                await AddActivityAsync("Просмотр статистики", "view");
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить статистику: {ex.Message}", "OK");
                });
            }
        }

        private async Task LoadRecentActivitiesAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var activities = await connection.QueryAsync<ActivityItem>(@"
                    SELECT TOP 20
                        u.Username as User,
                        a.ActionType,
                        a.Details as Description,
                        a.ActionDate as Timestamp,
                        CASE 
                            WHEN a.ActionType LIKE '%login%' THEN '🔐'
                            WHEN a.ActionType LIKE '%user%' THEN '👤'
                            WHEN a.ActionType LIKE '%course%' THEN '📚'
                            WHEN a.ActionType LIKE '%group%' THEN '👥'
                            WHEN a.ActionType LIKE '%role%' THEN '👑'
                            WHEN a.ActionType LIKE '%setting%' THEN '⚙️'
                            ELSE '📝'
                        END as Icon,
                        CASE 
                            WHEN a.ActionType LIKE '%error%' THEN '#F44336'
                            WHEN a.ActionType LIKE '%login%' THEN '#4CAF50'
                            WHEN a.ActionType LIKE '%user%' THEN '#2196F3'
                            WHEN a.ActionType LIKE '%course%' THEN '#FF9800'
                            ELSE '#607D8B'
                        END as Color
                    FROM AdminActions a
                    JOIN Users u ON a.UserId = u.UserId
                    ORDER BY a.ActionDate DESC
                ");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    RecentActivities.Clear();
                    foreach (var activity in activities)
                    {
                        activity.Time = GetRelativeTime(activity.Timestamp);
                        RecentActivities.Add(activity);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки активности: {ex.Message}");
            }
        }

        private string GetRelativeTime(DateTime timestamp)
        {
            var diff = DateTime.Now - timestamp;
            if (diff.TotalMinutes < 1) return "только что";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} мин назад";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} ч назад";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} дн назад";
            return timestamp.ToString("dd.MM.yyyy HH:mm");
        }

        private async Task AddActivityAsync(string action, string type)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                await connection.ExecuteAsync(@"
                    INSERT INTO AdminActions (UserId, ActionType, ActionDate, Details)
                    VALUES (@UserId, @ActionType, GETDATE(), @Details)
                ", new
                {
                    UserId = _currentUser.UserId,
                    ActionType = type,
                    Details = action
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка логирования: {ex.Message}");
            }
        }

        // Обработчики событий
        private async void OnRefreshActivitiesClicked(object sender, EventArgs e)
        {
            await LoadRecentActivitiesAsync();
            await AddActivityAsync("Обновление лога активности", "refresh");
        }

        private async void OnManageUsersClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminUsersPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыто управление пользователями", "navigation");
        }

        private async void OnAddUserClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminUserEditPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыта страница создания пользователя", "navigation");
        }

        private async void OnManageRolesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminRolesPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыто управление ролями", "navigation");
        }

        private async void OnAddRoleClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminRoleEditPage(_currentUser, _dbService, _settingsService, null));
            await AddActivityAsync("Открыта страница создания роли", "navigation");
        }

        private async void OnManageCoursesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminCoursesPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыто управление курсами", "navigation");
        }

        private async void OnAddCourseClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateCoursePage(_currentUser, _dbService, _settingsService, 0));
            await AddActivityAsync("Открыта страница создания курса", "navigation");
        }

        private async void OnManageGroupsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminGroupsPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыто управление группами", "navigation");
        }

        private async void OnAddGroupClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TeacherGroupsManagementPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыта страница создания группы", "navigation");
        }

        private async void OnManageLanguagesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminLanguagesPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыто управление языками", "navigation");
        }

        private async void OnAddLanguageClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminLanguageEditPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыта страница создания языка", "navigation");
        }

        private async void OnManageDifficultiesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminDifficultiesPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыто управление уровнями сложности", "navigation");
        }

        private async void OnAddDifficultyClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminDifficultyEditPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыта страница создания уровня сложности", "navigation");
        }

        private async void OnContentManagerClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ContentManagerPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыт контент-менеджер", "navigation");
        }

        private async void OnManageNewsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new NewsPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыто управление новостями", "navigation");
        }

        private async void OnAddNewsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateNewsPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыта страница создания новости", "navigation");
        }

        private async void OnSystemSettingsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminSettingsPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Открыты системные настройки", "navigation");
        }

        private async void OnViewLogsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminLogsPage(_currentUser, _dbService, _settingsService));
            await AddActivityAsync("Просмотр системных логов", "view");
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Подтверждение", "Вы уверены, что хотите выйти?", "Да", "Нет");
            if (confirm)
            {
                await AddActivityAsync("Выход из системы", "logout");
                Application.Current!.MainPage = new NavigationPage(new MainPage());
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class AdminStats
    {
        public int TotalUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int TotalCourses { get; set; }
        public int ActiveCourses { get; set; }
        public int TotalGroups { get; set; }
        public int ActiveGroups { get; set; }
        public int Teachers { get; set; }
        public int Students { get; set; }
        public int Admins { get; set; }
    }

    public class ActivityItem
    {
        public string Icon { get; set; } = "📝";
        public string User { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Time { get; set; } = string.Empty;
        public string Color { get; set; } = "#607D8B";
    }
}