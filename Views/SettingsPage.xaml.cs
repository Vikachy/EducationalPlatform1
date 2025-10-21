using EducationalPlatform.Models;
using EducationalPlatform.Services;
using static Microsoft.Maui.ApplicationModel.Permissions;

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

            // Обновляем отображение текущих настроек
            UpdateCurrentSettingsDisplay();

            // Устанавливаем состояния чекбоксов
            EmailNotifications.IsChecked = true;
            PushNotifications.IsChecked = true;
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

                // Показываем сообщение об успехе
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

                // Показываем сообщение об успехе
                DisplayAlert("Успех", "Тема изменена! 🎨", "OK");
            }
        }

        private async void OnSaveSettingsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Сохранено", "Настройки успешно сохранены! ✅", "OK");
            await Navigation.PopAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}