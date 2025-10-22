using EducationalPlatform.Services;
using EducationalPlatform.Models;
using EducationalPlatform.Resources.Styles;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class CoursesPage : ContentPage
    {
        private DatabaseService _dbService;
        private User _currentUser;
        private SettingsService _settingsService;
        public ObservableCollection<Course> Courses { get; set; }

        public CoursesPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            Courses = new ObservableCollection<Course>();
            BindingContext = this;

            // Подписываемся на глобальные события
            SettingsService.GlobalThemeChanged += OnGlobalThemeChanged;
            SettingsService.GlobalLanguageChanged += OnGlobalLanguageChanged;

            InitializeSettings();
            LoadCourses();
            UpdateUserInfo();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            SettingsService.GlobalThemeChanged -= OnGlobalThemeChanged;
            SettingsService.GlobalLanguageChanged -= OnGlobalLanguageChanged;
        }

        private void OnGlobalThemeChanged(object? sender, string theme)
        {
            UpdatePageAppearance();
        }

        private void OnGlobalLanguageChanged(object? sender, string language)
        {
            UpdatePageTexts();
            LoadCourses(); // Перезагружаем курсы с новым языком
        }

        private void UpdatePageAppearance()
        {
            // Обновляем внешний вид страницы курсов
        }

        private void UpdatePageTexts()
        {
            if (_settingsService == null) return;
            WelcomeLabel.Text = _settingsService.GetRandomGreeting(_currentUser.FirstName ?? "Пользователь");
            StreakLabel.Text = _settingsService.GetStreakMessage(_currentUser.StreakDays);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadCourses();
        }

        private void InitializeSettings()
        {
            LanguagePicker.SelectedIndex = _settingsService.CurrentLanguage == "ru" ? 0 : 1;
            ThemePicker.SelectedIndex = _settingsService.CurrentTheme == "standard" ? 0 : 1;
            UpdateCurrentSettingsDisplay();
            LanguagePicker.SelectedIndexChanged += OnLanguageChanged;
            ThemePicker.SelectedIndexChanged += OnThemeChanged;
        }

        private void UpdateCurrentSettingsDisplay()
        {
            if (_settingsService == null) return;
            CurrentLanguageLabel.Text = _settingsService.CurrentLanguage == "ru"
                ? "Текущий: Русский"
                : "Current: English";
            CurrentThemeLabel.Text = _settingsService.CurrentTheme == "standard"
                ? "Текущая: Стандартная"
                : "Current: For Teens";
        }

        private void UpdateUserInfo()
        {
            if (WelcomeLabel != null)
                WelcomeLabel.Text = _settingsService.GetRandomGreeting(_currentUser.FirstName ?? "Пользователь");
            if (StreakLabel != null)
                StreakLabel.Text = _settingsService.GetStreakMessage(_currentUser.StreakDays);
        }

        private async void LoadCourses()
        {
            try
            {
                var courses = await _dbService.GetCoursesAsync();
                Courses.Clear();
                foreach (var course in courses)
                {
                    // Можно адаптировать названия курсов под язык
                    if (_settingsService?.CurrentLanguage == "en")
                    {
                        // Здесь можно добавить перевод названий курсов
                    }
                    Courses.Add(course);
                }
                CoursesCollectionView.ItemsSource = Courses;
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _settingsService?.GetLocalizedString("Error") ?? "Ошибка",
                    _settingsService?.CurrentLanguage == "ru"
                        ? $"Не удалось загрузить курсы: {ex.Message}"
                        : $"Failed to load courses: {ex.Message}",
                    "OK");
            }
        }

        private async void OnStartCourseClicked(object? sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is Course course)
            {
                try
                {
                    bool success = await _dbService.UpdateProgressAsync(_currentUser.UserId, course.CourseId, "started");
                    if (success)
                    {
                        await DisplayAlert(
                            _settingsService?.GetLocalizedString("Success") ?? "Успех",
                            _settingsService?.CurrentLanguage == "ru"
                                ? $"Курс '{course.CourseName}' начат!"
                                : $"Course '{course.CourseName}' started!",
                            "OK");

                        // Переход на MainDashboardPage с передачей параметров без Shell
                        await Navigation.PushAsync(new MainDashboardPage(_currentUser, _dbService, _settingsService));
                    }
                    else
                    {
                        await DisplayAlert(
                            _settingsService?.GetLocalizedString("Error") ?? "Ошибка",
                            _settingsService?.CurrentLanguage == "ru"
                                ? "Не удалось обновить прогресс"
                                : "Failed to update progress",
                            "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert(
                        _settingsService?.GetLocalizedString("Error") ?? "Ошибка",
                        _settingsService?.CurrentLanguage == "ru"
                            ? $"Не удалось начать курс: {ex.Message}"
                            : $"Failed to start course: {ex.Message}",
                        "OK");
                }
            }
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            if (sender is Picker picker && picker.SelectedIndex != -1)
            {
                string language = picker.SelectedIndex == 0 ? "ru" : "en";
                _settingsService.ApplyLanguage(language);
                UpdateCurrentSettingsDisplay();
                UpdateUserInfo();
            }
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            if (sender is Picker picker && picker.SelectedIndex != -1)
            {
                string theme = picker.SelectedIndex == 0 ? "standard" : "teen";
                _settingsService.ApplyTheme(theme);
                UpdateCurrentSettingsDisplay();
                UpdateUserInfo();
            }
        }

        protected override bool OnBackButtonPressed()
        {
            ShowExitConfirmation();
            return true;
        }

        private async void ShowExitConfirmation()
        {
            bool result = await DisplayAlert(
                _settingsService?.GetLocalizedString("Confirmation") ?? "Подтверждение",
                _settingsService?.CurrentLanguage == "ru"
                    ? "Вы точно хотите выйти из приложения?"
                    : "Do you really want to exit the application?",
                _settingsService?.CurrentLanguage == "ru" ? "Да" : "Yes",
                _settingsService?.CurrentLanguage == "ru" ? "Нет" : "No");
            if (result && Application.Current != null)
            {
                Application.Current.Quit();
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            try
            {
                // Переход назад без Shell, с передачей параметров
                await Navigation.PushAsync(new MainDashboardPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось вернуться: {ex.Message}", "OK");
            }
        }

        private async void OnNewsClicked(object sender, EventArgs e)
        {
            await DisplayAlert(
                _settingsService?.GetLocalizedString("News") ?? "Новости",
                _settingsService?.CurrentLanguage == "ru"
                    ? "Переход к ленте новостей"
                    : "Go to news feed",
                "OK");
        }
    }
}
