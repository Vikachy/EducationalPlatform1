using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace EducationalPlatform.Views
{
    public partial class StatisticsPage : ContentPage, INotifyPropertyChanged
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;
        private LocalizationService _localizationService;

        private int _totalCourses;
        public int TotalCourses
        {
            get => _totalCourses;
            set
            {
                _totalCourses = value;
                OnPropertyChanged(nameof(TotalCourses));
            }
        }

        private int _completedCourses;
        public int CompletedCourses
        {
            get => _completedCourses;
            set
            {
                _completedCourses = value;
                OnPropertyChanged(nameof(CompletedCourses));
            }
        }

        private int _totalTimeSpent;
        public int TotalTimeSpent
        {
            get => _totalTimeSpent;
            set
            {
                _totalTimeSpent = value;
                OnPropertyChanged(nameof(TotalTimeSpent));
            }
        }

        private double _averageScore;
        public double AverageScore
        {
            get => _averageScore;
            set
            {
                _averageScore = value;
                OnPropertyChanged(nameof(AverageScore));
                OnPropertyChanged(nameof(AverageScoreFormatted));
            }
        }

        public string AverageScoreFormatted => $"{_averageScore:F1}";

        private double _completionRate;
        public double CompletionRate
        {
            get => _completionRate;
            set
            {
                _completionRate = value;
                OnPropertyChanged(nameof(CompletionRate));
            }
        }

        private int _currentStreak;
        public int CurrentStreak
        {
            get => _currentStreak;
            set
            {
                _currentStreak = value;
                OnPropertyChanged(nameof(CurrentStreak));
            }
        }

        private int _longestStreak;
        public int LongestStreak
        {
            get => _longestStreak;
            set
            {
                _longestStreak = value;
                OnPropertyChanged(nameof(LongestStreak));
            }
        }

        private int _totalDays;
        public int TotalDays
        {
            get => _totalDays;
            set
            {
                _totalDays = value;
                OnPropertyChanged(nameof(TotalDays));
            }
        }

        public ObservableCollection<Achievement> RecentAchievements { get; set; }

        public StatisticsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            RecentAchievements = new ObservableCollection<Achievement>();
            BindingContext = this;

            LoadStatistics();
        }

        private async void LoadStatistics()
        {
            try
            {
                var stats = await _dbService.GetUserStatisticsAsync(_currentUser.UserId);

                if (stats != null)
                {
                    var progressList = await _dbService.GetStudentProgressAsync(_currentUser.UserId);
                    TotalCourses = progressList?.Count ?? 0;

                    CompletedCourses = stats.CompletedCourses;
                    AverageScore = stats.AverageScore;
                    CompletionRate = stats.CompletionRate;
                    CurrentStreak = stats.CurrentStreak;
                    LongestStreak = stats.CurrentStreak; 
                    TotalDays = stats.TotalDays;

                    var totalMinutes = await _dbService.GetTotalLearningMinutesAsync(_currentUser.UserId);
                    TotalTimeSpent = totalMinutes / 60;
                }
                else
                {
                    TotalCourses = 0;
                    CompletedCourses = 0;
                    TotalTimeSpent = 0;
                    AverageScore = 0.0;
                    CompletionRate = 0.0;
                    CurrentStreak = 0;
                    LongestStreak = 0;
                    TotalDays = 0;
                }

                var achievements = await _dbService.GetRecentAchievementsAsync(_currentUser.UserId, 5);
                RecentAchievements.Clear();
                if (achievements != null)
                {
                    foreach (var achievement in achievements)
                    {
                        RecentAchievements.Add(achievement);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                    $"{_localizationService?.GetText("FailedToLoadStatistics") ?? "Не удалось загрузить статистику"}: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
                Console.WriteLine($"Ошибка загрузки статистики: {ex}");
            }
        }

        private async void OnAllAchievementsClicked(object sender, EventArgs e)
        {
            await DisplayAlert(
                _localizationService?.CurrentLanguage == "ru" ? "Достижения" : "Achievements",
                _localizationService?.CurrentLanguage == "ru" ? "Откроется полный список достижений." : "Full achievements list will open.",
                _localizationService?.GetText("OK") ?? "OK");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadStatistics();
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}