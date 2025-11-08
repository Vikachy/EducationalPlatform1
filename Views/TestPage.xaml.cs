using EducationalPlatform.Services;
using EducationalPlatform.Models;

namespace EducationalPlatform.Views
{
    public partial class TestPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _courseId;
        private readonly int _lessonId;
        private TestMeta? _meta;
        private int? _attemptId;

        public TestPage(User user, DatabaseService dbService, SettingsService settingsService, int courseId, int lessonId)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseId = courseId;
            _lessonId = lessonId;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _meta = await _dbService.GetTestMetaByLessonAsync(_lessonId);
            TitleLabel.Text = _meta?.Title ?? "Тест";
        }

        private async void OnStartClicked(object sender, EventArgs e)
        {
            if (_meta == null) { await DisplayAlert("Ошибка", "Тест не найден", "OK"); return; }
            _attemptId = await _dbService.StartTestAttemptAsync(_meta.TestId, _currentUser.UserId, null);
            if (_attemptId.HasValue)
            {
                StartButton.IsEnabled = false;
                FinishButton.IsEnabled = true;
                ResultLabel.Text = "Тест начат";
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось начать попытку", "OK");
            }
        }

        private async void OnFinishClicked(object sender, EventArgs e)
        {
            if (!_attemptId.HasValue || _meta == null) return;
            // Демо-оценка: всегда 100
            int score = 100;
            await _dbService.CompleteTestAttemptAsync(_attemptId.Value, score);
            await _dbService.UpdateProgressWithScoreAsync(_currentUser.UserId, _courseId, _lessonId, "completed", score);
            FinishButton.IsEnabled = false;
            ResultLabel.Text = "Тест выполнен";
        }
    }
}














