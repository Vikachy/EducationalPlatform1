using EducationalPlatform.Services;
using EducationalPlatform.Models;
using System.Text.Json;

namespace EducationalPlatform.Views
{
    public partial class PracticePage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _courseId;
        private readonly int _lessonId;

        private DatabaseService.PracticeDto? _exercise;

        public PracticePage(User user, DatabaseService dbService, SettingsService settingsService, int courseId, int lessonId, string lessonTitle)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseId = courseId;
            _lessonId = lessonId;
            TitleLabel.Text = lessonTitle;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _exercise = await _dbService.GetPracticeExerciseAsync(_lessonId);
            CodeEditor.Text = _exercise?.StarterCode ?? string.Empty;
        }

        private async void OnCheckClicked(object sender, EventArgs e)
        {
            try
            {
                // Примитивная проверка: сравнение ExpectedOutput
                int score = 0;
                if (_exercise != null)
                {
                    if (!string.IsNullOrEmpty(_exercise.ExpectedOutput))
                    {
                        // Имитация запуска: сравнение по строке
                        var output = CodeEditor.Text?.Trim() ?? string.Empty;
                        score = output.Contains(_exercise.ExpectedOutput.Trim()) ? 100 : 0;
                    }
                    else if (!string.IsNullOrEmpty(_exercise.TestCasesJson))
                    {
                        // Базовая проверка тест-кейсов (JSON массив объектов с expected)
                        try
                        {
                            var testCases = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(_exercise.TestCasesJson!) ?? new();
                            int passed = 0;
                            foreach (var tc in testCases)
                            {
                                if (tc.TryGetValue("expected", out var exp))
                                {
                                    if ((CodeEditor.Text ?? string.Empty).Contains(exp)) passed++;
                                }
                            }
                            score = testCases.Count == 0 ? 0 : (int)(100.0 * passed / testCases.Count);
                        }
                        catch { score = 0; }
                    }
                }

                await _dbService.UpdateProgressWithScoreAsync(_currentUser.UserId, _courseId, _lessonId, score >= 60 ? "completed" : "in_progress", score);
                ResultLabel.Text = score >= 60 ? "Задание выполнено" : "Пока не пройдено";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }
    }
}








