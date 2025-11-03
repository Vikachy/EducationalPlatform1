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
            LoadUserAvatar();
        }

        // Загрузка аватара пользователя
        private async void LoadUserAvatar()
        {
            try
            {
                // Получаем актуальный аватар из базы
                var currentAvatar = await _dbService.GetUserAvatarAsync(_currentUser.UserId);

                if (!string.IsNullOrEmpty(currentAvatar))
                {
                    AvatarImage.Source = ImageSource.FromFile(currentAvatar);
                    _currentUser.AvatarUrl = currentAvatar;
                }
                else
                {
                    AvatarImage.Source = "default_avatar.png";
                }

                // применяем экипировку (рамка/эмодзи/тема)
                var equipped = await _dbService.GetEquippedItemsAsync(_currentUser.UserId);
                // рамка: меняем цвет рамки как пример визуализации
                var frameBorder = this.FindByName<Border>("AvatarFrameBorder");
                if (frameBorder != null)
                {
                    frameBorder.Stroke = equipped.FrameItemId.HasValue ? Color.FromArgb("#FFD700") : frameBorder.Stroke;
                }
                // эмодзи рядом с именем
                if (!string.IsNullOrEmpty(equipped.EmojiIcon))
                {
                    UserNameLabel.Text = $"{UserNameLabel.Text} {equipped.EmojiIcon}";
                }
                // тема
                if (!string.IsNullOrEmpty(equipped.ThemeName))
                {
                    _settingsService.ApplyTheme(equipped.ThemeName.ToLower().Contains("океан") ? "ocean" : "standard");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки аватара: {ex.Message}");
                AvatarImage.Source = "default_avatar.png";
            }
        }

        // Обновите метод OnAppearing чтобы аватар обновлялся при возврате на страницу
        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadUserAvatar(); // Перезагружаем аватар каждый раз при показе страницы
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

        private async void LoadUserData()
        {
            try
            {
                UserNameLabel.Text = $"{_currentUser.FirstName} {_currentUser.LastName}";
                UpdatePageTexts();

                var stats = await _dbService.GetUserStatisticsAsync(_currentUser.UserId);
                CompletedCoursesLabel.Text = stats.CompletedCourses.ToString();
                StreakDaysLabel.Text = stats.CurrentStreak?.ToString() ?? _currentUser.StreakDays.ToString();
                var balance = await _dbService.GetUserGameCurrencyAsync(_currentUser.UserId);
                _currentUser.GameCurrency = balance;
                GameCurrencyLabel.Text = balance.ToString();

                var overall = await _dbService.GetOverallLearningProgressAsync(_currentUser.UserId);
                OverallProgressBar.Progress = overall;
                ProgressPercentLabel.Text = $"{Math.Round(overall * 100)}%";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки профиля: {ex.Message}");
            }
        }

        private async void LoadAchievements()
        {
            try
            {
                Achievements.Clear();
                var recent = await _dbService.GetRecentAchievementsAsync(_currentUser.UserId, 10);
                foreach (var a in recent) Achievements.Add(a);
                AchievementsCollectionView.ItemsSource = Achievements;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки достижений: {ex.Message}");
            }
        }

        private async void LoadActiveCourses()
        {
            try
            {
                ActiveCourses.Clear();
                var progress = await _dbService.GetStudentProgressAsync(_currentUser.UserId);
                foreach (var p in progress)
                {
                    ActiveCourses.Add(new ActiveCourse
                    {
                        CourseName = p.CourseName,
                        Progress = p.Score ?? 0
                    });
                }
                ActiveCoursesCollectionView.ItemsSource = ActiveCourses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки активных курсов: {ex.Message}");
            }
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
            try
            {
                await Navigation.PushAsync(new ShopPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть магазин: {ex.Message}", "OK");
            }
        }

        private async void OnStatisticsClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new StatisticsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть статистику: {ex.Message}", "OK");
            }
        }

        private async void OnAppearanceClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new SettingsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть внешний вид: {ex.Message}", "OK");
            }
        }

        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            try
            {
                // Создаем отдельную страницу для смены пароля
                await Navigation.PushAsync(new ChangePasswordPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть смену пароля: {ex.Message}", "OK");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                Application.Current!.MainPage = new NavigationPage(new MainPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выйти: {ex.Message}", "OK");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            OnBackClicked(null!, null!);
            return true;
        }
    }
    public class ActiveCourse
    {
        public string CourseName { get; set; } = string.Empty;
        public int Progress { get; set; }
        public double ProgressDecimal => Progress / 100.0;
    }
}