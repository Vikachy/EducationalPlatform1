using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;



namespace EducationalPlatform.Views
{
    public partial class ContestPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;

        public ObservableCollection<Contest> ActiveContests { get; set; } = new();
        public ObservableCollection<Contest> CompletedContests { get; set; } = new();
        public ObservableCollection<ContestSubmission> MySubmissions { get; set; } = new();

        public ContestPage(User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            // Убрать вызов InitializeComponent() если он не генерируется
            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = new LocalizationService(); 

            BindingContext = this;
            LoadContests();
        }

        private async void LoadContests()
        {
            try
            {
                // Загружаем активные конкурсы
                var activeContests = await _dbService.GetActiveContestsAsync();
                ActiveContests.Clear();
                foreach (var contest in activeContests)
                {
                    ActiveContests.Add(contest);
                }

                // Загружаем завершенные конкурсы
                var completedContests = await _dbService.GetCompletedContestsAsync();
                CompletedContests.Clear();
                foreach (var contest in completedContests)
                {
                    CompletedContests.Add(contest);
                }

                // Загружаем мои заявки
                var mySubmissions = await _dbService.GetUserContestSubmissionsAsync(_currentUser.UserId);
                MySubmissions.Clear();
                foreach (var submission in mySubmissions)
                {
                    MySubmissions.Add(submission);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка загрузки конкурсов: {ex.Message}", "OK");
            }
        }

        private async void OnParticipateClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int contestId)
            {
                await Navigation.PushAsync(new ContestSubmissionPage(contestId, _currentUser, _dbService, _settingsService));
            }
        }

        private async void OnViewResultsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int contestId)
            {
                await Navigation.PushAsync(new ContestResultsPage(contestId, _currentUser, _dbService, _settingsService));
            }
        }

        private async void OnCreateSubmissionClicked(object sender, EventArgs e)
        {
            // Показываем список активных конкурсов для выбора
            var activeContests = ActiveContests.ToList();
            if (activeContests.Count == 0)
            {
                await DisplayAlert("Информация", "Нет активных конкурсов для участия", "OK");
                return;
            }

            var contestNames = activeContests.Select(c => c.ContestName).ToArray();
            var selectedContest = await DisplayActionSheet("Выберите конкурс", "Отмена", null, contestNames);
            
            if (selectedContest != null && selectedContest != "Отмена")
            {
                var contest = activeContests.FirstOrDefault(c => c.ContestName == selectedContest);
                if (contest != null)
                {
                    await Navigation.PushAsync(new ContestSubmissionPage(contest.ContestId, _currentUser, _dbService, _settingsService));
                }
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadContests();
        }
    }
}

