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
            var localizationService = new LocalizationService();
            localizationService.SetLanguage(_settingsService.CurrentLanguage);
            localizationService.SetTeenStyle(_settingsService.CurrentTheme == "teen");
            
            CurrentLanguageLabel.Text = $"{localizationService.GetText("language")}: {(_settingsService.CurrentLanguage == "ru" ? "Русский" : "English")}";
            CurrentThemeLabel.Text = $"{localizationService.GetText("theme")}: {(_settingsService.CurrentTheme == "standard" ? localizationService.GetText("standard") : localizationService.GetText("teen"))}";

            // Подсвечиваем активную тему
            StandardThemePreview.StrokeThickness = _settingsService.CurrentTheme == "standard" ? 3 : 1;
            TeenThemePreview.StrokeThickness = _settingsService.CurrentTheme == "teen" ? 3 : 1;
        }

        private async void OnLanguageChanged(object sender, EventArgs e)
        {
            if (LanguagePicker.SelectedIndex != -1)
            {
                string language = LanguagePicker.SelectedIndex == 0 ? "ru" : "en";
                _settingsService.ApplyLanguage(language);
                
                // Сохраняем язык в БД для синхронизации между устройствами
                await _dbService.SaveUserSettingsAsync(_currentUser.UserId, language, _settingsService.CurrentTheme);
                
                UpdateCurrentSettingsDisplay();

                // Используем локализованное сообщение
                var localizationService = new LocalizationService();
                localizationService.SetLanguage(language);
                var successMsg = localizationService.GetText("operation_success");
                await DisplayAlert(successMsg, localizationService.GetText("language") + " " + localizationService.GetText("data_updated"), "OK");
            }
        }

        private async void OnThemeChanged(object sender, EventArgs e)
        {
            if (ThemePicker.SelectedIndex != -1)
            {
                string theme = ThemePicker.SelectedIndex == 0 ? "standard" : "teen";
                _settingsService.ApplyTheme(theme);
                
                // Сохраняем тему в БД для синхронизации между устройствами
                await _dbService.SaveUserSettingsAsync(_currentUser.UserId, _settingsService.CurrentLanguage, theme);
                
                UpdateCurrentSettingsDisplay();

                await DisplayAlert("Успех", "Тема изменена! 🎨", "OK");
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

                    _settingsService.ApplyTheme(_settingsService.CurrentTheme);
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