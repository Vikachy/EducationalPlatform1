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

        public ObservableCollection<MyCourse> MyCourses { get; set; }
        public ObservableCollection<TodayTask> TodayTasks { get; set; }
        public ObservableCollection<NewsItem> NewsItems { get; set; }

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

            MyCourses = new ObservableCollection<MyCourse>();
            TodayTasks = new ObservableCollection<TodayTask>();
            NewsItems = new ObservableCollection<NewsItem>();

            BindingContext = this;

            // Подписываемся на события смены темы и языка
            SettingsService.GlobalThemeChanged += OnGlobalThemeChanged;
            SettingsService.GlobalLanguageChanged += OnGlobalLanguageChanged;

            InitializeDashboard();
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
        }

        private void UpdatePageAppearance()
        {
            // Обновляем внешний вид при смене темы
        }

        private void UpdatePageTexts()
        {
            if (_settingsService == null || _currentUser == null) return;

            WelcomeLabel.Text = _settingsService.GetRandomGreeting(_currentUser.FirstName ?? "Пользователь");
            StatsLabel.Text = _settingsService.GetLocalizedString("Streak") + $": {_currentUser.StreakDays} дней 🔥 | " +
                            _settingsService.GetLocalizedString("Currency") + $": {_currentUser.GameCurrency} 💰";
            StreakFireLabel.Text = _settingsService.GetLocalizedString("Streak") + $": {_currentUser.StreakDays} дней";

            UpdateSectionTitles();
        }

        private void UpdateSectionTitles()
        {
            if (_settingsService == null) return;

            // Обновляем заголовки секций
            var myCoursesLabel = this.FindByName<Label>("MyCoursesLabel");
            if (myCoursesLabel != null)
                myCoursesLabel.Text = _settingsService.GetLocalizedString("MyCourses");
            
            var todayTasksLabel = this.FindByName<Label>("TodayTasksLabel");
            if (todayTasksLabel != null)
                todayTasksLabel.Text = _settingsService.GetLocalizedString("TodayTasks");
            
            var newsLabel = this.FindByName<Label>("NewsLabel");
            if (newsLabel != null)
                newsLabel.Text = _settingsService.GetLocalizedString("News");
            
            var quickActionsLabel = this.FindByName<Label>("QuickActionsLabel");
            if (quickActionsLabel != null)
                quickActionsLabel.Text = _settingsService.GetLocalizedString("QuickActions");
        }

        private async Task LoadUserAvatar()
        {
            try
            {
                var avatarImage = this.FindByName<Image>("AvatarImage");
                if (avatarImage != null)
                {
                    // Получаем актуальный аватар из базы
                    var currentAvatar = await _dbService.GetUserAvatarAsync(_currentUser.UserId);

                    if (!string.IsNullOrEmpty(currentAvatar))
                    {
                        avatarImage.Source = ImageSource.FromFile(currentAvatar);
                        _currentUser.AvatarUrl = currentAvatar;
                    }
                    else
                    {
                        avatarImage.Source = "default_avatar.png";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки аватара: {ex.Message}");
            }
        }

        private async void InitializeDashboard()
        {
            if (_currentUser == null || _settingsService == null) return;

            UpdatePageTexts();

            // Загружаем аватар пользователя
            await LoadUserAvatar();

            // Показываем панель учителя если пользователь - учитель, админ или контент-менеджер
            if (_currentUser.RoleId == 2 || _currentUser.RoleId == 3 || _currentUser.RoleId == 4)
            {
                var teacherPanel = this.FindByName<Border>("TeacherPanel");
                if (teacherPanel != null)
                {
                    teacherPanel.IsVisible = true;
                }
            }

            LoadMyCourses();
            LoadTodayTasks();
            LoadNews();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_currentUser != null)
            {
                InitializeDashboard();
            }
        }

        private async void OnTeacherGroupsClicked(object sender, EventArgs e)
        {
            if (_currentUser?.RoleId == 2 || _currentUser?.RoleId == 3 || _currentUser?.RoleId == 4)
            {
                try
                {
                    await DisplayAlert("Группы", "Управление учебными группами", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось перейти к группам: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Доступ запрещен", "Эта функция доступна только преподавателям и администраторам", "OK");
            }
        }

        private async void LoadMyCourses()
        {
            try
            {
                MyCourses.Clear();

                // Загружаем активные курсы пользователя
                if (_currentUser?.RoleId == 1) // Студент
                {
                    var progress = await _dbService!.GetStudentProgressAsync(_currentUser.UserId);
                    foreach (var item in progress)
                    {
                        MyCourses.Add(new MyCourse
                        {
                            CourseName = item.CourseName,
                            Progress = item.Score,
                            Language = "C#",
                            Difficulty = item.Status,
                            TimeLeft = "7 дней"
                        });
                    }
                }
                else if (_currentUser?.RoleId == 2) // Учитель
                {
                    var teacherCourses = await _dbService!.GetTeacherCoursesAsync(_currentUser.UserId);
                    foreach (var course in teacherCourses)
                    {
                        MyCourses.Add(new MyCourse
                        {
                            CourseName = course.CourseName,
                            Progress = (int)(course.AverageRating * 20),
                            Language = course.LanguageName,
                            Difficulty = course.DifficultyName,
                            TimeLeft = $"{course.StudentCount} студентов"
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

                if (_currentUser?.RoleId == 1) // Студент
                {
                    TodayTasks.Add(new TodayTask
                    {
                        Icon = "📚",
                        Title = _settingsService?.CurrentLanguage == "ru" ? "Продолжить курс C#" : "Continue C# Course",
                        Description = _settingsService?.CurrentLanguage == "ru" ? "Урок 5: Основы ООП" : "Lesson 5: OOP Basics",
                        ButtonText = _settingsService?.CurrentLanguage == "ru" ? "Продолжить" : "Continue",
                        CardColor = Color.FromArgb("#E3F2FD"),
                        BorderColor = Color.FromArgb("#2196F3"),
                        ButtonColor = Color.FromArgb("#2196F3")
                    });

                    TodayTasks.Add(new TodayTask
                    {
                        Icon = "🧩",
                        Title = _settingsService?.CurrentLanguage == "ru" ? "Решить практическую задачу" : "Solve Practice Task",
                        Description = _settingsService?.CurrentLanguage == "ru" ? "Создать класс Calculator" : "Create Calculator class",
                        ButtonText = _settingsService?.CurrentLanguage == "ru" ? "Решить" : "Solve",
                        CardColor = Color.FromArgb("#E8F5E8"),
                        BorderColor = Color.FromArgb("#4CAF50"),
                        ButtonColor = Color.FromArgb("#4CAF50")
                    });
                }
                else if (_currentUser?.RoleId == 2) // Учитель
                {
                    TodayTasks.Add(new TodayTask
                    {
                        Icon = "📝",
                        Title = _settingsService?.CurrentLanguage == "ru" ? "Проверить работы" : "Check Assignments",
                        Description = _settingsService?.CurrentLanguage == "ru" ? "5 работ ожидают проверки" : "5 assignments awaiting review",
                        ButtonText = _settingsService?.CurrentLanguage == "ru" ? "Проверить" : "Review",
                        CardColor = Color.FromArgb("#FFF3E0"),
                        BorderColor = Color.FromArgb("#FF9800"),
                        ButtonColor = Color.FromArgb("#FF9800")
                    });

                    TodayTasks.Add(new TodayTask
                    {
                        Icon = "👥",
                        Title = _settingsService?.CurrentLanguage == "ru" ? "Ответить в чатах" : "Reply in Chats",
                        Description = _settingsService?.CurrentLanguage == "ru" ? "3 новых сообщения" : "3 new messages",
                        ButtonText = _settingsService?.CurrentLanguage == "ru" ? "Открыть" : "Open",
                        CardColor = Color.FromArgb("#F3E5F5"),
                        BorderColor = Color.FromArgb("#9C27B0"),
                        ButtonColor = Color.FromArgb("#9C27B0")
                    });
                }

                TodayTasksCountLabel.Text = _settingsService?.CurrentLanguage == "ru" ? "0/2 завершено" : "0/2 complete";
                TodayTasksCollectionView.ItemsSource = TodayTasks;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось загрузить задачи: {ex.Message}");
            }
        }

        private void LoadNews()
        {
            try
            {
                var news = new List<NewsItem>
                {
                    new() {
                        Title = _settingsService?.CurrentLanguage == "ru" ? "🎉 Новый курс по C#" : "🎉 New C# Course",
                        Content = _settingsService?.CurrentLanguage == "ru"
                            ? "Добавлен продвинутый курс по C# с практическими заданиями"
                            : "New advanced C# course with practical assignments added",
                        PublishedDate = DateTime.Now.AddDays(-1)
                    },
                    new() {
                        Title = _settingsService?.CurrentLanguage == "ru" ? "🏆 Конкурс проектов" : "🏆 Project Contest",
                        Content = _settingsService?.CurrentLanguage == "ru"
                            ? "Участвуйте в конкурсе и выигрывайте игровую валюту!"
                            : "Participate in the contest and win game currency!",
                        PublishedDate = DateTime.Now.AddDays(-3)
                    }
                };

                NewsCollectionView.ItemsSource = news;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось загрузить новости: {ex.Message}");
            }
        }

        // ОСНОВНАЯ НАВИГАЦИЯ
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

        // ДОПОЛНИТЕЛЬНАЯ НАВИГАЦИЯ
        private async void OnStatisticsClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser != null && _dbService != null && _settingsService != null)
                    await Navigation.PushAsync(new StatisticsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти к статистике: {ex.Message}", "OK");
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
                await DisplayAlert("Ошибка", $"Не удалось перейти в магазин: {ex.Message}", "OK");
            }
        }

        private async void OnAchievementsClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser != null && _dbService != null && _settingsService != null)
                    await Navigation.PushAsync(new StatisticsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось перейти к достижениям: {ex.Message}", "OK");
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

        private async void OnTeacherPanelClicked(object sender, EventArgs e)
        {
            if (_currentUser?.RoleId == 2 || _currentUser?.RoleId == 3 || _currentUser?.RoleId == 4)
            {
                try
                {
                    if (_dbService != null && _settingsService != null)
                        await Navigation.PushAsync(new TeacherDashboardPage(_currentUser, _dbService, _settingsService));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось перейти к панели учителя: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Доступ запрещен", "Эта функция доступна только преподавателям и администраторам", "OK");
            }
        }

        private async void OnContinueStreakClicked(object sender, EventArgs e)
        {
            await DisplayAlert(
                _settingsService?.GetLocalizedString("Streak") ?? "Серия",
                _settingsService?.CurrentLanguage == "ru"
                    ? "Продолжайте в том же духе! Ваша серия сохраняется."
                    : "Keep it up! Your streak is maintained.",
                "OK");
        }

        private async void OnMyCourseSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is MyCourse selectedCourse)
            {
                await DisplayAlert("Курс", $"Переход к курсу: {selectedCourse.CourseName}", "OK");
            }
            MyCoursesCollectionView.SelectedItem = null;
        }

        private async void OnTaskButtonClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is TodayTask task)
            {
                await DisplayAlert("Задача", $"Начало выполнения: {task.Title}", "OK");
            }
        }

        private async void OnTeacherManagementClicked(object sender, EventArgs e)
        {
            if (_currentUser?.RoleId == 2 || _currentUser?.RoleId == 3 || _currentUser?.RoleId == 4)
            {
                try
                {
                    if (_dbService != null && _settingsService != null)
                        await Navigation.PushAsync(new TeacherManagementPage(_currentUser, _dbService, _settingsService));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось перейти к управлению: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Доступ запрещен", "Эта функция доступна только преподавателям и администраторам", "OK");
            }
        }

        private async void OnAllNewsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Все новости", "📢 Полная лента новостей будет доступна в следующем обновлении", "OK");
        }

        // БЫСТРЫЕ ДЕЙСТВИЯ
        private async void OnQuickAction1Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CoursesPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnQuickAction2Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new StatisticsPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnQuickAction3Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ShopPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnQuickAction4Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage(_currentUser, _dbService, _settingsService));
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                bool result = await DisplayAlert(
                    _settingsService?.GetLocalizedString("Confirmation") ?? "Подтверждение",
                    _settingsService?.CurrentLanguage == "ru"
                        ? "Вы действительно хотите выйти из приложения?"
                        : "Do you really want to exit the application?",
                    _settingsService?.CurrentLanguage == "ru" ? "Да" : "Yes",
                    _settingsService?.CurrentLanguage == "ru" ? "Нет" : "No");

                if (result) Application.Current?.Quit();
            });
            return true;
        }
    }

    // МОДЕЛИ ДАННЫХ
    public class MyCourse
    {
        public string CourseName { get; set; } = string.Empty;
        public int Progress { get; set; }
        public double ProgressDecimal => Progress / 100.0;
        public string Language { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string TimeLeft { get; set; } = string.Empty;
    }

    public class TodayTask
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ButtonText { get; set; } = string.Empty;
        public Color CardColor { get; set; } = Colors.White;
        public Color BorderColor { get; set; } = Colors.Gray;
        public Color ButtonColor { get; set; } = Colors.Blue;
    }

    public class NewsItem
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
    }
}