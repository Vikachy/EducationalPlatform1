using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class AdminLogsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        private CollectionView? _logsCollectionView;
        private Label? _todayLogsLabel;
        private Label? _weekLogsLabel;
        private Label? _errorLogsLabel;

        private ObservableCollection<LogItem> _allLogs = new();
        private ObservableCollection<LogItem> _filteredLogs = new();

        public ObservableCollection<LogItem> Logs
        {
            get => _filteredLogs;
            set
            {
                _filteredLogs = value;
                OnPropertyChanged();
            }
        }

        private string _currentFilter = "all";

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminLogsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminLogsPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            InitializeControls();
            BindingContext = this;

            Task.Run(async () => await LoadLogsAsync());
        }

        private void InitializeControls()
        {
            _logsCollectionView = this.FindByName<CollectionView>("LogsCollectionView");
            _todayLogsLabel = this.FindByName<Label>("TodayLogsLabel");
            _weekLogsLabel = this.FindByName<Label>("WeekLogsLabel");
            _errorLogsLabel = this.FindByName<Label>("ErrorLogsLabel");

            if (_logsCollectionView != null)
                _logsCollectionView.ItemsSource = Logs;
        }

        private async Task LoadLogsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                // ИСПРАВЛЕННЫЙ SQL запрос - убрал алиас "User"
                var logs = await connection.QueryAsync<LogItem>(@"
                    SELECT 
                        a.ActionId,
                        a.UserId,
                        u.Username as UserName,
                        a.ActionType,
                        a.Details as Description,
                        a.ActionDate as Timestamp,
                        CASE 
                            WHEN a.ActionType LIKE '%login%' OR a.ActionType LIKE '%logout%' THEN '🔐'
                            WHEN a.ActionType LIKE '%user%' OR a.ActionType LIKE '%register%' THEN '👤'
                            WHEN a.ActionType LIKE '%course%' THEN '📚'
                            WHEN a.ActionType LIKE '%group%' THEN '👥'
                            WHEN a.ActionType LIKE '%role%' THEN '👑'
                            WHEN a.ActionType LIKE '%setting%' OR a.ActionType LIKE '%config%' THEN '⚙️'
                            WHEN a.ActionType LIKE '%error%' OR a.ActionType LIKE '%exception%' THEN '❌'
                            ELSE '📝'
                        END as Icon,
                        CASE 
                            WHEN a.ActionType LIKE '%error%' OR a.ActionType LIKE '%exception%' THEN '#F44336'
                            WHEN a.ActionType LIKE '%login%' THEN '#4CAF50'
                            WHEN a.ActionType LIKE '%user%' OR a.ActionType LIKE '%register%' THEN '#2196F3'
                            WHEN a.ActionType LIKE '%course%' THEN '#FF9800'
                            WHEN a.ActionType LIKE '%group%' THEN '#9C27B0'
                            WHEN a.ActionType LIKE '%role%' THEN '#9C27B0'
                            WHEN a.ActionType LIKE '%setting%' THEN '#607D8B'
                            ELSE '#607D8B'
                        END as Color,
                        CASE 
                            WHEN a.ActionType LIKE '%login%' THEN 'Вход в систему'
                            WHEN a.ActionType LIKE '%logout%' THEN 'Выход из системы'
                            WHEN a.ActionType LIKE '%create_user%' THEN 'Создание пользователя'
                            WHEN a.ActionType LIKE '%edit_user%' THEN 'Редактирование пользователя'
                            WHEN a.ActionType LIKE '%delete_user%' THEN 'Удаление пользователя'
                            WHEN a.ActionType LIKE '%create_course%' THEN 'Создание курса'
                            WHEN a.ActionType LIKE '%edit_course%' THEN 'Редактирование курса'
                            WHEN a.ActionType LIKE '%delete_course%' THEN 'Удаление курса'
                            WHEN a.ActionType LIKE '%create_group%' THEN 'Создание группы'
                            WHEN a.ActionType LIKE '%edit_group%' THEN 'Редактирование группы'
                            WHEN a.ActionType LIKE '%delete_group%' THEN 'Удаление группы'
                            WHEN a.ActionType LIKE '%error%' THEN 'Ошибка системы'
                            ELSE a.Details
                        END as Action
                    FROM AdminActions a
                    JOIN Users u ON a.UserId = u.UserId
                    ORDER BY a.ActionDate DESC
                ");

                // Подсчет статистики
                var today = DateTime.Today;
                var weekAgo = DateTime.Today.AddDays(-7);

                var todayCount = logs.Count(l => l.Timestamp.Date == today);
                var weekCount = logs.Count(l => l.Timestamp.Date >= weekAgo);
                var errorCount = logs.Count(l => l.ActionType?.Contains("error") == true ||
                                                  l.ActionType?.Contains("exception") == true);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _allLogs.Clear();
                    foreach (var log in logs)
                    {
                        log.TimeFormatted = GetRelativeTime(log.Timestamp);
                        log.User = log.UserName; // Используем UserName из запроса
                        _allLogs.Add(log);
                    }

                    if (_todayLogsLabel != null) _todayLogsLabel.Text = todayCount.ToString();
                    if (_weekLogsLabel != null) _weekLogsLabel.Text = weekCount.ToString();
                    if (_errorLogsLabel != null) _errorLogsLabel.Text = errorCount.ToString();

                    ApplyFilter();
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить логи: {ex.Message}", "OK");
                });
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

        private void ApplyFilter()
        {
            switch (_currentFilter)
            {
                case "logins":
                    Logs = new ObservableCollection<LogItem>(
                        _allLogs.Where(l => l.ActionType?.Contains("login") == true));
                    break;
                case "users":
                    Logs = new ObservableCollection<LogItem>(
                        _allLogs.Where(l => l.ActionType?.Contains("user") == true));
                    break;
                case "courses":
                    Logs = new ObservableCollection<LogItem>(
                        _allLogs.Where(l => l.ActionType?.Contains("course") == true));
                    break;
                case "errors":
                    Logs = new ObservableCollection<LogItem>(
                        _allLogs.Where(l => l.ActionType?.Contains("error") == true ||
                                            l.ActionType?.Contains("exception") == true));
                    break;
                default:
                    Logs = new ObservableCollection<LogItem>(_allLogs);
                    break;
            }
        }

        private void OnFilterAllClicked(object sender, EventArgs e)
        {
            _currentFilter = "all";
            ApplyFilter();
        }

        private void OnFilterLoginsClicked(object sender, EventArgs e)
        {
            _currentFilter = "logins";
            ApplyFilter();
        }

        private void OnFilterUsersClicked(object sender, EventArgs e)
        {
            _currentFilter = "users";
            ApplyFilter();
        }

        private void OnFilterCoursesClicked(object sender, EventArgs e)
        {
            _currentFilter = "courses";
            ApplyFilter();
        }

        private void OnFilterErrorsClicked(object sender, EventArgs e)
        {
            _currentFilter = "errors";
            ApplyFilter();
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadLogsAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class LogItem
    {
        public int ActionId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty; // Для отображения
        public string ActionType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TimeFormatted { get; set; } = string.Empty;
        public string Icon { get; set; } = "📝";
        public string Color { get; set; } = "#607D8B";
    }
}