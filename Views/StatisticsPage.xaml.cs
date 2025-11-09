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

        // Приватные поля для свойств
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

            RecentAchievements = new ObservableCollection<Achievement>();
            BindingContext = this;

            LoadStatistics();
        }

        private async void LoadStatistics()
        {
            try
            {
                IsBusy = true;

                // Загружаем статистику из базы данных
                var stats = await _dbService.GetUserStatisticsAsync(_currentUser.UserId);

                if (stats != null)
                {
                    TotalCourses = stats.TotalCourses;
                    CompletedCourses = stats.CompletedCourses;
                    TotalTimeSpent = stats.TotalTimeSpent ?? 0;
                    AverageScore = stats.AverageScore ?? 0.0;
                    CompletionRate = stats.CompletionRate ?? 0.0;
                    CurrentStreak = stats.CurrentStreak ?? 0;
                    LongestStreak = stats.LongestStreak ?? 0;
                    TotalDays = stats.TotalDays ?? 0;
                }
                else
                {
                    // Если статистика не найдена, устанавливаем значения по умолчанию
                    TotalCourses = 0;
                    CompletedCourses = 0;
                    TotalTimeSpent = 0;
                    AverageScore = 0.0;
                    CompletionRate = 0.0;
                    CurrentStreak = 0;
                    LongestStreak = 0;
                    TotalDays = 0;
                }

                // Загружаем последние достижения
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
                await DisplayAlert("Ошибка", $"Не удалось загрузить статистику: {ex.Message}", "OK");
                Console.WriteLine($"Ошибка загрузки статистики: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void OnAllAchievementsClicked(object sender, EventArgs e)
        {
            await DisplayAlert(
                _settingsService?.CurrentLanguage == "ru" ? "Достижения" : "Achievements",
                _settingsService?.CurrentLanguage == "ru" ? "Откроется полный список достижений." : "Full achievements list will open.",
                "OK");
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
