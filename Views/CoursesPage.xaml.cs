using EducationalPlatform.Services;
using EducationalPlatform.Models;
using EducationalPlatform.Resources.Styles;

namespace EducationalPlatform.Views
{
    public partial class CoursesPage : ContentPage
    {
        private DatabaseService _dbService;
        private User _currentUser;
        private SettingsService _settingsService;

        public CoursesPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            InitializeSettings();
            LoadCourses();
            UpdateUserInfo();

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Загружаем данные когда страница появляется
            LoadCourses();
        }

        private void InitializeSettings()
        {
            LanguagePicker.SelectedIndex = _settingsService.CurrentLanguage == "ru" ? 0 : 1;
            ThemePicker.SelectedIndex = _settingsService.CurrentTheme == "teen" ? 0 : 1;

            UpdateCurrentSettingsDisplay();

            LanguagePicker.SelectedIndexChanged += OnLanguageChanged;
            ThemePicker.SelectedIndexChanged += OnThemeChanged;
        }

        private void UpdateCurrentSettingsDisplay()
        {
            CurrentLanguageLabel.Text = $"Текущий: {(_settingsService.CurrentLanguage == "ru" ? "Русский" : "English")}";
            CurrentThemeLabel.Text = $"Текущая: {(_settingsService.CurrentTheme == "teen" ? "Стандартная" : "Для подростков")}";
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
                if (CoursesCollectionView != null)
                    CoursesCollectionView.ItemsSource = courses;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить курсы: {ex.Message}", "OK");
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
                        await DisplayAlert("Успех", $"Курс '{course.CourseName}' начат!", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось обновить прогресс", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось начать курс: {ex.Message}", "OK");
                }
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
        protected override bool OnBackButtonPressed()
        {
            ShowExitConfirmation();
            return true;
        }

        private async void ShowExitConfirmation()
        {
            bool result = await DisplayAlert("Подтверждение",
                "Вы точно хотите выйти из приложения?", "Да", "Нет");
            if (result && Application.Current != null)
            {
                Application.Current.Quit();
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainDashboard");
        }


        private async void OnNewsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Новости", "Переход к ленте новостей", "OK");
            // Можно добавить переход на отдельную страницу новостей
        }

    }
}