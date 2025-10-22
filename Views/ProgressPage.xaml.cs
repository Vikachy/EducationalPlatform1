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

        public ObservableCollection<ProgressItem> ProgressItems { get; set; }
        public ObservableCollection<Achievement> RecentAchievements { get; set; }

        public ProgressPage()
        {
            InitializeComponent();
            ProgressItems = new ObservableCollection<ProgressItem>();
            RecentAchievements = new ObservableCollection<Achievement>();
        }

        // Конструктор с параметрами для инициализации
        public ProgressPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            ProgressItems = new ObservableCollection<ProgressItem>();
            RecentAchievements = new ObservableCollection<Achievement>();

            BindingContext = this;

            // Подписываемся на события смены темы и языка
            SettingsService.GlobalThemeChanged += OnGlobalThemeChanged;
            SettingsService.GlobalLanguageChanged += OnGlobalLanguageChanged;

            LoadProgressData();
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
            if (_settingsService == null) return;

            Title = _settingsService.GetLocalizedString("Progress");
            var overallProgressLabel = this.FindByName<Label>("OverallProgressLabel");
            if (overallProgressLabel != null)
                overallProgressLabel.Text = _settingsService.GetLocalizedString("OverallProgress");
            
            var recentAchievementsLabel = this.FindByName<Label>("RecentAchievementsLabel");
            if (recentAchievementsLabel != null)
                recentAchievementsLabel.Text = _settingsService.GetLocalizedString("RecentAchievements");
            
            var courseProgressLabel = this.FindByName<Label>("CourseProgressLabel");
            if (courseProgressLabel != null)
                courseProgressLabel.Text = _settingsService.GetLocalizedString("CourseProgress");
            
            var statisticsLabel = this.FindByName<Label>("StatisticsLabel");
            if (statisticsLabel != null)
                statisticsLabel.Text = _settingsService.GetLocalizedString("Statistics");
        }

        private async void LoadProgressData()
        {
            try
            {
                if (_currentUser == null || _dbService == null) return;

                UpdatePageTexts();

                // Загружаем статистику пользователя
                var statistics = await _dbService.GetUserStatisticsAsync(_currentUser.UserId);
                UpdateStatistics(statistics);

                // Загружаем прогресс по курсам
                await LoadCourseProgress();

                // Загружаем последние достижения
                await LoadRecentAchievements();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные прогресса: {ex.Message}", "OK");
            }
        }

        private void UpdateStatistics(UserStatistics statistics)
        {
            if (statistics == null) return;

            // Обновляем прогресс-бары и значения
            var totalCoursesProgress = this.FindByName<ProgressBar>("TotalCoursesProgress");
            var averageScoreProgress = this.FindByName<ProgressBar>("AverageScoreProgress");
            var completionRateProgress = this.FindByName<ProgressBar>("CompletionRateProgress");
            var totalCoursesLabel = this.FindByName<Label>("TotalCoursesLabel");
            var averageScoreLabel = this.FindByName<Label>("AverageScoreLabel");
            var completionRateLabel = this.FindByName<Label>("CompletionRateLabel");
            var currentStreakLabel = this.FindByName<Label>("CurrentStreakLabel");
            var totalDaysLabel = this.FindByName<Label>("TotalDaysLabel");

            if (totalCoursesProgress != null)
                totalCoursesProgress.Progress = statistics.TotalCourses > 0 ?
                    (double)statistics.CompletedCourses / statistics.TotalCourses : 0;

            if (averageScoreProgress != null)
                averageScoreProgress.Progress = statistics.AverageScore / 100.0;

            if (completionRateProgress != null)
                completionRateProgress.Progress = statistics.CompletionRate;

            if (totalCoursesLabel != null)
                totalCoursesLabel.Text = $"{statistics.CompletedCourses}/{statistics.TotalCourses}";
            if (averageScoreLabel != null)
                averageScoreLabel.Text = $"{statistics.AverageScore:F1}%";
            if (completionRateLabel != null)
                completionRateLabel.Text = $"{statistics.CompletionRate * 100:F1}%";
            if (currentStreakLabel != null)
                currentStreakLabel.Text = $"{statistics.CurrentStreak} {_settingsService?.GetLocalizedString("Days")}";
            if (totalDaysLabel != null)
                totalDaysLabel.Text = $"{statistics.TotalDays} {_settingsService?.GetLocalizedString("Days")}";
        }

        private async Task LoadCourseProgress()
        {
            try
            {
                ProgressItems.Clear();

                var progress = await _dbService!.GetStudentProgressAsync(_currentUser!.UserId);
                foreach (var item in progress)
                {
                    ProgressItems.Add(new ProgressItem
                    {
                        CourseName = item.CourseName,
                        Status = item.Status,
                        Score = item.Score,
                        CompletionDate = item.CompletionDate,
                        Attempts = item.Attempts
                    });
                }

                var courseProgressCollectionView = this.FindByName<CollectionView>("CourseProgressCollectionView");
                if (courseProgressCollectionView != null)
                    courseProgressCollectionView.ItemsSource = ProgressItems;
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
                RecentAchievements.Clear();

                var achievements = await _dbService!.GetRecentAchievementsAsync(_currentUser!.UserId, 5);
                foreach (var achievement in achievements)
                {
                    RecentAchievements.Add(achievement);
                }

                var achievementsCollectionView = this.FindByName<CollectionView>("AchievementsCollectionView");
                if (achievementsCollectionView != null)
                    achievementsCollectionView.ItemsSource = RecentAchievements;
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
                await DisplayAlert(
                    selectedAchievement.Name,
                    $"{selectedAchievement.Description}\n\nДата получения: {selectedAchievement.EarnedDate:dd.MM.yyyy}",
                    "OK");
            }
            var achievementsCollectionView = this.FindByName<CollectionView>("AchievementsCollectionView");
            if (achievementsCollectionView != null)
                achievementsCollectionView.SelectedItem = null;
        }

        private async void OnCourseSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is ProgressItem selectedCourse)
            {
                await DisplayAlert("Курс", $"Переход к курсу: {selectedCourse.CourseName}", "OK");
            }
            var courseProgressCollectionView = this.FindByName<CollectionView>("CourseProgressCollectionView");
            if (courseProgressCollectionView != null)
                courseProgressCollectionView.SelectedItem = null;
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

    // Модель данных для отображения прогресса
    public class ProgressItem
    {
        public string CourseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int Attempts { get; set; }
        public string StatusColor => Status switch
        {
            "completed" => "#4CAF50",
            "in_progress" => "#2196F3",
            "not_started" => "#9E9E9E",
            _ => "#9E9E9E"
        };
        public string StatusText => Status switch
        {
            "completed" => "Завершен",
            "in_progress" => "В процессе",
            "not_started" => "Не начат",
            _ => Status
        };
        public string FormattedCompletionDate => CompletionDate?.ToString("dd.MM.yyyy") ?? "Не завершен";
    }
}