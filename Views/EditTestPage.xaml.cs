using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class EditTestPage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _lessonId;

        public ObservableCollection<Question> Questions { get; set; } = new();

        // Добавляем свойства для привязки данных
        public string TestTitle { get; set; }
        public string TestDescription { get; set; }
        public int TimeLimit { get; set; } = 60;
        public int PassingScore { get; set; } = 60;

        public EditTestPage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
        {
            InitializeComponent();
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _lessonId = lessonId;

            BindingContext = this;
            LoadTestData();
        }

        private async void LoadTestData()
        {
            try
            {
                // Получаем метаданные теста
                var testMeta = await _dbService.GetTestMetaByLessonAsync(_lessonId);
                if (testMeta != null)
                {
                    TestTitle = testMeta.Title;
                    TestDescription = testMeta.Description ?? "";
                    TimeLimit = testMeta.TimeLimitMinutes;
                    PassingScore = testMeta.PassingScore;

                    OnPropertyChanged(nameof(TestTitle));
                    OnPropertyChanged(nameof(TestDescription));
                    OnPropertyChanged(nameof(TimeLimit));
                    OnPropertyChanged(nameof(PassingScore));

                    // Загружаем вопросы теста
                    var questions = await _dbService.GetTestQuestionsAsync(testMeta.TestId);
                    Questions.Clear();
                    foreach (var question in questions)
                    {
                        Questions.Add(question);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные теста: {ex.Message}", "OK");
            }
        }

        private async void OnAddQuestionClicked(object sender, EventArgs e)
        {
            // Переходим на страницу создания вопроса
            var testMeta = await _dbService.GetTestMetaByLessonAsync(_lessonId);
            if (testMeta != null)
            {
                await Navigation.PushAsync(new EditQuestionPage(_user, _dbService, _settingsService, testMeta.TestId));
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось загрузить данные теста", "OK");
            }
        }

        private async void OnEditQuestionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Question question)
            {
                var testMeta = await _dbService.GetTestMetaByLessonAsync(_lessonId);
                if (testMeta != null)
                {
                    await Navigation.PushAsync(new EditQuestionPage(_user, _dbService, _settingsService, testMeta.TestId, question));
                }
            }
        }

        private async void OnDeleteQuestionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Question question)
            {
                var result = await DisplayAlert("Подтверждение", "Вы уверены, что хотите удалить этот вопрос?", "Да", "Нет");
                if (result)
                {
                    var success = await _dbService.DeleteQuestionAsync(question.QuestionId);
                    if (success)
                    {
                        Questions.Remove(question);
                        await DisplayAlert("Успех", "Вопрос удален", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось удалить вопрос", "OK");
                    }
                }
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TestTitle))
                {
                    await DisplayAlert("Ошибка", "Введите название теста", "OK");
                    return;
                }

                if (TimeLimit <= 0)
                {
                    await DisplayAlert("Ошибка", "Введите корректный лимит времени", "OK");
                    return;
                }

                if (PassingScore <= 0)
                {
                    await DisplayAlert("Ошибка", "Введите корректный проходной балл", "OK");
                    return;
                }

                // Получаем метаданные теста
                var testMeta = await _dbService.GetTestMetaByLessonAsync(_lessonId);
                if (testMeta == null)
                {
                    await DisplayAlert("Ошибка", "Тест не найден", "OK");
                    return;
                }

                // Обновляем тест
                var success = await _dbService.UpdateTestMetaAsync(
                    testMeta.TestId,
                    TestTitle,
                    TestDescription,
                    TimeLimit,
                    PassingScore
                );

                if (success)
                {
                    await DisplayAlert("Успех", "Тест успешно обновлен", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось сохранить изменения", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка сохранения: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}