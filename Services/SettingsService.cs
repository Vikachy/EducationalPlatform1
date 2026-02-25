// Services/SettingsService.cs
using Microsoft.Maui.Storage;

namespace EducationalPlatform.Services
{
    public class SettingsService
    {
        private const string THEME_KEY = "AppTheme";
        private const string DEFAULT_THEME = "standard";

        public event EventHandler<string>? ThemeChanged;

        private DatabaseService? _dbService;
        private int? _currentUserId;

        public string CurrentTheme
        {
            get => Preferences.Get(THEME_KEY, DEFAULT_THEME);
            set
            {
                if (Preferences.Get(THEME_KEY, DEFAULT_THEME) != value)
                {
                    Preferences.Set(THEME_KEY, value);
                    ApplyTheme(value);
                    ThemeChanged?.Invoke(this, value);

                    // Сохраняем в БД, если есть данные пользователя
                    if (_dbService != null && _currentUserId.HasValue)
                    {
                        _ = _dbService.SaveUserThemeAsync(_currentUserId.Value, value);
                    }
                }
            }
        }

        public void InitializeForUser(int userId, DatabaseService dbService)
        {
            _currentUserId = userId;
            _dbService = dbService;

            // Загружаем тему пользователя из БД
            _ = LoadUserThemeAsync();
        }

        private async Task LoadUserThemeAsync()
        {
            if (_dbService != null && _currentUserId.HasValue)
            {
                var userTheme = await _dbService.GetUserThemeAsync(_currentUserId.Value);
                if (!string.IsNullOrEmpty(userTheme) && userTheme != CurrentTheme)
                {
                    CurrentTheme = userTheme;
                }
            }
        }

        public void InitializeTheme()
        {
            // Просто применяем текущую тему из настроек
            ApplyTheme(CurrentTheme);
        }

        public void ApplyTheme(string theme)
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Dispatch(() =>
            {
                try
                {
                    // Получаем цвета для темы
                    var colors = ThemeColorService.GetThemeColors(theme);

                    // Обновляем ресурсы приложения
                    Application.Current.Resources["PrimaryColor"] = colors.Primary;
                    Application.Current.Resources["SecondaryColor"] = colors.Secondary;
                    Application.Current.Resources["BackgroundColor"] = colors.Background;
                    Application.Current.Resources["AccentColor"] = colors.Accent;
                    Application.Current.Resources["DangerColor"] = colors.Danger;
                    Application.Current.Resources["TextColor"] = colors.Text;
                    Application.Current.Resources["LightTextColor"] = colors.LightText;

                    Console.WriteLine($"Тема '{theme}' применена");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Ошибка применения темы: {ex.Message}");
                }
            });
        }
    }
}