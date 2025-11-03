using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class ContestResultsPage : ContentPage
    {
        private readonly int _contestId;
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        public bool CanGrade { get; private set; }

        public ContestResultsPage(int contestId, User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _contestId = contestId;
            _currentUser = currentUser;
            _dbService = dbService;

            // Разрешить оценивание только преподавателям и администраторам
            CanGrade = _currentUser.RoleId == 2 || string.Equals(_currentUser.RoleName, "Teacher", StringComparison.OrdinalIgnoreCase)
                       || _currentUser.RoleId == 3 || string.Equals(_currentUser.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSubmissions();
        }

        private async void LoadSubmissions()
        {
            try
            {
                var items = await _dbService.GetContestSubmissionsForContestAsync(_contestId);
                SubmissionsCollection.ItemsSource = items;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить работы: {ex.Message}", "OK");
            }
        }

        private async void OnDownloadClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string url)
            {
                await Launcher.OpenAsync(new Uri(url));
            }
        }

        private async void OnGradeClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ContestSubmission sub)
            {
                if (!CanGrade)
                {
                    await DisplayAlert("Доступ запрещен", "Оценивать могут только преподаватели и администраторы", "OK");
                    return;
                }
                string scoreStr = await DisplayPromptAsync("Оценка", "Введите балл (0-100)", initialValue: sub.TeacherScore?.ToString() ?? "0");
                if (int.TryParse(scoreStr, out int score))
                {
                    string? comment = await DisplayPromptAsync("Комментарий", "Замечания преподавателя", initialValue: sub.TeacherComment);
                    bool ok = await _dbService.GradeContestSubmissionAsync(sub.SubmissionId, _currentUser.UserId, score, comment ?? "");
                    if (ok)
                    {
                        await DisplayAlert("Успех", "Оценка сохранена", "OK");
                        LoadSubmissions();
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось сохранить оценку", "OK");
                    }
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}