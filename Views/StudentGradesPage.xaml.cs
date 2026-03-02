using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class StudentGradesPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        public ObservableCollection<GradedWork> GradedWorks { get; set; } = new();

        private double _averageScore;
        public double AverageScore
        {
            get => _averageScore;
            set
            {
                _averageScore = value;
                OnPropertyChanged();
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public StudentGradesPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            BindingContext = this;
            LoadGrades();
        }

        private async void LoadGrades()
        {
            try
            {
                var grades = await _dbService.GetStudentGradedWorksAsync(_currentUser.UserId);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    GradedWorks.Clear();
                    foreach (var grade in grades)
                    {
                        GradedWorks.Add(grade);
                    }

                    // Вычисляем средний балл
                    if (grades.Any())
                    {
                        AverageScore = Math.Round(grades.Average(g => g.TeacherScore), 1);
                        AverageScoreLabel.Text = AverageScore.ToString("F1");
                    }
                    else
                    {
                        AverageScore = 0;
                        AverageScoreLabel.Text = "0";
                    }
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}