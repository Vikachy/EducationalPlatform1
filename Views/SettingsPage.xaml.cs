using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;

        private List<ShopItem> _purchasedThemes = new();
        private bool _isInitializing = true; // Флаг для предотвращения лишних срабатываний

        public SettingsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            // Подписываемся на смену языка
            _localizationService.LanguageChanged += OnLanguageChanged;

            InitializeSettings();
            LoadNotificationSettings();
            LoadPurchasedThemes();
            UpdateTexts();
        }

        private void OnLanguageChanged(object? sender, string language)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                UpdateTexts();
                UpdateThemePicker();
            });
        }

        private void UpdateTexts()
        {
            Title = _localizationService.GetText("Settings");

            var appearanceLabel = this.FindByName<Label>("AppearanceLabel");
            if (appearanceLabel != null)
                appearanceLabel.Text = _localizationService.GetText("Appearance");

            var languageLabel = this.FindByName<Label>("LanguageLabel");
            if (languageLabel != null)
                languageLabel.Text = _localizationService.GetText("Language");

            var themeLabel = this.FindByName<Label>("ThemeLabel");
            if (themeLabel != null)
                themeLabel.Text = _localizationService.GetText("Theme");

            var notificationsLabel = this.FindByName<Label>("NotificationsLabel");
            if (notificationsLabel != null)
                notificationsLabel.Text = _localizationService.GetText("Notifications");

            var emailLabel = this.FindByName<Label>("EmailLabel");
            if (emailLabel != null)
                emailLabel.Text = _localizationService.GetText("EmailNotifications");

            var pushLabel = this.FindByName<Label>("PushLabel");
            if (pushLabel != null)
                pushLabel.Text = _localizationService.GetText("PushNotifications");

            var saveButton = this.FindByName<Button>("SaveButton");
            if (saveButton != null)
                saveButton.Text = _localizationService.GetText("Save");

            var backButton = this.FindByName<Button>("BackButton");
            if (backButton != null)
                backButton.Text = $"← {_localizationService.GetText("Back")}";

            // Обновляем LanguagePicker элементы
            if (LanguagePicker != null)
            {
                LanguagePicker.Items[0] = _localizationService.GetText("Russian");
                LanguagePicker.Items[1] = _localizationService.GetText("English");
            }

            UpdateThemePicker();
            UpdateCurrentSettingsDisplay();
        }

        private void InitializeSettings()
        {
            _isInitializing = true;

            // Устанавливаем текущие значения из настроек
            LanguagePicker.SelectedIndex = _localizationService.CurrentLanguage == "ru" ? 0 : 1;

            // Устанавливаем тему после загрузки списка
            Device.BeginInvokeOnMainThread(() => {
                UpdateThemePicker();
                _isInitializing = false;
            });

            UpdateCurrentSettingsDisplay();
        }

        private async void LoadPurchasedThemes()
        {
            try
            {
                var purchasedItems = await _dbService.GetUserPurchasedItemsAsync(_currentUser.UserId, "theme");
                _purchasedThemes = purchasedItems?.ToList() ?? new List<ShopItem>();

                UpdateThemePicker();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки купленных тем: {ex.Message}");
                UpdateThemePicker();
            }
        }

        private void UpdateThemePicker()
        {
            if (ThemePicker == null) return;

            var currentSelection = ThemePicker.SelectedIndex;

            // Очищаем и добавляем базовые темы
            ThemePicker.Items.Clear();
            ThemePicker.Items.Add(_localizationService.GetText("Standard")); // Индекс 0
            ThemePicker.Items.Add(_localizationService.GetText("Teen"));     // Индекс 1

            // Добавляем купленные темы из магазина
            foreach (var theme in _purchasedThemes)
            {
                string themeName = theme.Name;
                if (!ThemePicker.Items.Contains(themeName))
                {
                    ThemePicker.Items.Add(themeName);
                }
            }

            // Устанавливаем выбранную тему
            ThemePicker.SelectedIndex = GetThemeIndex(_settingsService.CurrentTheme);

            Console.WriteLine($"🔄 ThemePicker обновлен. Текущая тема: {_settingsService.CurrentTheme}, индекс: {ThemePicker.SelectedIndex}");
        }

        private int GetThemeIndex(string themeKey)
        {
            // Сначала проверяем стандартные темы
            if (themeKey == "standard") return 0;
            if (themeKey == "teen") return 1;

            // Проверяем купленные темы по их названиям
            int index = 2;
            foreach (var theme in _purchasedThemes)
            {
                string themeKeyFromName = ThemeColorService.GetThemeKeyFromName(theme.Name);
                if (themeKeyFromName == themeKey)
                {
                    return index;
                }
                index++;
            }

            return 0;
        }

        private void LoadNotificationSettings()
        {
            EmailNotifications.IsChecked = Preferences.Default.Get("EmailNotifications", true);
            PushNotifications.IsChecked = Preferences.Default.Get("PushNotifications", true);
        }

        private void UpdateCurrentSettingsDisplay()
        {
            var currentLanguageLabel = this.FindByName<Label>("CurrentLanguageLabel");
            if (currentLanguageLabel != null)
            {
                string lang = _localizationService.CurrentLanguage == "ru"
                    ? _localizationService.GetText("Russian")
                    : _localizationService.GetText("English");
                currentLanguageLabel.Text = $"{_localizationService.GetText("Current")}: {lang}";
            }

            var currentThemeLabel = this.FindByName<Label>("CurrentThemeLabel");
            if (currentThemeLabel != null)
            {
                string theme = GetCurrentThemeDisplayName();
                currentThemeLabel.Text = $"{_localizationService.GetText("Current")}: {theme}";
            }

            // Подсвечиваем выбранную тему в предпросмотре
            var standardPreview = this.FindByName<Border>("StandardThemePreview");
            var teenPreview = this.FindByName<Border>("TeenThemePreview");

            if (standardPreview != null)
                standardPreview.StrokeThickness = _settingsService.CurrentTheme == "standard" ? 4 : 1;
            if (teenPreview != null)
                teenPreview.StrokeThickness = _settingsService.CurrentTheme == "teen" ? 4 : 1;
        }

        private string GetCurrentThemeDisplayName()
        {
            string currentTheme = _settingsService.CurrentTheme;

            if (currentTheme == "standard")
                return _localizationService.GetText("Standard");
            if (currentTheme == "teen")
                return _localizationService.GetText("Teen");

            foreach (var theme in _purchasedThemes)
            {
                if (ThemeColorService.GetThemeKeyFromName(theme.Name) == currentTheme)
                    return theme.Name;
            }

            return _localizationService.GetText("Standard");
        }

        private void OnLanguagePickerChanged(object sender, EventArgs e)
        {
            if (_isInitializing || LanguagePicker.SelectedIndex == -1) return;

            string newLang = LanguagePicker.SelectedIndex == 0 ? "ru" : "en";

            // Проверяем, изменился ли язык
            if (_localizationService.CurrentLanguage == newLang)
            {
                Console.WriteLine("Язык уже применен, пропускаем");
                return;
            }

            Console.WriteLine($"🔤 Выбран язык: {newLang}");

            // Меняем язык через сервис
            _localizationService.CurrentLanguage = newLang;

            // Сохраняем в БД
            _ = _dbService.SaveUserSettingsAsync(_currentUser.UserId, newLang, _settingsService.CurrentTheme);

            // Показываем сообщение
            DisplayAlert(
                _localizationService.GetText("Success"),
                _localizationService.GetText("LanguageChanged"),
                _localizationService.GetText("OK"));
        }

        private async void OnThemePickerChanged(object sender, EventArgs e)
        {
            if (_isInitializing || ThemePicker.SelectedIndex == -1) return;

            string selectedThemeName = ThemePicker.Items[ThemePicker.SelectedIndex];
            string themeKey = ThemeColorService.GetThemeKeyFromName(selectedThemeName);

            Console.WriteLine($"Выбрана тема: {selectedThemeName} -> ключ: {themeKey}");

            // Проверяем, изменилась ли тема
            if (_settingsService.CurrentTheme == themeKey)
            {
                Console.WriteLine("Тема уже применена, пропускаем");
                return;
            }

            // Применяем тему
            _settingsService.CurrentTheme = themeKey;

            // Сохраняем в БД
            await _dbService.SaveUserThemeAsync(_currentUser.UserId, themeKey);

            // Сохраняем настройки
            await _dbService.SaveUserSettingsAsync(
                _currentUser.UserId,
                _localizationService.CurrentLanguage,
                themeKey);

            UpdateCurrentSettingsDisplay();

            // Показываем сообщение
            await DisplayAlert(
                _localizationService.GetText("Success"),
                _localizationService.GetText("ThemeChanged"),
                _localizationService.GetText("OK"));
        }

        private void OnEmailNotificationsChanged(object sender, CheckedChangedEventArgs e)
        {
            Preferences.Default.Set("EmailNotifications", e.Value);
            Console.WriteLine($"Email notifications set to: {e.Value}");
        }

        private void OnPushNotificationsChanged(object sender, CheckedChangedEventArgs e)
        {
            Preferences.Default.Set("PushNotifications", e.Value);
            Console.WriteLine($"Push notifications set to: {e.Value}");
        }

        private async void OnSaveSettingsClicked(object sender, EventArgs e)
        {
            try
            {
                bool success = await _dbService.SaveUserSettingsAsync(
                    _currentUser.UserId,
                    _localizationService.CurrentLanguage,
                    _settingsService.CurrentTheme);

                if (success)
                {
                    await DisplayAlert(
                        _localizationService.GetText("Success"),
                        _localizationService.GetText("SettingsSaved"),
                        _localizationService.GetText("OK"));

                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert(
                        _localizationService.GetText("Error"),
                        _localizationService.GetText("SaveFailed"),
                        _localizationService.GetText("OK"));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService.GetText("Error"),
                    ex.Message,
                    _localizationService.GetText("OK"));
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _localizationService.LanguageChanged -= OnLanguageChanged;

            if (ThemePicker != null)
            {
                ThemePicker.SelectedIndexChanged -= OnThemePickerChanged;
            }
        }
    }
}