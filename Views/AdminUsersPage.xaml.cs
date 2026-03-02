using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class AdminUsersPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        private Entry? _searchEntry;
        private Picker? _roleFilter;
        private Picker? _statusFilter;
        private CollectionView? _usersCollectionView;
        private Label? _totalUsersLabel;
        private Label? _activeUsersLabel;
        private Label? _newUsersLabel;

        private ObservableCollection<AdminUserModel> _allUsers = new();
        private ObservableCollection<AdminUserModel> _filteredUsers = new();

        public ObservableCollection<AdminUserModel> Users
        {
            get => _filteredUsers;
            set
            {
                _filteredUsers = value;
                OnPropertyChanged();
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminUsersPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminUsersPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            InitializeControls();
            BindingContext = this;

            Task.Run(async () => await LoadUsersAsync());
        }

        private void InitializeControls()
        {
            _searchEntry = this.FindByName<Entry>("SearchEntry");
            _roleFilter = this.FindByName<Picker>("RoleFilter");
            _statusFilter = this.FindByName<Picker>("StatusFilter");
            _usersCollectionView = this.FindByName<CollectionView>("UsersCollectionView");
            _totalUsersLabel = this.FindByName<Label>("TotalUsersLabel");
            _activeUsersLabel = this.FindByName<Label>("ActiveUsersLabel");
            _newUsersLabel = this.FindByName<Label>("NewUsersLabel");

            if (_usersCollectionView != null)
                _usersCollectionView.ItemsSource = Users;
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var users = await connection.QueryAsync<User>(@"
                    SELECT UserId, Username, Email, FirstName, LastName, 
                           RoleId, AvatarUrl, IsActive, RegistrationDate
                    FROM Users
                    ORDER BY RegistrationDate DESC
                ");

                var userList = users.ToList();

                Console.WriteLine($"📊 Загружено пользователей: {userList.Count}");

                // Статистика
                var totalUsers = userList.Count;
                var activeUsers = userList.Count(u => u.IsActive);
                var newUsers = userList.Count(u => u.RegistrationDate.Date == DateTime.Today);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_totalUsersLabel != null) _totalUsersLabel.Text = totalUsers.ToString();
                    if (_activeUsersLabel != null) _activeUsersLabel.Text = activeUsers.ToString();
                    if (_newUsersLabel != null) _newUsersLabel.Text = newUsers.ToString();

                    _allUsers.Clear();
                    foreach (var user in userList)
                    {
                        _allUsers.Add(new AdminUserModel
                        {
                            UserId = user.UserId,
                            Username = user.Username ?? "",
                            Email = user.Email ?? "",
                            FirstName = user.FirstName ?? "",
                            LastName = user.LastName ?? "",
                            FullName = $"{user.FirstName} {user.LastName}".Trim(),
                            RoleId = user.RoleId,
                            IsActive = user.IsActive,
                            AvatarUrl = user.AvatarUrl,
                            RegistrationDate = user.RegistrationDate
                        });
                    }

                    ApplyFilter();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки пользователей: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить пользователей: {ex.Message}", "OK");
                });
            }
        }

        // ДОБАВЛЯЕМ МЕТОД ApplyFilter
        private void ApplyFilter()
        {
            var filtered = _allUsers.AsEnumerable();

            // Поиск по тексту
            if (_searchEntry != null && !string.IsNullOrWhiteSpace(_searchEntry.Text))
            {
                var searchText = _searchEntry.Text.ToLower();
                filtered = filtered.Where(u =>
                    (u.Username?.ToLower().Contains(searchText) ?? false) ||
                    (u.Email?.ToLower().Contains(searchText) ?? false) ||
                    (u.FirstName?.ToLower().Contains(searchText) ?? false) ||
                    (u.LastName?.ToLower().Contains(searchText) ?? false) ||
                    (u.FullName?.ToLower().Contains(searchText) ?? false));
            }

            // Фильтр по роли
            if (_roleFilter != null && _roleFilter.SelectedIndex > 0)
            {
                int roleId = _roleFilter.SelectedIndex switch
                {
                    1 => 1, // Студенты
                    2 => 2, // Преподаватели
                    3 => 3, // Администраторы
                    4 => 4, // Контент-менеджеры
                    _ => 0
                };

                if (roleId > 0)
                    filtered = filtered.Where(u => u.RoleId == roleId);
            }

            // Фильтр по статусу
            if (_statusFilter != null && _statusFilter.SelectedIndex > 0)
            {
                bool isActive = _statusFilter.SelectedIndex == 1;
                filtered = filtered.Where(u => u.IsActive == isActive);
            }

            Users = new ObservableCollection<AdminUserModel>(filtered);

            // Обновляем CollectionView
            if (_usersCollectionView != null)
            {
                _usersCollectionView.ItemsSource = null;
                _usersCollectionView.ItemsSource = Users;
            }

            Console.WriteLine($"📊 Отфильтровано пользователей: {Users.Count}");
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void OnResetFiltersClicked(object sender, EventArgs e)
        {
            if (_searchEntry != null) _searchEntry.Text = "";
            if (_roleFilter != null) _roleFilter.SelectedIndex = 0;
            if (_statusFilter != null) _statusFilter.SelectedIndex = 0;
            ApplyFilter();
        }

        private async void OnAddUserClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminUserEditPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnEditUserClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is AdminUserModel user)
            {
                await Navigation.PushAsync(new AdminUserEditPage(_currentUser, _dbService, _settingsService, user));
                await LogAdminActionAsync($"Редактирование пользователя {user.Email}");
            }
        }

        private async void OnToggleStatusClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is AdminUserModel user)
            {
                string action = user.IsActive ? "деактивировать" : "активировать";
                bool confirm = await DisplayAlert("Подтверждение",
                    $"Вы уверены, что хотите {action} пользователя {user.Email}?",
                    "Да", "Нет");

                if (confirm)
                {
                    try
                    {
                        using var connection = new SqlConnection(_dbService.ConnectionString);
                        await connection.OpenAsync();

                        if (user.IsActive)
                        {
                            await connection.ExecuteAsync(
                                "UPDATE Users SET IsActive = 0 WHERE UserId = @UserId",
                                new { UserId = user.UserId });
                        }
                        else
                        {
                            await connection.ExecuteAsync(
                                "UPDATE Users SET IsActive = 1 WHERE UserId = @UserId",
                                new { UserId = user.UserId });
                        }

                        user.IsActive = !user.IsActive;
                        ApplyFilter();

                        await DisplayAlert("Успех", $"Пользователь {action}н", "OK");
                        await LogAdminActionAsync($"Статус пользователя {user.Email} изменен на {!user.IsActive}");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", ex.Message, "OK");
                    }
                }
            }
        }

        private async void OnResetPasswordClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is AdminUserModel user)
            {
                bool confirm = await DisplayAlert("Подтверждение",
                    $"Сбросить пароль пользователя {user.Email}?\nНовый пароль будет показан на экране.",
                    "Да", "Нет");

                if (confirm)
                {
                    try
                    {
                        string newPassword = GenerateRandomPassword();

                        using var connection = new SqlConnection(_dbService.ConnectionString);
                        await connection.OpenAsync();

                        await connection.ExecuteAsync(
                            "UPDATE Users SET PasswordHash = @Password WHERE UserId = @UserId",
                            new
                            {
                                UserId = user.UserId,
                                Password = HashPassword(newPassword)
                            });

                        await DisplayAlert("Новый пароль",
                            $"Пароль для {user.Email}: {newPassword}\n\nСохраните его в безопасном месте!",
                            "OK");

                        await LogAdminActionAsync($"Сброс пароля для {user.Email}");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", ex.Message, "OK");
                    }
                }
            }
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private async Task LogAdminActionAsync(string action)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                await connection.ExecuteAsync(@"
                    INSERT INTO AdminActions (UserId, ActionType, ActionDate, Details)
                    VALUES (@UserId, 'user_management', GETDATE(), @Details)
                ", new
                {
                    UserId = _currentUser.UserId,
                    Details = action
                });
            }
            catch { }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class AdminUserModel : INotifyPropertyChanged
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int RoleId { get; set; }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public string? AvatarUrl { get; set; }
        public DateTime RegistrationDate { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}