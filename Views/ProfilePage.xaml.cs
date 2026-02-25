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
        private LocalizationService _localizationService;
        private List<StudentProgress> _userProgress = new();

        public ObservableCollection<Achievement> Achievements { get; set; }
        public ObservableCollection<ActiveCourse> ActiveCourses { get; set; }

        public ProfilePage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            Achievements = new ObservableCollection<Achievement>();
            ActiveCourses = new ObservableCollection<ActiveCourse>();
            BindingContext = this;

            // Устанавливаем глобального пользователя
            UserSessionService.CurrentUser = _currentUser;

            // Подписываемся на события
            _settingsService.ThemeChanged += OnThemeChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;
            UserSessionService.AvatarChanged += OnGlobalAvatarChanged;

            // Подписываемся на события магазина
            MessagingCenter.Subscribe<ShopPage>(this, "InventoryUpdated", async (sender) =>
            {
                Console.WriteLine($"📢 Получено событие InventoryUpdated в ProfilePage");
                await LoadUserAvatar();
                await LoadUserData();
            });

            // Загружаем данные
            Task.Run(async () =>
            {
                await LoadUserData();
                await LoadAchievements();
                await LoadActiveCourses();
                await LoadUserAvatar();
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Перезагружаем данные при каждом показе страницы
            Task.Run(async () =>
            {
                await LoadUserAvatar();
                await LoadUserData();
                await LoadAchievements();
                await LoadActiveCourses();
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _settingsService.ThemeChanged -= OnThemeChanged;
            _localizationService.LanguageChanged -= OnLanguageChanged;
            UserSessionService.AvatarChanged -= OnGlobalAvatarChanged;
            MessagingCenter.Unsubscribe<ShopPage>(this, "InventoryUpdated");
        }

        private void OnThemeChanged(object? sender, string theme)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdatePageAppearance();
            });
        }

        private void OnLanguageChanged(object? sender, string language)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdatePageTexts();
                LoadUserData();
            });
        }

        private void UpdatePageAppearance()
        {
            // Обновление внешнего вида при смене темы
            LoadEquippedItems();
        }

        private void UpdatePageTexts()
        {
            if (_localizationService == null || _currentUser?.RegistrationDate == null) return;

            UserSinceLabel.Text = _localizationService.CurrentLanguage == "ru"
                ? $"С нами с {_currentUser.RegistrationDate:dd.MM.yyyy}"
                : $"Member since {_currentUser.RegistrationDate:dd.MM.yyyy}";

            // Обновляем звание
            string title = GetUserTitle(_currentUser.StreakDays, _currentUser.GameCurrency);
            UserTitleLabel.Text = title;
        }

        private string GetUserTitle(int streakDays, int currency)
        {
            if (_localizationService?.CurrentLanguage == "ru")
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

        private async Task LoadUserAvatar()
        {
            try
            {
                Console.WriteLine($"🔄 Загружаем аватар для пользователя {_currentUser.UserId}");

                var currentAvatar = await _dbService.GetUserAvatarAsync(_currentUser.UserId);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (!string.IsNullOrEmpty(currentAvatar))
                    {
                        AvatarImage.Source = ServiceHelper.GetImageSourceFromAvatarData(currentAvatar);
                        _currentUser.AvatarUrl = currentAvatar;
                    }
                    else
                    {
                        AvatarImage.Source = "default_avatar.png";
                    }
                });

                await LoadEquippedItems();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки аватара: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AvatarImage.Source = "default_avatar.png";
                });
            }
        }

        private async Task LoadEquippedItems()
        {
            try
            {
                var equipped = await _dbService.GetEquippedItemsAsync(_currentUser.UserId);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Применяем рамку
                    var frameBorder = this.FindByName<Border>("AvatarFrameBorder");
                    if (frameBorder != null)
                    {
                        if (!string.IsNullOrEmpty(equipped.FrameColor))
                        {
                            try
                            {
                                var frameColor = Color.FromArgb(equipped.FrameColor);
                                frameBorder.Stroke = frameColor;
                                frameBorder.StrokeThickness = 3;
                                frameBorder.BackgroundColor = frameColor.WithAlpha(0.2f);
                            }
                            catch
                            {
                                frameBorder.Stroke = (Color)Application.Current.Resources["AccentColor"];
                                frameBorder.StrokeThickness = 2;
                                frameBorder.BackgroundColor = Colors.White;
                            }
                        }
                        else
                        {
                            frameBorder.Stroke = (Color)Application.Current.Resources["AccentColor"];
                            frameBorder.StrokeThickness = 2;
                            frameBorder.BackgroundColor = Colors.White;
                        }
                    }

                    // Применяем эмодзи к имени
                    string baseName = $"{_currentUser.FirstName} {_currentUser.LastName}";
                    UserNameLabel.Text = !string.IsNullOrEmpty(equipped.EmojiIcon)
                        ? $"{baseName} {equipped.EmojiIcon}"
                        : baseName;
                });

                Console.WriteLine($"🎨 Текущая тема в профиле: {_settingsService.CurrentTheme}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки экипировки: {ex.Message}");
            }
        }

        private async Task LoadUserData()
        {
            try
            {
                var stats = await _dbService.GetUserStatisticsAsync(_currentUser.UserId);
                var balance = await _dbService.GetUserGameCurrencyAsync(_currentUser.UserId);
                var overall = await _dbService.GetOverallLearningProgressAsync(_currentUser.UserId);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    CompletedCoursesLabel.Text = stats.CompletedCourses.ToString();
                    StreakDaysLabel.Text = stats.CurrentStreak.ToString();
                    GameCurrencyLabel.Text = balance.ToString();
                    OverallProgressBar.Progress = overall;
                    ProgressPercentLabel.Text = $"{Math.Round(overall * 100)}%";
                    UserTitleLabel.Text = GetUserTitle(stats.CurrentStreak, balance);
                });

                _currentUser.GameCurrency = balance;
                _currentUser.StreakDays = stats.CurrentStreak;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки профиля: {ex.Message}");
            }
        }

        private async Task LoadAchievements()
        {
            try
            {
                var recent = await _dbService.GetRecentAchievementsAsync(_currentUser.UserId, 10);
                Console.WriteLine($"✅ Загружено {recent.Count} достижений");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Achievements.Clear();
                    foreach (var a in recent)
                    {
                        Achievements.Add(a);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки достижений: {ex.Message}");
            }
        }

        private async Task LoadActiveCourses()
        {
            try
            {
                Console.WriteLine($"🔍 Загружаем активные курсы для пользователя {_currentUser.UserId}");

                // Очищаем список перед загрузкой
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ActiveCourses.Clear();
                });

                // Загружаем прогресс
                _userProgress = await _dbService.GetStudentProgressAsync(_currentUser.UserId);

                Console.WriteLine($"📊 Всего записей прогресса: {_userProgress.Count}");

                if (_userProgress.Count == 0)
                {
                    Console.WriteLine("⚠️ Нет записей прогресса в БД");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        // Можно показать заглушку
                    });
                    return;
                }

                // Фильтруем только незавершенные курсы
                var activeCourses = _userProgress
                    .Where(p => p.Status != "completed")
                    .OrderByDescending(p => p.Score)
                    .ToList();

                Console.WriteLine($"📚 Активных курсов: {activeCourses.Count}");

                if (activeCourses.Count == 0)
                {
                    Console.WriteLine("⚠️ Нет активных курсов (все завершены)");
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var p in activeCourses)
                    {
                        Console.WriteLine($"   ➕ Добавляем курс: {p.CourseName} (ID: {p.CourseId}, прогресс: {p.Score}%)");

                        ActiveCourses.Add(new ActiveCourse
                        {
                            CourseId = p.CourseId,
                            CourseName = p.CourseName,
                            Progress = p.Score ?? 0,
                            Status = p.Status
                        });
                    }

                    // Принудительно обновляем CollectionView
                    ActiveCoursesCollectionView.ItemsSource = null;
                    ActiveCoursesCollectionView.ItemsSource = ActiveCourses;

                    Console.WriteLine($"✅ В UI добавлено {ActiveCourses.Count} курсов");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки активных курсов: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }

        // НАВИГАЦИЯ
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainDashboardPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnCourseSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is ActiveCourse selectedCourse)
            {
                try
                {
                    Console.WriteLine($"📚 Выбран курс: {selectedCourse.CourseName} (ID: {selectedCourse.CourseId})");

                    var progress = _userProgress.FirstOrDefault(p => p.CourseId == selectedCourse.CourseId);
                    if (progress != null)
                    {
                        await Navigation.PushAsync(new CourseStudyPage(_currentUser, _dbService, _settingsService, selectedCourse.CourseId));
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть курс: {ex.Message}", "OK");
                }

                ActiveCoursesCollectionView.SelectedItem = null;
            }
        }

        private async void OnAllCoursesClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new MyCoursesPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти к курсам: {ex.Message}", "OK");
            }
        }

        private async void OnAllAchievementsClicked(object sender, EventArgs e)
        {
            try
            {
                if (Achievements.Count == 0)
                {
                    await DisplayAlert(
                        _localizationService?.GetText("Achievements") ?? "Достижения",
                        _localizationService?.CurrentLanguage == "ru"
                            ? "У вас пока нет достижений"
                            : "You don't have any achievements yet",
                        "OK");
                    return;
                }

                // Показываем все достижения в диалоге
                var achievementsList = Achievements.Select(a =>
                    $"{a.Icon} {a.Name}\n   {a.Description}").ToList();

                var message = string.Join("\n\n", achievementsList);

                await DisplayAlert(
                    string.Format(_localizationService?.GetText("MyAchievements") ?? "Мои достижения ({0})", Achievements.Count),
                    message,
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnAchievementSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Achievement selectedAchievement)
            {
                string dateStr = selectedAchievement.EarnedDate.HasValue
                    ? selectedAchievement.EarnedDate.Value.ToString("dd.MM.yyyy")
                    : "—";

                string message = _localizationService?.CurrentLanguage == "ru"
                    ? $"{selectedAchievement.Description}\n\nПолучено: {dateStr}\nНаграда: {selectedAchievement.RewardCurrency} 🪙"
                    : $"{selectedAchievement.Description}\n\nEarned: {dateStr}\nReward: {selectedAchievement.RewardCurrency} 🪙";

                await DisplayAlert(selectedAchievement.Name, message, "OK");

                AchievementsCollectionView.SelectedItem = null;
            }
        }

        // Остальные методы On... без изменений
        private async void OnSettingsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new SettingsPage(_currentUser, _dbService, _settingsService));
        private async void OnEditProfileClicked(object sender, EventArgs e) => await Navigation.PushAsync(new EditProfilePage(_currentUser, _dbService, _settingsService));
        private async void OnShopClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ShopPage(_currentUser, _dbService, _settingsService));
        private async void OnStatisticsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new StatisticsPage(_currentUser, _dbService, _settingsService));
        private async void OnAppearanceClicked(object sender, EventArgs e) => await Navigation.PushAsync(new SettingsPage(_currentUser, _dbService, _settingsService));
        private async void OnChangePasswordClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ChangePasswordPage(_currentUser, _dbService, _settingsService));

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                bool confirm = await DisplayAlert(
                    _localizationService?.GetText("Confirmation") ?? "Подтверждение",
                    _localizationService?.CurrentLanguage == "ru"
                        ? "Вы действительно хотите выйти?"
                        : "Do you really want to logout?",
                    _localizationService?.GetText("Yes") ?? "Да",
                    _localizationService?.GetText("No") ?? "Нет");

                if (confirm)
                {
                    UserSessionService.CurrentUser = null;
                    Application.Current!.MainPage = new NavigationPage(new MainPage());
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выйти: {ex.Message}", "OK");
            }
        }

        private void OnGlobalAvatarChanged(object? sender, AvatarChangedEventArgs e)
        {
            try
            {
                if (_currentUser == null || e.UserId != _currentUser.UserId)
                    return;

                _currentUser.AvatarUrl = e.AvatarData ?? _currentUser.AvatarUrl;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AvatarImage.Source = ServiceHelper.GetImageSourceFromAvatarData(e.AvatarData);
                    LoadEquippedItems();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки глобального изменения аватара: {ex.Message}");
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
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string Status { get; set; } = string.Empty;
        public double ProgressDecimal => Progress / 100.0;
    }
}