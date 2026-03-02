using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Data.SqlClient;
using Dapper;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class AdminSettingsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        // Элементы управления
        private Entry? _platformNameEntry;
        private Entry? _supportEmailEntry;
        private Switch? _allowRegistrationSwitch;
        private Picker? _defaultThemePicker;
        private Entry? _startBalanceEntry;
        private Entry? _lessonRewardEntry;
        private Entry? _testRewardEntry;
        private Entry? _maxDailyCurrencyEntry;
        private Entry? _minPasswordEntry;
        private Switch? _require2FASwitch;
        private Entry? _sessionTimeoutEntry;
        private Entry? _lockoutAttemptsEntry;
        private Switch? _emailNotificationsSwitch;
        private Switch? _pushNotificationsSwitch;
        private Switch? _smsNotificationsSwitch;

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminSettingsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminSettingsPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            InitializeControls();
            LoadSettings();
        }

        private void InitializeControls()
        {
            _platformNameEntry = this.FindByName<Entry>("PlatformNameEntry");
            _supportEmailEntry = this.FindByName<Entry>("SupportEmailEntry");
            _allowRegistrationSwitch = this.FindByName<Switch>("AllowRegistrationSwitch");
            _defaultThemePicker = this.FindByName<Picker>("DefaultThemePicker");
            _startBalanceEntry = this.FindByName<Entry>("StartBalanceEntry");
            _lessonRewardEntry = this.FindByName<Entry>("LessonRewardEntry");
            _testRewardEntry = this.FindByName<Entry>("TestRewardEntry");
            _maxDailyCurrencyEntry = this.FindByName<Entry>("MaxDailyCurrencyEntry");
            _minPasswordEntry = this.FindByName<Entry>("MinPasswordEntry");
            _require2FASwitch = this.FindByName<Switch>("Require2FASwitch");
            _sessionTimeoutEntry = this.FindByName<Entry>("SessionTimeoutEntry");
            _lockoutAttemptsEntry = this.FindByName<Entry>("LockoutAttemptsEntry");
            _emailNotificationsSwitch = this.FindByName<Switch>("EmailNotificationsSwitch");
            _pushNotificationsSwitch = this.FindByName<Switch>("PushNotificationsSwitch");
            _smsNotificationsSwitch = this.FindByName<Switch>("SmsNotificationsSwitch");

            if (_defaultThemePicker != null)
                _defaultThemePicker.SelectedIndex = 0;
        }

        private async void LoadSettings()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                // Проверяем существование таблицы SystemSettings
                var tableExists = await connection.QueryFirstOrDefaultAsync<int?>(@"
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = 'SystemSettings'
                ");

                if (tableExists == null)
                {
                    // Создаем таблицу если её нет
                    await connection.ExecuteAsync(@"
                        CREATE TABLE SystemSettings (
                            Id INT PRIMARY KEY,
                            PlatformName NVARCHAR(100) NOT NULL,
                            SupportEmail NVARCHAR(100) NOT NULL,
                            AllowRegistration BIT NOT NULL DEFAULT 1,
                            DefaultTheme NVARCHAR(50) DEFAULT 'Светлая',
                            StartBalance INT NOT NULL DEFAULT 100,
                            LessonReward INT NOT NULL DEFAULT 50,
                            TestReward INT NOT NULL DEFAULT 75,
                            MaxDailyCurrency INT NOT NULL DEFAULT 500,
                            MinPasswordLength INT NOT NULL DEFAULT 6,
                            Require2FA BIT NOT NULL DEFAULT 0,
                            SessionTimeout INT NOT NULL DEFAULT 24,
                            LockoutAttempts INT NOT NULL DEFAULT 5,
                            EmailNotifications BIT NOT NULL DEFAULT 1,
                            PushNotifications BIT NOT NULL DEFAULT 1,
                            SmsNotifications BIT NOT NULL DEFAULT 0,
                            CreatedDate DATETIME2 DEFAULT GETDATE(),
                            UpdatedDate DATETIME2 DEFAULT GETDATE()
                        )
                    ");

                    // Вставляем начальные настройки
                    await connection.ExecuteAsync(@"
                        INSERT INTO SystemSettings (
                            Id, PlatformName, SupportEmail, AllowRegistration, DefaultTheme,
                            StartBalance, LessonReward, TestReward, MaxDailyCurrency,
                            MinPasswordLength, Require2FA, SessionTimeout, LockoutAttempts,
                            EmailNotifications, PushNotifications, SmsNotifications
                        ) VALUES (
                            1, 'Educational Platform', 'support@eduplatform.com', 1, 'Светлая',
                            100, 50, 75, 500,
                            6, 0, 24, 5,
                            1, 1, 0
                        )
                    ");
                }

                var settings = await connection.QueryFirstOrDefaultAsync<SystemSettings>(@"
                    SELECT * FROM SystemSettings WHERE Id = 1
                ");

                if (settings != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_platformNameEntry != null) _platformNameEntry.Text = settings.PlatformName;
                        if (_supportEmailEntry != null) _supportEmailEntry.Text = settings.SupportEmail;
                        if (_allowRegistrationSwitch != null) _allowRegistrationSwitch.IsToggled = settings.AllowRegistration;

                        if (_defaultThemePicker != null && !string.IsNullOrEmpty(settings.DefaultTheme))
                        {
                            var index = _defaultThemePicker.Items.IndexOf(settings.DefaultTheme);
                            if (index >= 0) _defaultThemePicker.SelectedIndex = index;
                        }

                        if (_startBalanceEntry != null) _startBalanceEntry.Text = settings.StartBalance.ToString();
                        if (_lessonRewardEntry != null) _lessonRewardEntry.Text = settings.LessonReward.ToString();
                        if (_testRewardEntry != null) _testRewardEntry.Text = settings.TestReward.ToString();
                        if (_maxDailyCurrencyEntry != null) _maxDailyCurrencyEntry.Text = settings.MaxDailyCurrency.ToString();
                        if (_minPasswordEntry != null) _minPasswordEntry.Text = settings.MinPasswordLength.ToString();
                        if (_require2FASwitch != null) _require2FASwitch.IsToggled = settings.Require2FA;
                        if (_sessionTimeoutEntry != null) _sessionTimeoutEntry.Text = settings.SessionTimeout.ToString();
                        if (_lockoutAttemptsEntry != null) _lockoutAttemptsEntry.Text = settings.LockoutAttempts.ToString();
                        if (_emailNotificationsSwitch != null) _emailNotificationsSwitch.IsToggled = settings.EmailNotifications;
                        if (_pushNotificationsSwitch != null) _pushNotificationsSwitch.IsToggled = settings.PushNotifications;
                        if (_smsNotificationsSwitch != null) _smsNotificationsSwitch.IsToggled = settings.SmsNotifications;
                    });
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить настройки: {ex.Message}", "OK");
                });
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var settings = new
                {
                    PlatformName = _platformNameEntry?.Text ?? "Educational Platform",
                    SupportEmail = _supportEmailEntry?.Text ?? "support@eduplatform.com",
                    AllowRegistration = _allowRegistrationSwitch?.IsToggled ?? true,
                    DefaultTheme = _defaultThemePicker?.SelectedItem?.ToString() ?? "Светлая",
                    StartBalance = int.TryParse(_startBalanceEntry?.Text, out var sb) ? sb : 100,
                    LessonReward = int.TryParse(_lessonRewardEntry?.Text, out var lr) ? lr : 50,
                    TestReward = int.TryParse(_testRewardEntry?.Text, out var tr) ? tr : 75,
                    MaxDailyCurrency = int.TryParse(_maxDailyCurrencyEntry?.Text, out var md) ? md : 500,
                    MinPasswordLength = int.TryParse(_minPasswordEntry?.Text, out var mp) ? mp : 6,
                    Require2FA = _require2FASwitch?.IsToggled ?? false,
                    SessionTimeout = int.TryParse(_sessionTimeoutEntry?.Text, out var st) ? st : 24,
                    LockoutAttempts = int.TryParse(_lockoutAttemptsEntry?.Text, out var la) ? la : 5,
                    EmailNotifications = _emailNotificationsSwitch?.IsToggled ?? true,
                    PushNotifications = _pushNotificationsSwitch?.IsToggled ?? true,
                    SmsNotifications = _smsNotificationsSwitch?.IsToggled ?? false
                };

                await connection.ExecuteAsync(@"
                    IF EXISTS (SELECT 1 FROM SystemSettings WHERE Id = 1)
                        UPDATE SystemSettings SET 
                            PlatformName = @PlatformName,
                            SupportEmail = @SupportEmail,
                            AllowRegistration = @AllowRegistration,
                            DefaultTheme = @DefaultTheme,
                            StartBalance = @StartBalance,
                            LessonReward = @LessonReward,
                            TestReward = @TestReward,
                            MaxDailyCurrency = @MaxDailyCurrency,
                            MinPasswordLength = @MinPasswordLength,
                            Require2FA = @Require2FA,
                            SessionTimeout = @SessionTimeout,
                            LockoutAttempts = @LockoutAttempts,
                            EmailNotifications = @EmailNotifications,
                            PushNotifications = @PushNotifications,
                            SmsNotifications = @SmsNotifications,
                            UpdatedDate = GETDATE()
                        WHERE Id = 1
                    ELSE
                        INSERT INTO SystemSettings (
                            Id, PlatformName, SupportEmail, AllowRegistration, DefaultTheme,
                            StartBalance, LessonReward, TestReward, MaxDailyCurrency,
                            MinPasswordLength, Require2FA, SessionTimeout, LockoutAttempts,
                            EmailNotifications, PushNotifications, SmsNotifications
                        ) VALUES (
                            1, @PlatformName, @SupportEmail, @AllowRegistration, @DefaultTheme,
                            @StartBalance, @LessonReward, @TestReward, @MaxDailyCurrency,
                            @MinPasswordLength, @Require2FA, @SessionTimeout, @LockoutAttempts,
                            @EmailNotifications, @PushNotifications, @SmsNotifications
                        )
                ", settings);

                // Логируем действие
                await LogAdminActionAsync("Изменение системных настроек");

                await DisplayAlert("Успех", "Настройки сохранены", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnClearLogsClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Подтверждение",
                "Вы уверены, что хотите очистить все логи?\nЭто действие необратимо.",
                "Да", "Нет");

            if (confirm)
            {
                try
                {
                    using var connection = new SqlConnection(_dbService.ConnectionString);
                    await connection.OpenAsync();

                    // Очищаем таблицу логов
                    await connection.ExecuteAsync("DELETE FROM AdminActions");

                    // Логируем действие
                    await LogAdminActionAsync("Очистка системных логов");

                    await DisplayAlert("Успех", "Логи успешно очищены", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", ex.Message, "OK");
                }
            }
        }

        private async void OnClearCacheClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Подтверждение",
                "Очистить кэш приложения?\nЭто может временно замедлить работу при следующем запуске.",
                "Да", "Нет");

            if (confirm)
            {
                try
                {
                    // Очищаем временные папки
                    string[] cacheFolders = new[]
                    {
                        Path.Combine(FileSystem.AppDataDirectory, "Cache"),
                        Path.Combine(FileSystem.AppDataDirectory, "Temp"),
                        Path.Combine(FileSystem.CacheDirectory)
                    };

                    foreach (var folder in cacheFolders)
                    {
                        if (Directory.Exists(folder))
                        {
                            try
                            {
                                Directory.Delete(folder, true);
                            }
                            catch { }
                        }
                    }

                    // Логируем действие
                    await LogAdminActionAsync("Очистка кэша приложения");

                    await DisplayAlert("Успех", "Кэш успешно очищен", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", ex.Message, "OK");
                }
            }
        }

        private async void OnResetSettingsClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Подтверждение",
                "Вы уверены, что хотите сбросить все настройки к значениям по умолчанию?\nЭто действие необратимо.",
                "Да", "Нет");

            if (confirm)
            {
                try
                {
                    using var connection = new SqlConnection(_dbService.ConnectionString);
                    await connection.OpenAsync();

                    // Сбрасываем настройки к значениям по умолчанию
                    await connection.ExecuteAsync(@"
                        UPDATE SystemSettings SET 
                            PlatformName = 'Educational Platform',
                            SupportEmail = 'support@eduplatform.com',
                            AllowRegistration = 1,
                            DefaultTheme = 'Светлая',
                            StartBalance = 100,
                            LessonReward = 50,
                            TestReward = 75,
                            MaxDailyCurrency = 500,
                            MinPasswordLength = 6,
                            Require2FA = 0,
                            SessionTimeout = 24,
                            LockoutAttempts = 5,
                            EmailNotifications = 1,
                            PushNotifications = 1,
                            SmsNotifications = 0,
                            UpdatedDate = GETDATE()
                        WHERE Id = 1
                    ");

                    // Перезагружаем настройки
                    LoadSettings();

                    // Логируем действие
                    await LogAdminActionAsync("Сброс системных настроек к значениям по умолчанию");

                    await DisplayAlert("Успех", "Настройки сброшены к значениям по умолчанию", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", ex.Message, "OK");
                }
            }
        }

        private async Task LogAdminActionAsync(string action)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                await connection.ExecuteAsync(@"
                    INSERT INTO AdminActions (UserId, ActionType, ActionDate, Details)
                    VALUES (@UserId, 'settings', GETDATE(), @Details)
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

    public class SystemSettings
    {
        public int Id { get; set; }
        public string PlatformName { get; set; } = "Educational Platform";
        public string SupportEmail { get; set; } = "support@eduplatform.com";
        public bool AllowRegistration { get; set; } = true;
        public string DefaultTheme { get; set; } = "Светлая";
        public int StartBalance { get; set; } = 100;
        public int LessonReward { get; set; } = 50;
        public int TestReward { get; set; } = 75;
        public int MaxDailyCurrency { get; set; } = 500;
        public int MinPasswordLength { get; set; } = 6;
        public bool Require2FA { get; set; } = false;
        public int SessionTimeout { get; set; } = 24;
        public int LockoutAttempts { get; set; } = 5;
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = false;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}