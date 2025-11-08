using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class SettingsPage : ContentPage
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;

        public SettingsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            InitializeSettings();
        }

        private void InitializeSettings()
        {
            // Устанавливаем текущие значения
            LanguagePicker.SelectedIndex = _settingsService.CurrentLanguage == "ru" ? 0 : 1;
            ThemePicker.SelectedIndex = _settingsService.CurrentTheme == "standard" ? 0 : 1;

            // Устанавливаем состояния уведомлений
            EmailNotifications.IsChecked = Preferences.Get("EmailNotifications", true);
            PushNotifications.IsChecked = Preferences.Get("PushNotifications", true);

            // Обновляем отображение текущих настроек
            UpdateCurrentSettingsDisplay();
        }

        private void UpdateCurrentSettingsDisplay()
        {
            CurrentLanguageLabel.Text = $"Текущий: {(_settingsService.CurrentLanguage == "ru" ? "Русский" : "English")}";
            CurrentThemeLabel.Text = $"Текущая: {(_settingsService.CurrentTheme == "standard" ? "Стандартная" : "Для подростков")}";

            // Подсвечиваем активную тему
            StandardThemePreview.StrokeThickness = _settingsService.CurrentTheme == "standard" ? 3 : 1;
            TeenThemePreview.StrokeThickness = _settingsService.CurrentTheme == "teen" ? 3 : 1;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (LanguagePicker.SelectedIndex != -1)
            {
                string language = LanguagePicker.SelectedIndex == 0 ? "ru" : "en";
                _settingsService.ApplyLanguage(language);
                UpdateCurrentSettingsDisplay();

                DisplayAlert("Успех", "Язык изменен! ✨", "OK");
            }
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (ThemePicker.SelectedIndex != -1)
            {
                string theme = ThemePicker.SelectedIndex == 0 ? "standard" : "teen";
                _settingsService.ApplyTheme(theme);
                UpdateCurrentSettingsDisplay();

                DisplayAlert("Успех", "Тема изменена! 🎨", "OK");
            }
        }

        // Обработчики уведомлений
        private void OnEmailNotificationsChanged(object sender, CheckedChangedEventArgs e)
        {
            Preferences.Set("EmailNotifications", e.Value);
            Console.WriteLine($"📧 Email уведомления: {(e.Value ? "включены" : "выключены")}");
        }

        private void OnPushNotificationsChanged(object sender, CheckedChangedEventArgs e)
        {
            Preferences.Set("PushNotifications", e.Value);
            Console.WriteLine($"🔔 Push уведомления: {(e.Value ? "включены" : "выключены")}");
        }

        private async void OnSaveSettingsClicked(object sender, EventArgs e)
        {
            try
            {
                // Сохраняем настройки в базу данных
                bool success = await _dbService.SaveUserSettingsAsync(
                    _currentUser.UserId,
                    _settingsService.CurrentLanguage,
                    _settingsService.CurrentTheme);

                if (success)
                {
                    // Сохраняем настройки уведомлений
                    Preferences.Set("EmailNotifications", EmailNotifications.IsChecked);
                    Preferences.Set("PushNotifications", PushNotifications.IsChecked);

                    await DisplayAlert("Сохранено", "Настройки успешно сохранены! ✅", "OK");

                    // Применяем тему ко всем страницам
                    App.Current.Resources["PrimaryColor"] = _settingsService.CurrentTheme == "teen" ?
                        Color.FromArgb("#FF6B9C") : Color.FromArgb("#2E86AB");
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось сохранить настройки в базу данных", "OK");
                }

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка сохранения: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // Просмотр текущей темы
        private void OnCheckThemeClicked(object sender, EventArgs e)
        {
            string currentTheme = _settingsService.GetCurrentTheme();
            string themeName = currentTheme == "standard" ? "Стандартная" : "Для подростков";

            DisplayAlert("Текущая тема", $"Активна тема: {themeName}", "OK");
        }
    }
}