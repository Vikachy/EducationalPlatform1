using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class CreateTestPage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _courseId;

        public ObservableCollection<ExtendedQuestionCreationModel> Questions { get; set; } = new();

        // Свойства для привязки данных
        public string TestTitle { get; set; }
        public string TestDescription { get; set; }
        public int TimeLimit { get; set; } = 60;
        public int PassingScore { get; set; } = 60;

        public CreateTestPage(User user, DatabaseService dbService, SettingsService settingsService, int courseId)
        {
            InitializeComponent();
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseId = courseId;

            BindingContext = this;
        }

        private async void OnAddQuestionClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new CreateQuestionPage(
                Questions,
                null,
                OnQuestionSaved
            ));
        }

        private void OnQuestionSaved(ExtendedQuestionCreationModel question)
        {
            // Вопрос уже добавлен в коллекцию через CreateQuestionPage
            // Обновляем отображение
            OnPropertyChanged(nameof(Questions));
        }

        private void OnRemoveQuestionClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ExtendedQuestionCreationModel question)
            {
                Questions.Remove(question);
            }
        }

        private async void OnEditQuestionClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ExtendedQuestionCreationModel question)
            {
                await Navigation.PushModalAsync(new CreateQuestionPage(
                    Questions,
                    question,
                    OnQuestionSaved
                ));
            }
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TestTitle))
                {
                    await DisplayAlert("Ошибка", "Введите название теста", "OK");
                    return;
                }

                if (!Questions.Any())
                {
                    await DisplayAlert("Ошибка", "Добавьте хотя бы один вопрос", "OK");
                    return;
                }

                // Преобразуем ExtendedQuestionCreationModel в QuestionCreationModel для базы данных
                var questionsForDb = new List<QuestionCreationModel>();
                foreach (var question in Questions)
                {
                    var dbQuestion = new QuestionCreationModel
                    {
                        QuestionText = question.QuestionText,
                        QuestionType = question.QuestionType,
                        Score = question.Score,
                        AnswerOptions = new List<AnswerOptionCreationModel>()
                    };

                    foreach (var answer in question.AnswerOptions)
                    {
                        dbQuestion.AnswerOptions.Add(new AnswerOptionCreationModel
                        {
                            AnswerText = answer.AnswerText,
                            IsCorrect = answer.IsCorrect
                        });
                    }

                    questionsForDb.Add(dbQuestion);
                }

                var lessonId = await _dbService.CreateTestWithQuestionsAsync(
                    _courseId,
                    TestTitle.Trim(),
                    TestDescription?.Trim() ?? "",
                    TimeLimit,
                    PassingScore,
                    questionsForDb
                );

                if (lessonId.HasValue)
                {
                    await DisplayAlert("Успех", "Тест создан!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось создать тест", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка создания: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Обновляем отображение при возвращении на страницу
            OnPropertyChanged(nameof(Questions));
        }
    }
}