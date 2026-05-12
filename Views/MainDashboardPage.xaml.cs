using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class MainDashboardPage : ContentPage
    {
        private User? _currentUser;
        private DatabaseService? _dbService;
        private SettingsService? _settingsService;
        private LocalizationService? _localizationService;

        public ObservableCollection<MyCourse> MyCourses { get; set; }
        public ObservableCollection<TodayTask> TodayTasks { get; set; }
        public ObservableCollection<NewsItem> NewsItems { get; set; }

        public bool IsTeacher => _currentUser?.RoleId == 2;
        public bool IsAdmin => _currentUser?.RoleId == 3;
        public bool IsContentManager => _currentUser?.RoleId == 4 || _currentUser?.RoleId == 3; 
        public bool IsStudent => _currentUser?.RoleId == 1;

        public MainDashboardPage()
        {
            InitializeComponent();
            MyCourses = new ObservableCollection<MyCourse>();
            TodayTasks = new ObservableCollection<TodayTask>();
            NewsItems = new ObservableCollection<NewsItem>();
        }

        public MainDashboardPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            MyCourses = new ObservableCollection<MyCourse>();
            TodayTasks = new ObservableCollection<TodayTask>();
            NewsItems = new ObservableCollection<NewsItem>();

            BindingContext = this;

            UserSessionService.CurrentUser = _currentUser;

            _settingsService.ThemeChanged += OnThemeChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;
            UserSessionService.AvatarChanged += OnGlobalAvatarChanged;

            MessagingCenter.Subscribe<ShopPage, int?>(this, "FrameChanged", async (sender, frameItemId) =>
            {
                Console.WriteLine($"📢 Получено событие FrameChanged: {frameItemId}");
                await LoadEquippedItems();
            });

            MessagingCenter.Subscribe<ShopPage, string?>(this, "EmojiChanged", async (sender, emojiIcon) =>
            {
                Console.WriteLine($"📢 Получено событие EmojiChanged: {emojiIcon}");
                await LoadEquippedItems();
            });

            MessagingCenter.Subscribe<ShopPage>(this, "InventoryUpdated", async (sender) =>
            {
                Console.WriteLine($"📢 Получено событие InventoryUpdated");
                await LoadEquippedItems();
            });

            UpdateTexts();

            Task.Run(async () => await InitializeDashboard());
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            UpdateUIBasedOnRole();

            if (_currentUser != null)
            {
                Task.Run(async () => {
                    await LoadUserAvatar();
                    await LoadEquippedItems();
                });

                LoadMyCourses();
                LoadTodayTasks();
                LoadNews();
            }
        }

        private void UpdateUIBasedOnRole()
        {
            if (TeacherPanel != null)
                TeacherPanel.IsVisible = IsTeacher || IsAdmin || IsContentManager;

            if (TeacherButtonsLayout != null)
                TeacherButtonsLayout.IsVisible = IsTeacher || IsAdmin || IsContentManager;

            if (ContentManagerButton != null)
                ContentManagerButton.IsVisible = IsContentManager;

            var adminPanelButton = this.FindByName<Button>("AdminPanelButton");
            if (adminPanelButton != null)
                adminPanelButton.IsVisible = IsAdmin;

            var gradesButton = this.FindByName<Border>("GradesButton");
            if (gradesButton != null)
                gradesButton.IsVisible = IsStudent;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _settingsService.ThemeChanged -= OnThemeChanged;
            _localizationService.LanguageChanged -= OnLanguageChanged;
            UserSessionService.AvatarChanged -= OnGlobalAvatarChanged;

            MessagingCenter.Unsubscribe<ShopPage, int?>(this, "FrameChanged");
            MessagingCenter.Unsubscribe<ShopPage, string?>(this, "EmojiChanged");
            MessagingCenter.Unsubscribe<ShopPage>(this, "InventoryUpdated");
        }

        private void OnThemeChanged(object? sender, string theme)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                UpdatePageAppearance();
                _ = LoadEquippedItems();
            });
        }

        private void OnLanguageChanged(object? sender, string language)
        {
            Console.WriteLine($"MainDashboardPage: язык изменен на {language}");
            MainThread.BeginInvokeOnMainThread(() => {
                UpdateTexts();
                LoadTodayTasks();
                LoadNews();
            });
        }

        private void UpdateTexts()
        {
            if (_localizationService == null || _currentUser == null) return;

            if (WelcomeLabel != null)
                WelcomeLabel.Text = _localizationService.GetRandomGreeting(_currentUser.FirstName ??
                    _localizationService.GetText("User"));

            if (StatsLabel != null)
                StatsLabel.Text = $"{_localizationService.GetText("Streak")}: {_currentUser.StreakDays} 🔥 | " +
                                 $"{_localizationService.GetText("Currency")}: {_currentUser.GameCurrency} 💰";

            var myCoursesLabel = this.FindByName<Label>("MyCoursesLabel");
            if (myCoursesLabel != null)
                myCoursesLabel.Text = _localizationService.GetText("MyActiveCourses");

            var todayTasksLabel = this.FindByName<Label>("TodayTasksLabel");
            if (todayTasksLabel != null)
                todayTasksLabel.Text = _localizationService.GetText("TodayTasks");

            var newsLabel = this.FindByName<Label>("NewsLabel");
            if (newsLabel != null)
                newsLabel.Text = _localizationService.GetText("LatestNews");

            var quickActionsLabel = this.FindByName<Label>("QuickActionsLabel");
            if (quickActionsLabel != null)
                quickActionsLabel.Text = _localizationService.GetText("QuickAccess");

            var allCoursesBtn = this.FindByName<Button>("AllCoursesBtn");
            if (allCoursesBtn != null)
                allCoursesBtn.Text = _localizationService.GetText("AllCourses") + " →";

            var myCoursesBtn = this.FindByName<Button>("MyCoursesBtn");
            if (myCoursesBtn != null)
                myCoursesBtn.Text = _localizationService.GetText("MyCourses") + " →";

            var progressBtn = this.FindByName<Button>("ProgressBtn");
            if (progressBtn != null)
                progressBtn.Text = _localizationService.GetText("Progress");

            var achievementsBtn = this.FindByName<Button>("AchievementsBtn");
            if (achievementsBtn != null)
                achievementsBtn.Text = _localizationService.GetText("Achievements");

            var shopBtn = this.FindByName<Button>("ShopBtn");
            if (shopBtn != null)
                shopBtn.Text = _localizationService.GetText("Shop");
        }

        private void UpdatePageAppearance() { }

        private async Task LoadUserAvatar()
        {
            try
            {
                if (AvatarImage != null && _dbService != null && _currentUser != null)
                {
                    Console.WriteLine($"🔄 Загружаем аватар для пользователя {_currentUser.UserId}");
                    var currentAvatar = await _dbService.GetUserAvatarAsync(_currentUser.UserId);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        AvatarImage.Source = ServiceHelper.GetImageSourceFromAvatarData(currentAvatar);
                        _currentUser.AvatarUrl = currentAvatar;
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки аватара: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (AvatarImage != null)
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
                    var avatarFrameBorder = this.FindByName<Border>("AvatarFrameBorder");
                    if (avatarFrameBorder != null)
                    {
                        if (!string.IsNullOrEmpty(equipped.FrameColor))
                        {
                            try
                            {
                                var frameColor = Color.FromArgb(equipped.FrameColor);
                                avatarFrameBorder.Stroke = frameColor;
                                avatarFrameBorder.StrokeThickness = 3;
                                avatarFrameBorder.BackgroundColor = frameColor.WithAlpha(0.2f);
                                Console.WriteLine($"✅ Рамка применена с цветом: {equipped.FrameColor}");
                            }
                            catch
                            {
                                avatarFrameBorder.Stroke = (Color)Application.Current.Resources["AccentColor"];
                                avatarFrameBorder.StrokeThickness = 2;
                                avatarFrameBorder.BackgroundColor = Colors.White;
                            }
                        }
                        else
                        {
                            avatarFrameBorder.Stroke = (Color)Application.Current.Resources["AccentColor"];
                            avatarFrameBorder.StrokeThickness = 2;
                            avatarFrameBorder.BackgroundColor = Colors.White;
                        }
                    }

                    var userEmojiLabel = this.FindByName<Label>("UserEmojiLabel");
                    if (userEmojiLabel != null)
                    {
                        if (!string.IsNullOrEmpty(equipped.EmojiIcon))
                        {
                            userEmojiLabel.Text = equipped.EmojiIcon;
                            userEmojiLabel.IsVisible = true;
                        }
                        else
                        {
                            userEmojiLabel.IsVisible = false;
                        }
                    }
                });

                if (!string.IsNullOrEmpty(equipped.ThemeKey) &&
                    equipped.ThemeKey != "standard" &&
                    _settingsService.CurrentTheme != equipped.ThemeKey)
                {
                    _settingsService.CurrentTheme = equipped.ThemeKey;
                }
                else if (string.IsNullOrEmpty(equipped.ThemeKey) || equipped.ThemeKey == "standard")
                {
                    var userTheme = await _dbService.GetUserThemeAsync(_currentUser.UserId);
                    if (!string.IsNullOrEmpty(userTheme) && userTheme != "standard" && _settingsService.CurrentTheme != userTheme)
                    {
                        _settingsService.CurrentTheme = userTheme;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки экипировки: {ex.Message}");
            }
        }

        private void OnGlobalAvatarChanged(object? sender, AvatarChangedEventArgs e)
        {
            try
            {
                if (_currentUser == null || e.UserId != _currentUser.UserId)
                    return;

                _currentUser.AvatarUrl = e.AvatarData ?? _currentUser.AvatarUrl;

                if (AvatarImage != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        AvatarImage.Source = ServiceHelper.GetImageSourceFromAvatarData(e.AvatarData);
                        _ = LoadEquippedItems();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки изменения аватара: {ex.Message}");
            }
        }

        private async Task InitializeDashboard()
        {
            if (_currentUser == null || _settingsService == null || _dbService == null) return;

            try
            {
                await LoadEquippedItems();

                var greeting = await _dbService.GetRandomLoginGreetingAsync(
                    _localizationService?.CurrentLanguage ?? "ru",
                    _currentUser.InterfaceStyle == "teen"
                );

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (WelcomeLabel != null)
                        WelcomeLabel.Text = greeting;

                    UpdateTexts();
                    UpdateUIBasedOnRole();
                });

                await LoadUserAvatar();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LoadMyCourses();
                    LoadTodayTasks();
                    LoadNews();
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", $"Не удалось инициализировать дашборд: {ex.Message}", "OK");
                });
            }
        }

        private async void OnMyCourseSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is MyCourse selectedCourse)
            {
                try
                {
                    if (_currentUser != null && _dbService != null && _settingsService != null)
                    {
                        await Navigation.PushAsync(new CourseStudyPage(_currentUser, _dbService, _settingsService, selectedCourse.CourseId));
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть курс: {ex.Message}", "OK");
                }
            }
            MyCoursesCollectionView.SelectedItem = null;
        }

        private async void OnCoursesClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser != null && _dbService != null && _settingsService != null)
                    await Navigation.PushAsync(new CoursesPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти к курсам: {ex.Message}", "OK");
            }
        }

        private async void OnProgressClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser != null && _dbService != null && _settingsService != null)
                    await Navigation.PushAsync(new ProgressPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти к прогрессу: {ex.Message}", "OK");
            }
        }

        private async void OnProfileClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser != null && _dbService != null && _settingsService != null)
                    await Navigation.PushAsync(new ProfilePage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти к профилю: {ex.Message}", "OK");
            }
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser != null && _dbService != null && _settingsService != null)
                    await Navigation.PushAsync(new SettingsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти к настройкам: {ex.Message}", "OK");
            }
        }

        private async void OnAllCoursesClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser != null && _dbService != null && _settingsService != null)
                    await Navigation.PushAsync(new CoursesPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти к курсам: {ex.Message}", "OK");
            }
        }

        private async void OnMyCoursesClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser != null && _dbService != null && _settingsService != null)
                    await Navigation.PushAsync(new MyCoursesPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти к моим курсам: {ex.Message}", "OK");
            }
        }

        private async void OnAchievementsClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser != null && _dbService != null && _settingsService != null)
                {
                    await Navigation.PushAsync(new StatisticsPage(_currentUser, _dbService, _settingsService));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService?.GetText("Error") ?? "Ошибка",
                    $"Не удалось перейти к достижениям: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnShopClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser != null && _dbService != null && _settingsService != null)
                    await Navigation.PushAsync(new ShopPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService?.GetText("Error") ?? "Ошибка",
                    $"Не удалось перейти в магазин: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                bool result = await DisplayAlert(
                    _localizationService?.GetText("Confirmation") ?? "Подтверждение",
                    _localizationService?.CurrentLanguage == "ru"
                        ? "Вы действительно хотите выйти из приложения?"
                        : "Do you really want to exit the application?",
                    _localizationService?.CurrentLanguage == "ru" ? "Да" : "Yes",
                    _localizationService?.CurrentLanguage == "ru" ? "Нет" : "No");

                if (result)
                    Application.Current!.MainPage = new NavigationPage(new MainPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выйти: {ex.Message}", "OK");
            }
        }

        private async void OnTeacherManagementClicked(object sender, EventArgs e)
        {
            if (IsTeacher || IsAdmin || IsContentManager)
            {
                try
                {
                    await Navigation.PushAsync(new TeacherManagementPage(_currentUser, _dbService, _settingsService));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось перейти к управлению: {ex.Message}", "OK");
                }
            }
        }

        private async void OnManageCourseContentClicked(object sender, EventArgs e)
        {
            if (IsTeacher || IsAdmin || IsContentManager)
            {
                try
                {
                    var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                    if (courses.Any())
                    {
                        string[] courseNames = courses.Select(c => c.CourseName).ToArray();
                        string selectedCourseName = await DisplayActionSheet(
                            _localizationService?.CurrentLanguage == "ru" ? "Выберите курс для управления контентом" : "Select course to manage content",
                            _localizationService?.CurrentLanguage == "ru" ? "Отмена" : "Cancel",
                            null,
                            courseNames);

                        if (!string.IsNullOrEmpty(selectedCourseName) && selectedCourseName != (_localizationService?.CurrentLanguage == "ru" ? "Отмена" : "Cancel"))
                        {
                            var selectedCourse = courses.FirstOrDefault(c => c.CourseName == selectedCourseName);
                            if (selectedCourse != null)
                            {
                                await Navigation.PushAsync(new ManageCourseContentPage(
                                    _currentUser, _dbService, _settingsService, selectedCourse));
                            }
                        }
                    }
                    else
                    {
                        await DisplayAlert(
                            _localizationService?.CurrentLanguage == "ru" ? "Информация" : "Information",
                            _localizationService?.CurrentLanguage == "ru" ? "У вас нет курсов для управления" : "You have no courses to manage",
                            _localizationService?.GetText("OK") ?? "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось перейти к управлению контентом: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert(
                    _localizationService?.CurrentLanguage == "ru" ? "Доступ запрещен" : "Access Denied",
                    _localizationService?.CurrentLanguage == "ru" ? "Эта функция доступна только преподавателям" : "This function is only available to teachers",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnTeacherGroupsClicked(object sender, EventArgs e)
        {
            if (IsTeacher || IsAdmin || IsContentManager)
            {
                try
                {
                    var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                    if (courses.Any())
                    {
                        await Navigation.PushAsync(new TeacherGroupsPage(_currentUser, _dbService, _settingsService));
                    }
                    else
                    {
                        await DisplayAlert(
                            _localizationService?.CurrentLanguage == "ru" ? "Информация" : "Information",
                            _localizationService?.CurrentLanguage == "ru" ? "У вас нет курсов для управления группами" : "You have no courses to manage groups",
                            _localizationService?.GetText("OK") ?? "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось перейти к группам: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert(
                    _localizationService?.CurrentLanguage == "ru" ? "Доступ запрещен" : "Access Denied",
                    _localizationService?.CurrentLanguage == "ru" ? "Эта функция доступна только преподавателям и администраторам" : "This function is only available to teachers and administrators",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnCreateTestClicked(object sender, EventArgs e)
        {
            if (IsTeacher || IsAdmin || IsContentManager)
            {
                try
                {
                    var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                    if (courses.Any())
                    {
                        string[] courseNames = courses.Select(c => c.CourseName).ToArray();
                        string selectedCourseName = await DisplayActionSheet(
                            _localizationService?.CurrentLanguage == "ru" ? "Выберите курс для создания теста" : "Select course to create test",
                            _localizationService?.CurrentLanguage == "ru" ? "Отмена" : "Cancel",
                            null,
                            courseNames);

                        if (!string.IsNullOrEmpty(selectedCourseName) && selectedCourseName != (_localizationService?.CurrentLanguage == "ru" ? "Отмена" : "Cancel"))
                        {
                            var selectedCourse = courses.FirstOrDefault(c => c.CourseName == selectedCourseName);
                            if (selectedCourse != null)
                            {
                                await Navigation.PushAsync(new CreateTestPage(_currentUser, _dbService, _settingsService, selectedCourse.CourseId));
                            }
                        }
                    }
                    else
                    {
                        await DisplayAlert(
                            _localizationService?.CurrentLanguage == "ru" ? "Информация" : "Information",
                            _localizationService?.CurrentLanguage == "ru" ? "У вас нет курсов для создания теста" : "You have no courses to create test",
                            _localizationService?.GetText("OK") ?? "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось создать тест: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert(
                    _localizationService?.CurrentLanguage == "ru" ? "Доступ запрещен" : "Access Denied",
                    _localizationService?.CurrentLanguage == "ru" ? "Эта функция доступна только преподавателям" : "This function is only available to teachers",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnCreatePracticeClicked(object sender, EventArgs e)
        {
            if (IsTeacher || IsAdmin || IsContentManager)
            {
                try
                {
                    var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                    if (courses.Any())
                    {
                        string[] courseNames = courses.Select(c => c.CourseName).ToArray();
                        string selectedCourseName = await DisplayActionSheet(
                            _localizationService?.CurrentLanguage == "ru" ? "Выберите курс для создания практики" : "Select course to create practice",
                            _localizationService?.CurrentLanguage == "ru" ? "Отмена" : "Cancel",
                            null,
                            courseNames);

                        if (!string.IsNullOrEmpty(selectedCourseName) && selectedCourseName != (_localizationService?.CurrentLanguage == "ru" ? "Отмена" : "Cancel"))
                        {
                            var selectedCourse = courses.FirstOrDefault(c => c.CourseName == selectedCourseName);
                            if (selectedCourse != null)
                            {
                                await Navigation.PushAsync(new CreatePracticePage(_currentUser, _dbService, _settingsService, selectedCourse.CourseId));
                            }
                        }
                    }
                    else
                    {
                        await DisplayAlert(
                            _localizationService?.CurrentLanguage == "ru" ? "Информация" : "Information",
                            _localizationService?.CurrentLanguage == "ru" ? "У вас нет курсов для создания практики" : "You have no courses to create practice",
                            _localizationService?.GetText("OK") ?? "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось создать практику: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert(
                    _localizationService?.CurrentLanguage == "ru" ? "Доступ запрещен" : "Access Denied",
                    _localizationService?.CurrentLanguage == "ru" ? "Эта функция доступна только преподавателям" : "This function is only available to teachers",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnCreateTheoryClicked(object sender, EventArgs e)
        {
            if (IsTeacher || IsAdmin || IsContentManager)
            {
                try
                {
                    var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                    if (courses.Any())
                    {
                        string[] courseNames = courses.Select(c => c.CourseName).ToArray();
                        string selectedCourseName = await DisplayActionSheet(
                            _localizationService?.CurrentLanguage == "ru" ? "Выберите курс для создания теории" : "Select course to create theory",
                            _localizationService?.CurrentLanguage == "ru" ? "Отмена" : "Cancel",
                            null,
                            courseNames);

                        if (!string.IsNullOrEmpty(selectedCourseName) && selectedCourseName != (_localizationService?.CurrentLanguage == "ru" ? "Отмена" : "Cancel"))
                        {
                            var selectedCourse = courses.FirstOrDefault(c => c.CourseName == selectedCourseName);
                            if (selectedCourse != null)
                            {
                                await Navigation.PushAsync(new CreateTheoryPage(_currentUser, _dbService, _settingsService, selectedCourse.CourseId));
                            }
                        }
                    }
                    else
                    {
                        await DisplayAlert(
                            _localizationService?.CurrentLanguage == "ru" ? "Информация" : "Information",
                            _localizationService?.CurrentLanguage == "ru" ? "У вас нет курсов для создания теории" : "You have no courses to create theory",
                            _localizationService?.GetText("OK") ?? "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось создать теорию: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert(
                    _localizationService?.CurrentLanguage == "ru" ? "Доступ запрещен" : "Access Denied",
                    _localizationService?.CurrentLanguage == "ru" ? "Эта функция доступна только преподавателям" : "This function is only available to teachers",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnContentManagerClicked(object sender, EventArgs e)
        {
            if (IsContentManager)
            {
                await Navigation.PushAsync(new ContentManagerPage(_currentUser, _dbService, _settingsService));
            }
        }

        private async void OnAdminDashboardClicked(object sender, EventArgs e)
        {
            if (IsAdmin)
            {
                await Navigation.PushAsync(new AdminDashboardPage(_currentUser, _dbService, _settingsService));
            }
        }

        private async void LoadMyCourses()
        {
            try
            {
                MyCourses.Clear();

                if (IsStudent)
                {
                    var progressList = await _dbService.GetStudentProgressAsync(_currentUser.UserId);
                    foreach (var progress in progressList)
                    {
                        MyCourses.Add(new MyCourse
                        {
                            CourseId = progress.CourseId,
                            CourseName = progress.CourseName,
                            Progress = progress.Score ?? 0,
                            Language = "C#",
                            Difficulty = progress.Status,
                            TimeLeft = _localizationService?.CurrentLanguage == "ru" ? "7 дней" : "7 days"
                        });
                    }
                }
                else if (IsTeacher || IsAdmin || IsContentManager)
                {
                    var teacherCourses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                    foreach (var course in teacherCourses)
                    {
                        MyCourses.Add(new MyCourse
                        {
                            CourseId = course.CourseId,
                            CourseName = course.CourseName,
                            Progress = (int)(course.AverageRating * 20),
                            Language = course.LanguageName,
                            Difficulty = course.DifficultyName,
                            TimeLeft = _localizationService?.CurrentLanguage == "ru"
                                ? $"{course.StudentCount} студентов"
                                : $"{course.StudentCount} students"
                        });
                    }
                }

                MyCoursesCollectionView.ItemsSource = MyCourses;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить курсы: {ex.Message}", "OK");
            }
        }

        private void LoadTodayTasks()
        {
            try
            {
                TodayTasks.Clear();

                if (IsStudent)
                {
                    TodayTasks.Add(new TodayTask
                    {
                        Icon = "📚",
                        Title = _localizationService?.CurrentLanguage == "ru" ? "Продолжить курс C#" : "Continue C# Course",
                        Description = _localizationService?.CurrentLanguage == "ru" ? "Урок 5: Основы ООП" : "Lesson 5: OOP Basics",
                        ButtonText = _localizationService?.CurrentLanguage == "ru" ? "Продолжить" : "Continue",
                        CardColor = Color.FromArgb("#E3F2FD"),
                        BorderColor = Color.FromArgb("#2196F3"),
                        ButtonColor = Color.FromArgb("#2196F3"),
                        TaskId = 1,
                        IsCompleted = false,
                        TaskType = "lesson"
                    });

                    TodayTasks.Add(new TodayTask
                    {
                        Icon = "🧩",
                        Title = _localizationService?.CurrentLanguage == "ru" ? "Решить практическую задачу" : "Solve Practice Task",
                        Description = _localizationService?.CurrentLanguage == "ru" ? "Создать класс Calculator" : "Create Calculator class",
                        ButtonText = _localizationService?.CurrentLanguage == "ru" ? "Решить" : "Solve",
                        CardColor = Color.FromArgb("#E8F5E8"),
                        BorderColor = Color.FromArgb("#4CAF50"),
                        ButtonColor = Color.FromArgb("#4CAF50"),
                        TaskId = 2,
                        IsCompleted = false,
                        TaskType = "practice"
                    });
                }
                else if (IsTeacher)
                {
                    TodayTasks.Add(new TodayTask
                    {
                        Icon = "📝",
                        Title = _localizationService?.CurrentLanguage == "ru" ? "Проверить работы" : "Check Assignments",
                        Description = _localizationService?.CurrentLanguage == "ru" ? "5 работ ожидают проверки" : "5 assignments awaiting review",
                        ButtonText = _localizationService?.CurrentLanguage == "ru" ? "Проверить" : "Review",
                        CardColor = Color.FromArgb("#FFF3E0"),
                        BorderColor = Color.FromArgb("#FF9800"),
                        ButtonColor = Color.FromArgb("#FF9800"),
                        TaskId = 1,
                        IsCompleted = false,
                        TaskType = "review"
                    });

                    TodayTasks.Add(new TodayTask
                    {
                        Icon = "👥",
                        Title = _localizationService?.CurrentLanguage == "ru" ? "Ответить в чатах" : "Reply in Chats",
                        Description = _localizationService?.CurrentLanguage == "ru" ? "3 новых сообщения" : "3 new messages",
                        ButtonText = _localizationService?.CurrentLanguage == "ru" ? "Открыть" : "Open",
                        CardColor = Color.FromArgb("#F3E5F5"),
                        BorderColor = Color.FromArgb("#9C27B0"),
                        ButtonColor = Color.FromArgb("#9C27B0"),
                        TaskId = 2,
                        IsCompleted = false,
                        TaskType = "chat"
                    });
                }
                else if (IsAdmin)
                {
                    TodayTasks.Add(new TodayTask
                    {
                        Icon = "👑",
                        Title = "Администрирование",
                        Description = "Управление системой",
                        ButtonText = "Открыть",
                        CardColor = Color.FromArgb("#F3E5F5"),
                        BorderColor = Color.FromArgb("#9C27B0"),
                        ButtonColor = Color.FromArgb("#9C27B0"),
                        TaskId = 1,
                        IsCompleted = false,
                        TaskType = "admin"
                    });
                }

                TodayTasksCountLabel.Text = _localizationService?.CurrentLanguage == "ru" ? "0/2 завершено" : "0/2 complete";
                TodayTasksCollectionView.ItemsSource = TodayTasks;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось загрузить задачи: {ex.Message}");
            }
        }

        private async void LoadNews()
        {
            try
            {
                var news = await _dbService.GetLatestNewsAsync(3);

                if (news != null && news.Any())
                {
                    NewsCollectionView.ItemsSource = news;
                }
                else
                {
                    NewsCollectionView.ItemsSource = new List<NewsItem>
                    {
                        new() {
                            Title = _localizationService?.CurrentLanguage == "ru"
                                ? "Добро пожаловать!"
                                : "Welcome!",
                            Content = _localizationService?.CurrentLanguage == "ru"
                                ? "Начните обучение прямо сейчас"
                                : "Start learning now",
                            PublishedDate = DateTime.Now
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки новостей: {ex.Message}");
            }
        }

        private async void OnTaskButtonClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is TodayTask task)
            {
                if (task.TaskType == "admin" && IsAdmin)
                {
                    OnAdminDashboardClicked(sender, e);
                }
                else
                {
                    await DisplayAlert(
                        _localizationService?.CurrentLanguage == "ru" ? "Задача" : "Task",
                        $"{(_localizationService?.CurrentLanguage == "ru" ? "Начало выполнения: " : "Starting: ")}{task.Title}",
                        _localizationService?.GetText("OK") ?? "OK");
                }
            }
        }

        private async void OnAllNewsClicked(object sender, EventArgs e)
        {
            if (_currentUser != null && _dbService != null && _settingsService != null)
                await Navigation.PushAsync(new NewsPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnContestsClicked(object sender, EventArgs e)
        {
            if (_currentUser != null && _dbService != null && _settingsService != null)
                await Navigation.PushAsync(new ContestPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnNewsClicked(object sender, EventArgs e)
        {
            if (_currentUser != null && _dbService != null && _settingsService != null)
                await Navigation.PushAsync(new NewsPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnSupportClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser != null && _dbService != null && _settingsService != null)
                    await Navigation.PushAsync(new SupportPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти в поддержку: {ex.Message}", "OK");
            }
        }

        private async void OnChatClicked(object sender, EventArgs e)
        {
            try
            {
                if (IsStudent)
                {
                    await Navigation.PushAsync(new StudentChatsPage(_currentUser, _dbService, _settingsService));
                }
                else if (IsTeacher || IsAdmin || IsContentManager)
                {
                    await Navigation.PushAsync(new TeacherChatsPage(_currentUser, _dbService, _settingsService));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть чаты: {ex.Message}", "OK");
            }
        }

        private async void OnGradesClicked(object sender, EventArgs e)
        {
            try
            {
                if (IsStudent && _dbService != null && _settingsService != null)
                {
                    await Navigation.PushAsync(new StudentGradesPage(_currentUser, _dbService, _settingsService));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти к оценкам: {ex.Message}", "OK");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                bool result = await DisplayAlert(
                    _localizationService?.GetText("Confirmation") ?? "Подтверждение",
                    _localizationService?.CurrentLanguage == "ru"
                        ? "Вы действительно хотите выйти из приложения?"
                        : "Do you really want to exit the application?",
                    _localizationService?.CurrentLanguage == "ru" ? "Да" : "Yes",
                    _localizationService?.CurrentLanguage == "ru" ? "Нет" : "No");

                if (result) Application.Current?.Quit();
            });
            return true;
        }
    }

    public class MyCourse
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int Progress { get; set; }
        public double ProgressDecimal => Progress / 100.0;
        public string Language { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string TimeLeft { get; set; } = string.Empty;
    }
}