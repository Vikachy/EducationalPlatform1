using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class ProfilePage : ContentPage
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;
        public ObservableCollection<Achievement> Achievements { get; set; }
        public ObservableCollection<ActiveCourse> ActiveCourses { get; set; }

        public ProfilePage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            Achievements = new ObservableCollection<Achievement>();
            ActiveCourses = new ObservableCollection<ActiveCourse>();
            BindingContext = this;

            // Подписываемся на глобальные события
            SettingsService.GlobalThemeChanged += OnGlobalThemeChanged;
            SettingsService.GlobalLanguageChanged += OnGlobalLanguageChanged;

            LoadUserData();
            LoadAchievements();
            LoadActiveCourses();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Отписываемся от событий
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
        }

        private void UpdatePageAppearance()
        {
            // Дополнительное обновление внешнего вида если нужно
        }

        private void UpdatePageTexts()
        {
            if (_settingsService == null) return;
            UserSinceLabel.Text = _settingsService.CurrentLanguage == "ru"
                ? $"С нами с {_currentUser.RegistrationDate:dd.MM.yyyy}"
                : $"Member since {_currentUser.RegistrationDate:dd.MM.yyyy}";

            // Обновляем статистику
            CompletedCoursesLabel.Text = _settingsService.CurrentLanguage == "ru" ? "Курсов" : "Courses";
            StreakDaysLabel.Text = _settingsService.CurrentLanguage == "ru" ? "Дней серии" : "Streak Days";
            GameCurrencyLabel.Text = _settingsService.CurrentLanguage == "ru" ? "Монет" : "Coins";

            // Обновляем звание
            string title = GetUserTitle(_currentUser.StreakDays, _currentUser.GameCurrency);
            UserTitleLabel.Text = title;
        }

        private string GetUserTitle(int streakDays, int currency)
        {
            if (_settingsService?.CurrentLanguage == "ru")
            {
                if (currency >= 1000) return "🎯 Бог программирования";
                if (currency >= 500) return "🚀 Продвинутый кодер";
                if (streakDays >= 30) return "🔥 Серийный ученик";
                if (streakDays >= 7) return "⭐ Активный студент";
                return "🎯 Новичок программиста";
            }
            else
            {
                if (currency >= 1000) return "🎯 Programming God";
                if (currency >= 500) return "🚀 Advanced Coder";
                if (streakDays >= 30) return "🔥 Serial Learner";
                if (streakDays >= 7) return "⭐ Active Student";
                return "🎯 Programming Newbie";
            }
        }

        private void LoadUserData()
        {
            UserNameLabel.Text = $"{_currentUser.FirstName} {_currentUser.LastName}";
            UpdatePageTexts();
            CompletedCoursesLabel.Text = "3";
            StreakDaysLabel.Text = _currentUser.StreakDays.ToString();
            GameCurrencyLabel.Text = _currentUser.GameCurrency.ToString();
            OverallProgressBar.Progress = 0.65;
            ProgressPercentLabel.Text = "65%";
        }

        private void LoadAchievements()
        {
            Achievements.Clear();
            if (_settingsService?.CurrentLanguage == "ru")
            {
                Achievements.Add(new Achievement { Icon = "🏆", Name = "Первый курс", Description = "Завершил первый курс" });
                Achievements.Add(new Achievement { Icon = "🔥", Name = "Серия 7 дней", Description = "Входил 7 дней подряд" });
            }
            else
            {
                Achievements.Add(new Achievement { Icon = "🏆", Name = "First Course", Description = "Completed first course" });
                Achievements.Add(new Achievement { Icon = "🔥", Name = "7 Day Streak", Description = "Logged in 7 days in a row" });
            }
            AchievementsCollectionView.ItemsSource = Achievements;
        }

        private void LoadActiveCourses()
        {
            ActiveCourses.Clear();
            if (_settingsService?.CurrentLanguage == "ru")
            {
                ActiveCourses.Add(new ActiveCourse { CourseName = "C# для начинающих", Progress = 75 });
                ActiveCourses.Add(new ActiveCourse { CourseName = "Python основы", Progress = 40 });
            }
            else
            {
                ActiveCourses.Add(new ActiveCourse { CourseName = "C# for Beginners", Progress = 75 });
                ActiveCourses.Add(new ActiveCourse { CourseName = "Python Basics", Progress = 40 });
            }
            ActiveCoursesCollectionView.ItemsSource = ActiveCourses;
        }

        // ИСПРАВЛЕННАЯ НАВИГАЦИЯ:
        private async void OnBackClicked(object sender, EventArgs e)
        {
            try
            {
                // Переход на MainDashboardPage через Navigation.PushAsync с передачей необходимых параметров
                await Navigation.PushAsync(new MainDashboardPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось вернуться: {ex.Message}", "OK");
            }
        }


        private async void OnAllCoursesClicked(object sender, EventArgs e)
        {
            try
            {
                // Переход к странице CoursesPage без Shell
                await Navigation.PushAsync(new CoursesPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти к курсам: {ex.Message}", "OK");
            }
        }


        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            try
            {
                // Переход на SettingsPage без использования Shell
                await Navigation.PushAsync(new SettingsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти к настройкам: {ex.Message}", "OK");
            }
        }


        private async void OnEditProfileClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new EditProfilePage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть редактирование: {ex.Message}", "OK");
            }
        }

        private async void OnAllAchievementsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Достижения", "Полный список достижений скоро будет доступен!", "OK");
        }

        private async void OnShopClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Магазин", "Магазин внутриигровых предметов скоро будет доступен!", "OK");
        }

        private async void OnStatisticsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Статистика", "Подробная статистика обучения скоро будет доступна!", "OK");
        }

        private async void OnAppearanceClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Внешний вид", "Настройки внешнего вида профиля скоро будут доступны!", "OK");
        }

        protected override bool OnBackButtonPressed()
        {
            OnBackClicked(null!, null!);
            return true;
        }
    }


    // МОДЕЛИ ДЛЯ ПРИВЯЗКИ ДАННЫХ
    public class Achievement
    {
        public string Icon { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ActiveCourse
    {
        public string CourseName { get; set; } = string.Empty;
        public int Progress { get; set; }
        public double ProgressDecimal => Progress / 100.0;
    }
}