using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class EditQuestionPage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _testId;
        private Question _question;
        private List<AnswerOption> _answers;

        public ObservableCollection<AnswerOption> AnswerOptions { get; set; } = new();

        public EditQuestionPage(User user, DatabaseService dbService, SettingsService settingsService, int testId, Question? existingQuestion = null)
        {
            InitializeComponent();
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _testId = testId;
            _question = existingQuestion ?? new Question { TestId = testId };
            _answers = existingQuestion?.AnswerOptions ?? new List<AnswerOption>();

            BindingContext = this;
            AnswersCollection.ItemsSource = AnswerOptions;
            QuestionTypePicker.SelectedIndexChanged += OnQuestionTypeChanged;

            // Загружаем данные если редактируем существующий вопрос
            if (existingQuestion != null)
            {
                LoadQuestionData();
            }
            else
            {
                QuestionTypePicker.SelectedIndex = 0;
            }

            // Инициализируем видимость секции ответов
            OnQuestionTypeChanged(null, null);
        }

        private void LoadQuestionData()
        {
            QuestionTextEditor.Text = _question.QuestionText;

            // Устанавливаем тип вопроса
            var index = QuestionTypePicker.Items.IndexOf(_question.QuestionType);
            if (index >= 0)
                QuestionTypePicker.SelectedIndex = index;

            ScoreEntry.Text = _question.Score.ToString();

            // Загружаем варианты ответов
            AnswerOptions.Clear();
            foreach (var answer in _question.AnswerOptions)
            {
                AnswerOptions.Add(new AnswerOption
                {
                    AnswerId = answer.AnswerId,
                    AnswerText = answer.AnswerText,
                    IsCorrect = answer.IsCorrect
                });
            }
        }

        private void OnQuestionTypeChanged(object sender, EventArgs e)
        {
            var type = QuestionTypePicker.SelectedItem as string;
            AnswerOptionsSection.IsVisible = type != "text" && type != "code";
        }

        private void OnAddAnswerClicked(object sender, EventArgs e)
        {
            AnswerOptions.Add(new AnswerOption { AnswerText = "", IsCorrect = false });
        }

        private void OnDeleteAnswerClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is AnswerOption answer)
            {
                AnswerOptions.Remove(answer);
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(QuestionTextEditor.Text))
                {
                    await DisplayAlert("Ошибка", "Введите текст вопроса", "OK");
                    return;
                }

                var questionType = QuestionTypePicker.SelectedItem as string ?? "single";

                // Проверяем варианты ответов для типов с выбором
                if ((questionType == "single" || questionType == "multiple") && !AnswerOptions.Any())
                {
                    await DisplayAlert("Ошибка", "Добавьте варианты ответов", "OK");
                    return;
                }

                if ((questionType == "single" || questionType == "multiple") && !AnswerOptions.Any(a => a.IsCorrect))
                {
                    await DisplayAlert("Ошибка", "Отметьте хотя бы один правильный ответ", "OK");
                    return;
                }

                if (!int.TryParse(ScoreEntry.Text, out int score) || score <= 0)
                {
                    await DisplayAlert("Ошибка", "Введите корректное количество баллов", "OK");
                    return;
                }

                // Обновляем данные вопроса
                _question.QuestionText = QuestionTextEditor.Text.Trim();
                _question.QuestionType = questionType;
                _question.Score = score;

                bool success;

                if (_question.QuestionId == 0) // Новый вопрос
                {
                    // Добавляем вопрос
                    var questionId = await _dbService.AddQuestionAsync(_testId, _question.QuestionText, _question.QuestionType, _question.Score, 1);
                    if (questionId == null)
                    {
                        await DisplayAlert("Ошибка", "Не удалось создать вопрос", "OK");
                        return;
                    }
                    _question.QuestionId = questionId.Value;

                    // Добавляем варианты ответов
                    foreach (var answer in AnswerOptions)
                    {
                        if (!string.IsNullOrWhiteSpace(answer.AnswerText))
                        {
                            await _dbService.AddAnswerOptionAsync(_question.QuestionId, answer.AnswerText.Trim(), answer.IsCorrect);
                        }
                    }
                    success = true;
                }
                else // Редактирование существующего вопроса
                {
                    // Обновляем вопрос
                    success = await _dbService.UpdateQuestionAsync(_question.QuestionId, _question.QuestionText, _question.QuestionType, _question.Score);

                    if (success)
                    {
                        // Удаляем старые варианты ответов и добавляем новые
                        await _dbService.DeleteQuestionAsync(_question.QuestionId); // Это удалит и варианты ответов

                        // Добавляем новые варианты ответов
                        foreach (var answer in AnswerOptions)
                        {
                            if (!string.IsNullOrWhiteSpace(answer.AnswerText))
                            {
                                await _dbService.AddAnswerOptionAsync(_question.QuestionId, answer.AnswerText.Trim(), answer.IsCorrect);
                            }
                        }
                    }
                }

                if (success)
                {
                    await DisplayAlert("Успех", "Вопрос успешно сохранен", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось сохранить вопрос", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка сохранения: {ex.Message}", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}