using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class ProgressPage : ContentPage
    {
        private User? _currentUser;
        private DatabaseService? _dbService;
        private SettingsService? _settingsService;
        private LocalizationService? _localizationService;

        private UserStatistics? _userStatistics;
        public UserStatistics? UserStatistics
        {
            get => _userStatistics;
            set
            {
                _userStatistics = value;
                OnPropertyChanged(nameof(UserStatistics));
            }
        }

        public ObservableCollection<ProgressItem> ProgressItems { get; set; }
        public ObservableCollection<Achievement> RecentAchievements { get; set; }

        public ProgressPage()
        {
            InitializeComponent();
            ProgressItems = new ObservableCollection<ProgressItem>();
            RecentAchievements = new ObservableCollection<Achievement>();
            BindingContext = this;
        }

        public ProgressPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            ProgressItems = new ObservableCollection<ProgressItem>();
            RecentAchievements = new ObservableCollection<Achievement>();
            BindingContext = this;

            // Подписываемся на события
            _settingsService.ThemeChanged += OnThemeChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;

            LoadProgressData();
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
            MainThread.BeginInvokeOnMainThread(UpdatePageTexts);
        }

        private void UpdatePageAppearance()
        {
            // Обновляем внешний вид при смене темы
        }

        private void UpdatePageTexts()
        {
            if (_localizationService == null) return;

            Title = _localizationService.GetText("Progress");

            var titleLabel = this.FindByName<Label>("TitleLabel");
            if (titleLabel != null)
                titleLabel.Text = _localizationService.GetText("LearningProgress");

            var overallProgressLabel = this.FindByName<Label>("OverallProgressLabel");
            if (overallProgressLabel != null)
                overallProgressLabel.Text = _localizationService.GetText("OverallProgress");

            var completedCoursesText = this.FindByName<Label>("CompletedCoursesText");
            if (completedCoursesText != null)
                completedCoursesText.Text = _localizationService.GetText("CompletedCourses");

            var averageScoreText = this.FindByName<Label>("AverageScoreText");
            if (averageScoreText != null)
                averageScoreText.Text = _localizationService.GetText("AverageScore");

            var completionRateText = this.FindByName<Label>("CompletionRateText");
            if (completionRateText != null)
                completionRateText.Text = _localizationService.GetText("CompletionRate");

            var currentStreakText = this.FindByName<Label>("CurrentStreakText");
            if (currentStreakText != null)
                currentStreakText.Text = _localizationService.GetText("CurrentStreak");

            var totalLearningText = this.FindByName<Label>("TotalLearningText");
            if (totalLearningText != null)
                totalLearningText.Text = _localizationService.GetText("TotalDays");

            var recentAchievementsLabel = this.FindByName<Label>("RecentAchievementsLabel");
            if (recentAchievementsLabel != null)
                recentAchievementsLabel.Text = _localizationService.GetText("RecentAchievements");

            var courseProgressLabel = this.FindByName<Label>("CourseProgressLabel");
            if (courseProgressLabel != null)
                courseProgressLabel.Text = _localizationService.GetText("CourseProgress");

            var noAchievementsLabel = this.FindByName<Label>("NoAchievementsLabel");
            if (noAchievementsLabel != null)
                noAchievementsLabel.Text = _localizationService.GetText("NoAchievements");

            var noCoursesLabel = this.FindByName<Label>("NoCoursesLabel");
            if (noCoursesLabel != null)
                noCoursesLabel.Text = _localizationService.GetText("NoCourses");

            var backButton = this.FindByName<Button>("BackButton");
            if (backButton != null)
                backButton.Text = _localizationService.GetText("BackToMain");
        }

        private async void LoadProgressData()
        {
            try
            {
                if (_currentUser == null || _dbService == null) return;

                var loadingIndicator = this.FindByName<VerticalStackLayout>("LoadingIndicator");
                var mainContent = this.FindByName<VerticalStackLayout>("MainContent");

                if (loadingIndicator != null) loadingIndicator.IsVisible = true;
                if (mainContent != null) mainContent.IsVisible = false;

                UpdatePageTexts();

                var statistics = await CalculateUserStatistics(_currentUser.UserId);
                UserStatistics = statistics;

                await LoadCourseProgress();
                await LoadRecentAchievements();

                if (loadingIndicator != null) loadingIndicator.IsVisible = false;
                if (mainContent != null) mainContent.IsVisible = true;
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService?.GetText("Error") ?? "Error",
                    $"Failed to load progress data: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async Task<UserStatistics> CalculateUserStatistics(int userId)
        {
            var stats = new UserStatistics();

            try
            {
                var progressList = await _dbService.GetStudentProgressAsync(userId);

                if (progressList != null && progressList.Any())
                {
                    stats.CompletedCourses = progressList.Count(p => p.Status == "completed");

                    var completedScores = progressList
                        .Where(p => p.Status == "completed" && p.Score > 0)
                        .Select(p => (double)p.Score);

                    if (completedScores.Any())
                        stats.AverageScore = completedScores.Average();

                    var totalProgress = progressList.Sum(p => p.Score);
                    stats.CompletionRate = progressList.Count > 0
                        ? (double)totalProgress / (progressList.Count * 100)
                        : 0;
                }

                var achievements = await _dbService.GetUserAchievementsAsync(userId);
                stats.AchievementsCount = achievements?.Count ?? 0;

                stats.CurrentStreak = _currentUser?.StreakDays ?? 0;

                if (_currentUser?.RegistrationDate != null)
                {
                    stats.TotalDays = (DateTime.Now - _currentUser.RegistrationDate).Days;
                }

                var tasks = await _dbService.GetUserTasksAsync(userId);
                stats.TotalTasks = tasks?.Count ?? 0;
                stats.PendingTasks = tasks?.Count(t => !t.IsCompleted) ?? 0;

                var totalMinutes = await _dbService.GetTotalLearningMinutesAsync(userId);
                stats.TotalHours = totalMinutes / 60;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка расчета статистики: {ex.Message}");
            }

            return stats;
        }

        private async Task LoadCourseProgress()
        {
            try
            {
                if (_dbService == null || _currentUser == null) return;

                ProgressItems.Clear();

                var progress = await _dbService.GetStudentProgressAsync(_currentUser.UserId);

                foreach (var item in progress)
                {
                    var course = await _dbService.GetCourseByIdAsync(item.CourseId);

                    ProgressItems.Add(new ProgressItem
                    {
                        CourseName = course?.CourseName ?? item.CourseName,
                        Status = item.Status,
                        Score = item.Score ?? 0,
                        CompletionDate = item.CompletionDate,
                        Attempts = item.Attempts
                    });
                }

                CourseProgressCollectionView.ItemsSource = ProgressItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки прогресса курсов: {ex.Message}");
            }
        }

        private async Task LoadRecentAchievements()
        {
            try
            {
                if (_dbService == null || _currentUser == null) return;

                RecentAchievements.Clear();

                var achievements = await _dbService.GetRecentAchievementsAsync(_currentUser.UserId, 5);

                foreach (var achievement in achievements)
                {
                    RecentAchievements.Add(achievement);
                }

                AchievementsCollectionView.ItemsSource = RecentAchievements;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки достижений: {ex.Message}");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnAchievementSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Achievement selectedAchievement)
            {
                string dateStr = selectedAchievement.EarnedDate.HasValue
                    ? selectedAchievement.EarnedDate.Value.ToString("dd.MM.yyyy")
                    : "—";

                await DisplayAlert(
                    selectedAchievement.Name,
                    $"{selectedAchievement.Description}\n\n{_localizationService?.GetText("EarnedDate") ?? "Получено"}: {dateStr}",
                    _localizationService?.GetText("OK") ?? "OK");
            }

            if (AchievementsCollectionView != null)
                AchievementsCollectionView.SelectedItem = null;
        }

        private async void OnCourseSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is ProgressItem selectedCourse)
            {
                bool continueLearning = await DisplayAlert(
                    _localizationService?.GetText("Course") ?? "Курс",
                    $"{_localizationService?.GetText("ContinueCourse") ?? "Продолжить"} {selectedCourse.CourseName}?",
                    _localizationService?.GetText("Yes") ?? "Да",
                    _localizationService?.GetText("No") ?? "Нет");

                if (continueLearning && _currentUser != null && _dbService != null && _settingsService != null)
                {
                    // Найти CourseId по имени курса (нужно добавить в ProgressItem)
                }
            }

            if (CourseProgressCollectionView != null)
                CourseProgressCollectionView.SelectedItem = null;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_currentUser != null)
            {
                LoadProgressData();
            }
        }
    }
}