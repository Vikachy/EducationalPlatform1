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
        private LocalizationService _localizationService;
        public ObservableCollection<Course> Courses { get; set; }

        public CoursesPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            Courses = new ObservableCollection<Course>();
            BindingContext = this;

            // Подписываемся на события
            _settingsService.ThemeChanged += OnThemeChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;

            InitializeSettings();
            LoadCourses();
            UpdateUserInfo();
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new MainDashboardPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                    $"Не удалось перейти на главную: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnProfileClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new ProfilePage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                    $"Не удалось перейти к профилю: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new SettingsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                    $"Не удалось перейти к настройкам: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new MainDashboardPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                    $"Не удалось вернуться: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _settingsService.ThemeChanged -= OnThemeChanged;
            _localizationService.LanguageChanged -= OnLanguageChanged;
        }

        private void OnThemeChanged(object? sender, string theme)
        {
            UpdatePageAppearance();
        }

        private void OnLanguageChanged(object? sender, string language)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                UpdatePageTexts();
                UpdateCurrentSettingsDisplay();
                LoadCourses();
            });
        }

        private void UpdatePageAppearance()
        {
            // Обновляем внешний вид страницы курсов
        }

        private void UpdatePageTexts()
        {
            if (_localizationService == null) return;
            WelcomeLabel.Text = _localizationService.GetRandomGreeting(_currentUser.FirstName ??
                _localizationService.GetText("User"));
            StreakLabel.Text = _localizationService.GetStreakMessage(_currentUser.StreakDays);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadCourses();
        }

        private void InitializeSettings()
        {
            LanguagePicker.SelectedIndex = _localizationService.CurrentLanguage == "ru" ? 0 : 1;
            ThemePicker.SelectedIndex = _settingsService.CurrentTheme == "standard" ? 0 : 1;
            UpdateCurrentSettingsDisplay();
            LanguagePicker.SelectedIndexChanged += OnLanguagePickerChanged;
            ThemePicker.SelectedIndexChanged += OnThemePickerChanged;
        }

        private void UpdateCurrentSettingsDisplay()
        {
            if (_localizationService == null) return;
            CurrentLanguageLabel.Text = _localizationService.CurrentLanguage == "ru"
                ? _localizationService.GetText("Current") + ": " + _localizationService.GetText("Russian")
                : _localizationService.GetText("Current") + ": " + _localizationService.GetText("English");

            CurrentThemeLabel.Text = _localizationService.CurrentLanguage == "ru"
                ? _localizationService.GetText("Current") + ": " + _localizationService.GetText("Standard")
                : _localizationService.GetText("Current") + ": " + _localizationService.GetText("Teen");
        }

        private void UpdateUserInfo()
        {
            if (WelcomeLabel != null)
                WelcomeLabel.Text = _localizationService.GetRandomGreeting(_currentUser.FirstName ??
                    _localizationService.GetText("User"));
            if (StreakLabel != null)
                StreakLabel.Text = _localizationService.GetStreakMessage(_currentUser.StreakDays);
        }

        private async void OnStartCourseClicked(object? sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is Course course)
            {
                try
                {
                    bool success = await _dbService.EnrollStudentInCourseAsync(_currentUser.UserId, course.CourseId);

                    if (success)
                    {
                        await DisplayAlert(
                            _localizationService?.GetText("Success") ?? "Успех",
                            _localizationService?.CurrentLanguage == "ru"
                                ? $"Вы успешно записались на курс '{course.CourseName}'!"
                                : $"You have successfully enrolled in '{course.CourseName}' course!",
                            _localizationService?.GetText("OK") ?? "OK");

                        LoadCourses();
                    }
                    else
                    {
                        await DisplayAlert(
                            _localizationService?.GetText("Info") ?? "Информация",
                            _localizationService?.CurrentLanguage == "ru"
                                ? $"Вы уже записаны на курс '{course.CourseName}'"
                                : $"You are already enrolled in '{course.CourseName}' course",
                            _localizationService?.GetText("OK") ?? "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert(
                        _localizationService?.GetText("Error") ?? "Ошибка",
                        _localizationService?.CurrentLanguage == "ru"
                            ? $"Не удалось записаться на курс: {ex.Message}"
                            : $"Failed to enroll in course: {ex.Message}",
                        _localizationService?.GetText("OK") ?? "OK");
                }
            }
        }

        private async void LoadCourses()
        {
            try
            {
                var courses = await _dbService.GetAvailableCoursesAsync();
                Courses.Clear();
                foreach (var course in courses)
                {
                    if (course != null && !string.IsNullOrEmpty(course.CourseName))
                    {
                        Courses.Add(course);
                    }
                }
                CoursesCollectionView.ItemsSource = Courses;
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                    $"Не удалось загрузить курсы: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private void OnLanguagePickerChanged(object? sender, EventArgs e)
        {
            if (sender is Picker picker && picker.SelectedIndex != -1)
            {
                string language = picker.SelectedIndex == 0 ? "ru" : "en";
                _localizationService.CurrentLanguage = language;
                UpdateCurrentSettingsDisplay();
                UpdateUserInfo();
            }
        }

        private void OnThemePickerChanged(object? sender, EventArgs e)
        {
            if (sender is Picker picker && picker.SelectedIndex != -1)
            {
                string theme = picker.SelectedIndex == 0 ? "standard" : "teen";
                _settingsService.CurrentTheme = theme;
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
                _localizationService?.GetText("Confirmation") ?? "Подтверждение",
                _localizationService?.CurrentLanguage == "ru"
                    ? "Вы точно хотите выйти из приложения?"
                    : "Do you really want to exit the application?",
                _localizationService?.GetText("Yes") ?? "Да",
                _localizationService?.GetText("No") ?? "Нет");

            if (result && Application.Current != null)
            {
                Application.Current.Quit();
            }
        }

        private async void OnNewsClicked(object sender, EventArgs e)
        {
            await DisplayAlert(
                _localizationService?.GetText("News") ?? "Новости",
                _localizationService?.CurrentLanguage == "ru"
                    ? "Переход к ленте новостей"
                    : "Go to news feed",
                _localizationService?.GetText("OK") ?? "OK");
        }
    }
}