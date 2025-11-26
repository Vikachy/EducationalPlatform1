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

            // Устанавливаем глобального пользователя для всего приложения
            UserSessionService.CurrentUser = _currentUser;

            // Подписываемся на события смены темы, языка и изменения аватара
            SettingsService.GlobalThemeChanged += OnGlobalThemeChanged;
            SettingsService.GlobalLanguageChanged += OnGlobalLanguageChanged;
            UserSessionService.AvatarChanged += OnGlobalAvatarChanged;

            InitializeDashboard();
        }

        public bool IsTeacher => _currentUser?.RoleId == 2; // 2 - учитель

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // ПОКАЗЫВАЕМ ПАНЕЛЬ УЧИТЕЛЯ ЕСЛИ НУЖНО
            TeacherPanel.IsVisible = IsTeacher;
            TeacherButtonsLayout.IsVisible = IsTeacher;

            // Обновляем данные при каждом появлении страницы
            if (_currentUser != null)
            {
                InitializeDashboard();
                // Обновляем аватар при каждом появлении страницы
                LoadUserAvatar();
            }
        }

        private async void OnManageCourseContentClicked(object sender, EventArgs e)
        {
            if (_currentUser?.RoleId == 2 || _currentUser?.RoleId == 3 || _currentUser?.RoleId == 4)
            {
                try
                {
                    // Получаем курсы преподавателя
                    var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                    if (courses.Any())
                    {
                        // Показываем диалог выбора курса
                        string[] courseNames = courses.Select(c => c.CourseName).ToArray();
                        string selectedCourseName = await DisplayActionSheet(
                            "Выберите курс для управления контентом",
                            "Отмена",
                            null,
                            courseNames);

                        if (!string.IsNullOrEmpty(selectedCourseName) && selectedCourseName != "Отмена")
                        {
                            var selectedCourse = courses.FirstOrDefault(c => c.CourseName == selectedCourseName);
                            if (selectedCourse != null)
                            {
                                // Переходим на страницу управления контентом курса
                                await Navigation.PushAsync(new ManageCourseContentPage(
                                    _currentUser, _dbService, _settingsService, selectedCourse));
                            }
                        }
                    }
                    else
                    {
                        await DisplayAlert("Информация", "У вас нет курсов для управления", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось перейти к управлению контентом: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Доступ запрещен", "Эта функция доступна только преподавателям", "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            SettingsService.GlobalThemeChanged -= OnGlobalThemeChanged;
            SettingsService.GlobalLanguageChanged -= OnGlobalLanguageChanged;
            UserSessionService.AvatarChanged -= OnGlobalAvatarChanged;
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
                if (avatarImage != null && _dbService != null && _currentUser != null)
                {
                    Console.WriteLine($"🔄 Загружаем аватар для пользователя {_currentUser.UserId}");
                    
                    // Получаем данные аватара из базы данных (base64 или путь)
                    var currentAvatar = await _dbService.GetUserAvatarAsync(_currentUser.UserId);
                    
                    // Обновляем на главном потоке
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Используем вспомогательный метод для преобразования в ImageSource
                        avatarImage.Source = ServiceHelper.GetImageSourceFromAvatarData(currentAvatar);
                        _currentUser.AvatarUrl = currentAvatar;
                        Console.WriteLine($"✅ Аватар обновлен в UI");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки аватара: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // В случае ошибки показываем дефолтный аватар
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var avatarImage = this.FindByName<Image>("AvatarImage");
                    if (avatarImage != null)
                    {
                        avatarImage.Source = "default_avatar.png";
                    }
                });
            }
        }

        /// <summary>
        /// Глобальный обработчик изменения аватара.
        /// Вызывается после сохранения аватара в EditProfilePage.
        /// </summary>
        private void OnGlobalAvatarChanged(object? sender, AvatarChangedEventArgs e)
        {
            try
            {
                if (_currentUser == null || e.UserId != _currentUser.UserId)
                    return;

                _currentUser.AvatarUrl = e.AvatarData ?? _currentUser.AvatarUrl;

                var avatarImage = this.FindByName<Image>("AvatarImage");
                if (avatarImage != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        avatarImage.Source = ServiceHelper.GetImageSourceFromAvatarData(e.AvatarData);
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки глобального изменения аватара на дашборде: {ex.Message}");
            }
        }

        private async void InitializeDashboard()
        {
            if (_currentUser == null || _settingsService == null || _dbService == null) return;

            try
            {
                // Получаем случайное приветствие из базы данных
                var greeting = await _dbService.GetRandomLoginGreetingAsync(
                    _currentUser.LanguagePref ?? "ru",
                    _currentUser.InterfaceStyle == "teen"
                );

                // Обновляем приветствие
                if (WelcomeLabel != null)
                {
                    WelcomeLabel.Text = greeting;
                }

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
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось инициализировать дашборд: {ex.Message}", "OK");
            }
        }

        // ИСПРАВЛЕННЫЙ МЕТОД - переход на TeacherGroupsPage
        private async void OnTeacherGroupsClicked(object sender, EventArgs e)
        {
            if (_currentUser?.RoleId == 2 || _currentUser?.RoleId == 3 || _currentUser?.RoleId == 4)
            {
                try
                {
                    // Получаем курсы преподавателя
                    var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                    if (courses.Any())
                    {
                        // Переходим на страницу управления группами (теперь с 3 аргументами)
                        await Navigation.PushAsync(new TeacherGroupsPage(_currentUser, _dbService, _settingsService));
                    }
                    else
                    {
                        await DisplayAlert("Информация", "У вас нет курсов для управления группами", "OK");
                    }
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
                            TimeLeft = "7 дней"
                        });
                    }
                }
                else if (_currentUser?.RoleId == 2) // Учитель
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


        // ИСПРАВЛЕННЫЙ МЕТОД - переход к изучению курса при клике
        private async void OnMyCourseSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is MyCourse selectedCourse)
            {
                try
                {
                    // Переходим к изучению курса
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

        // БЫСТРЫЕ ДЕЙСТВИЯ
        private async void OnQuickAction1Clicked(object sender, EventArgs e)
        {
            if (_currentUser != null && _dbService != null && _settingsService != null)
                await Navigation.PushAsync(new CoursesPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnQuickAction2Clicked(object sender, EventArgs e)
        {
            if (_currentUser != null && _dbService != null && _settingsService != null)
                await Navigation.PushAsync(new StatisticsPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnQuickAction3Clicked(object sender, EventArgs e)
        {
            if (_currentUser != null && _dbService != null && _settingsService != null)
                await Navigation.PushAsync(new ShopPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnQuickAction4Clicked(object sender, EventArgs e)
        {
            if (_currentUser != null && _dbService != null && _settingsService != null)
                await Navigation.PushAsync(new ProfilePage(_currentUser, _dbService, _settingsService));
        }

        // НОВЫЕ МЕТОДЫ ДЛЯ СОЗДАНИЯ КОНТЕНТА
        private async void OnCreateContentClicked(object sender, EventArgs e)
        {
            if (_currentUser?.RoleId == 2 || _currentUser?.RoleId == 3 || _currentUser?.RoleId == 4)
            {
                try
                {
                    // Получаем курсы преподавателя
                    var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                    if (courses.Any())
                    {
                        // Показываем диалог выбора курса
                        string[] courseNames = courses.Select(c => c.CourseName).ToArray();
                        string selectedCourseName = await DisplayActionSheet(
                            "Выберите курс для добавления контента",
                            "Отмена",
                            null,
                            courseNames);

                        if (!string.IsNullOrEmpty(selectedCourseName) && selectedCourseName != "Отмена")
                        {
                            var selectedCourse = courses.FirstOrDefault(c => c.CourseName == selectedCourseName);
                            if (selectedCourse != null)
                            {
                                // Переходим на страницу управления контентом курса
                                await Navigation.PushAsync(new ManageCourseContentPage(
                                    _currentUser, _dbService, _settingsService, selectedCourse));
                            }
                        }
                    }
                    else
                    {
                        await DisplayAlert("Информация", "У вас нет курсов для добавления контента", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось создать контент: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Доступ запрещен", "Эта функция доступна только преподавателям", "OK");
            }
        }

        private async void OnCreateTestClicked(object sender, EventArgs e)
        {
            if (_currentUser?.RoleId == 2 || _currentUser?.RoleId == 3 || _currentUser?.RoleId == 4)
            {
                try
                {
                    // Получаем курсы преподавателя
                    var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                    if (courses.Any())
                    {
                        // Показываем диалог выбора курса
                        string[] courseNames = courses.Select(c => c.CourseName).ToArray();
                        string selectedCourseName = await DisplayActionSheet(
                            "Выберите курс для создания теста",
                            "Отмена",
                            null,
                            courseNames);

                        if (!string.IsNullOrEmpty(selectedCourseName) && selectedCourseName != "Отмена")
                        {
                            var selectedCourse = courses.FirstOrDefault(c => c.CourseName == selectedCourseName);
                            if (selectedCourse != null)
                            {
                                // Переходим на страницу создания теста
                                await Navigation.PushAsync(new CreateTestPage(_currentUser, _dbService, _settingsService, selectedCourse.CourseId));
                            }
                        }
                    }
                    else
                    {
                        await DisplayAlert("Информация", "У вас нет курсов для создания теста", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось создать тест: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Доступ запрещен", "Эта функция доступна только преподавателям", "OK");
            }
        }

        private async void OnCreatePracticeClicked(object sender, EventArgs e)
        {
            if (_currentUser?.RoleId == 2 || _currentUser?.RoleId == 3 || _currentUser?.RoleId == 4)
            {
                try
                {
                    // Получаем курсы преподавателя
                    var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                    if (courses.Any())
                    {
                        // Показываем диалог выбора курса
                        string[] courseNames = courses.Select(c => c.CourseName).ToArray();
                        string selectedCourseName = await DisplayActionSheet(
                            "Выберите курс для создания практики",
                            "Отмена",
                            null,
                            courseNames);

                        if (!string.IsNullOrEmpty(selectedCourseName) && selectedCourseName != "Отмена")
                        {
                            var selectedCourse = courses.FirstOrDefault(c => c.CourseName == selectedCourseName);
                            if (selectedCourse != null)
                            {
                                // Переходим на страницу создания практики
                                await Navigation.PushAsync(new CreatePracticePage(_currentUser, _dbService, _settingsService, selectedCourse.CourseId));
                            }
                        }
                    }
                    else
                    {
                        await DisplayAlert("Информация", "У вас нет курсов для создания практики", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось создать практику: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Доступ запрещен", "Эта функция доступна только преподавателям", "OK");
            }
        }

        private async void OnCreateTheoryClicked(object sender, EventArgs e)
        {
            if (_currentUser?.RoleId == 2 || _currentUser?.RoleId == 3 || _currentUser?.RoleId == 4)
            {
                try
                {
                    // Получаем курсы преподавателя
                    var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                    if (courses.Any())
                    {
                        // Показываем диалог выбора курса
                        string[] courseNames = courses.Select(c => c.CourseName).ToArray();
                        string selectedCourseName = await DisplayActionSheet(
                            "Выберите курс для создания теории",
                            "Отмена",
                            null,
                            courseNames);

                        if (!string.IsNullOrEmpty(selectedCourseName) && selectedCourseName != "Отмена")
                        {
                            var selectedCourse = courses.FirstOrDefault(c => c.CourseName == selectedCourseName);
                            if (selectedCourse != null)
                            {
                                // Переходим на страницу создания теории
                                await Navigation.PushAsync(new CreateTheoryPage(_currentUser, _dbService, _settingsService, selectedCourse.CourseId));
                            }
                        }
                    }
                    else
                    {
                        await DisplayAlert("Информация", "У вас нет курсов для создания теории", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось создать теорию: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Доступ запрещен", "Эта функция доступна только преподавателям", "OK");
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

        // ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ НАВИГАЦИИ
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

        private async void OnChatClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser.RoleId == 1) // Студент
                {
                    await Navigation.PushAsync(new StudentChatsPage(_currentUser, _dbService, _settingsService));
                }
                else if (_currentUser.RoleId == 2) // Учитель
                {
                    await Navigation.PushAsync(new TeacherChatsPage(_currentUser, _dbService, _settingsService));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть чаты: {ex.Message}", "OK");
            }
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
        public int CourseId { get; set; }
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