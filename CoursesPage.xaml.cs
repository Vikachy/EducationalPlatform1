using EducationalPlatform.Services;
using EducationalPlatform.Models;

namespace EducationalPlatform;

public partial class CoursesPage : ContentPage
{
    private DatabaseService _dbService;
    private User _currentUser;
    private SettingsService _settingsService;

    // УБИРАЕМ ВСЕ СВОЙСТВА - используем прямые обращения

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

    private void InitializeSettings()
    {
        if (LanguagePicker != null)
        {
            LanguagePicker.SelectedIndexChanged += OnLanguageChanged;
            LanguagePicker.SelectedIndex = _settingsService.CurrentLanguage == "ru" ? 0 : 1;
        }

        if (ThemePicker != null)
        {
            ThemePicker.SelectedIndexChanged += OnThemeChanged;
            ThemePicker.SelectedIndex = _settingsService.CurrentTheme == "standard" ? 0 : 1;
        }
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

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (sender is Picker picker && picker.SelectedIndex != -1)
        {
            string language = picker.SelectedIndex == 0 ? "ru" : "en";
            _settingsService.ApplyLanguage(language);
            UpdateUserInfo();
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (sender is Picker picker && picker.SelectedIndex != -1)
        {
            string theme = picker.SelectedIndex == 0 ? "standard" : "teen";
            _settingsService.ApplyTheme(theme);
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
}