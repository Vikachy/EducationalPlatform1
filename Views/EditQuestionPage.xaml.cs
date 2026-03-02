using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class EditQuestionPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _testId;
        private readonly Question? _existingQuestion;

        // Элементы управления
        private Editor? _questionTextEditor;
        private Picker? _questionTypePicker;
        private Entry? _scoreEntry;
        private CollectionView? _answersCollection;
        private Border? _answerOptionsSection;
        private Border? _textAnswerSection;
        private Editor? _textCorrectAnswerEditor;

        public ObservableCollection<AnswerOptionModel> AnswerOptions { get; set; } = new();

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public EditQuestionPage(User user, DatabaseService dbService, SettingsService settingsService, int testId, Question? existingQuestion = null)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации EditQuestionPage: {ex.Message}");
            }

            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _testId = testId;
            _existingQuestion = existingQuestion;

            // Инициализируем элементы управления
            _questionTextEditor = this.FindByName<Editor>("QuestionTextEditor");
            _questionTypePicker = this.FindByName<Picker>("QuestionTypePicker");
            _scoreEntry = this.FindByName<Entry>("ScoreEntry");
            _answersCollection = this.FindByName<CollectionView>("AnswersCollection");
            _answerOptionsSection = this.FindByName<Border>("AnswerOptionsSection");
            _textAnswerSection = this.FindByName<Border>("TextAnswerSection");
            _textCorrectAnswerEditor = this.FindByName<Editor>("TextCorrectAnswerEditor");

            BindingContext = this;

            if (_answersCollection != null)
                _answersCollection.ItemsSource = AnswerOptions;

            if (_questionTypePicker != null)
                _questionTypePicker.SelectedIndexChanged += OnQuestionTypeChanged;

            // Загружаем данные если редактируем существующий вопрос
            if (existingQuestion != null)
            {
                LoadQuestionData();
            }
            else
            {
                if (_questionTypePicker != null)
                    _questionTypePicker.SelectedIndex = 0;
            }

            OnQuestionTypeChanged(null, EventArgs.Empty);
        }

        private void LoadQuestionData()
        {
            if (_existingQuestion == null) return;

            if (_questionTextEditor != null)
                _questionTextEditor.Text = _existingQuestion.QuestionText;

            if (_questionTypePicker != null)
            {
                var index = _questionTypePicker.Items.IndexOf(_existingQuestion.QuestionType);
                if (index >= 0)
                    _questionTypePicker.SelectedIndex = index;
            }

            if (_scoreEntry != null)
                _scoreEntry.Text = _existingQuestion.Score.ToString();

            // Загружаем варианты ответов
            AnswerOptions.Clear();
            foreach (var answer in _existingQuestion.AnswerOptions)
            {
                AnswerOptions.Add(new AnswerOptionModel
                {
                    AnswerId = answer.AnswerId,
                    AnswerText = answer.AnswerText,
                    IsCorrect = answer.IsCorrect
                });
            }

            // Для текстового ответа загружаем правильный ответ
            if (_existingQuestion.QuestionType == "text" && _existingQuestion.AnswerOptions.Count == 1)
            {
                if (_textCorrectAnswerEditor != null)
                    _textCorrectAnswerEditor.Text = _existingQuestion.AnswerOptions[0].AnswerText;
            }
        }

        private void OnQuestionTypeChanged(object sender, EventArgs e)
        {
            var type = _questionTypePicker?.SelectedItem as string;

            // Для текстового ответа скрываем варианты и показываем поле для правильного ответа
            if (_answerOptionsSection != null)
                _answerOptionsSection.IsVisible = type != "text";

            if (_textAnswerSection != null)
                _textAnswerSection.IsVisible = type == "text";

            // Если тип "text", очищаем варианты ответов
            if (type == "text")
            {
                AnswerOptions.Clear();
            }
        }

        private void OnAddAnswerClicked(object sender, EventArgs e)
        {
            AnswerOptions.Add(new AnswerOptionModel
            {
                AnswerText = "",
                IsCorrect = false
            });
        }

        private void OnDeleteAnswerClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is AnswerOptionModel answer)
            {
                AnswerOptions.Remove(answer);
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_questionTextEditor?.Text))
                {
                    await DisplayAlert("Ошибка", "Введите текст вопроса", "OK");
                    return;
                }

                if (!int.TryParse(_scoreEntry?.Text, out int score) || score <= 0)
                {
                    score = 1;
                }

                var questionType = _questionTypePicker?.SelectedItem as string ?? "single";

                bool success;

                if (_existingQuestion == null) // Новый вопрос
                {
                    // Создаем вопрос
                    var questionId = await _dbService.AddQuestionAsync(_testId, _questionTextEditor.Text.Trim(), questionType, score, 1);

                    if (!questionId.HasValue)
                    {
                        await DisplayAlert("Ошибка", "Не удалось создать вопрос", "OK");
                        return;
                    }

                    // Для типа "text" добавляем правильный ответ как вариант ответа
                    if (questionType == "text")
                    {
                        var correctAnswer = _textCorrectAnswerEditor?.Text?.Trim();
                        if (string.IsNullOrWhiteSpace(correctAnswer))
                        {
                            await DisplayAlert("Ошибка", "Введите правильный ответ", "OK");
                            return;
                        }

                        await _dbService.AddAnswerOptionAsync(questionId.Value, correctAnswer, true);
                        success = true;
                    }
                    else
                    {
                        // Проверка для типов с вариантами ответов
                        if (!AnswerOptions.Any())
                        {
                            await DisplayAlert("Ошибка", "Добавьте варианты ответов", "OK");
                            return;
                        }

                        // Проверяем, что все варианты имеют текст
                        var emptyAnswers = AnswerOptions.Where(a => string.IsNullOrWhiteSpace(a.AnswerText)).ToList();
                        if (emptyAnswers.Any())
                        {
                            await DisplayAlert("Ошибка", "Заполните текст для всех вариантов ответов", "OK");
                            return;
                        }

                        if (!AnswerOptions.Any(a => a.IsCorrect))
                        {
                            await DisplayAlert("Ошибка", "Отметьте хотя бы один правильный ответ", "OK");
                            return;
                        }

                        // Для single убеждаемся, что только один правильный ответ
                        if (questionType == "single")
                        {
                            var correctCount = AnswerOptions.Count(a => a.IsCorrect);
                            if (correctCount > 1)
                            {
                                await DisplayAlert("Ошибка", "Для одиночного выбора должен быть только один правильный ответ", "OK");
                                return;
                            }
                        }

                        // Добавляем варианты ответов
                        foreach (var answer in AnswerOptions)
                        {
                            if (!string.IsNullOrWhiteSpace(answer.AnswerText))
                            {
                                await _dbService.AddAnswerOptionAsync(questionId.Value, answer.AnswerText.Trim(), answer.IsCorrect);
                            }
                        }
                        success = true;
                    }
                }
                else // Редактирование существующего вопроса
                {
                    // Обновляем вопрос
                    success = await _dbService.UpdateQuestionAsync(_existingQuestion.QuestionId, _questionTextEditor.Text.Trim(), questionType, score);

                    if (success)
                    {
                        // Удаляем старые варианты ответов
                        await DeleteAnswersByQuestionId(_existingQuestion.QuestionId);

                        // Для типа "text" добавляем правильный ответ как вариант ответа
                        if (questionType == "text")
                        {
                            var correctAnswer = _textCorrectAnswerEditor?.Text?.Trim();
                            if (string.IsNullOrWhiteSpace(correctAnswer))
                            {
                                await DisplayAlert("Ошибка", "Введите правильный ответ", "OK");
                                return;
                            }

                            await _dbService.AddAnswerOptionAsync(_existingQuestion.QuestionId, correctAnswer, true);
                        }
                        else
                        {
                            // Проверка для типов с вариантами ответов
                            if (!AnswerOptions.Any())
                            {
                                await DisplayAlert("Ошибка", "Добавьте варианты ответов", "OK");
                                return;
                            }

                            // Проверяем, что все варианты имеют текст
                            var emptyAnswers = AnswerOptions.Where(a => string.IsNullOrWhiteSpace(a.AnswerText)).ToList();
                            if (emptyAnswers.Any())
                            {
                                await DisplayAlert("Ошибка", "Заполните текст для всех вариантов ответов", "OK");
                                return;
                            }

                            if (!AnswerOptions.Any(a => a.IsCorrect))
                            {
                                await DisplayAlert("Ошибка", "Отметьте хотя бы один правильный ответ", "OK");
                                return;
                            }

                            // Для single убеждаемся, что только один правильный ответ
                            if (questionType == "single")
                            {
                                var correctCount = AnswerOptions.Count(a => a.IsCorrect);
                                if (correctCount > 1)
                                {
                                    await DisplayAlert("Ошибка", "Для одиночного выбора должен быть только один правильный ответ", "OK");
                                    return;
                                }
                            }

                            // Добавляем новые варианты ответов
                            foreach (var answer in AnswerOptions)
                            {
                                if (!string.IsNullOrWhiteSpace(answer.AnswerText))
                                {
                                    await _dbService.AddAnswerOptionAsync(_existingQuestion.QuestionId, answer.AnswerText.Trim(), answer.IsCorrect);
                                }
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

        private async Task DeleteAnswersByQuestionId(int questionId)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = "DELETE FROM AnswerOptions WHERE QuestionId = @QuestionId";
                await connection.ExecuteAsync(query, new { QuestionId = questionId });

                Console.WriteLine($"✅ Удалены старые варианты ответов для вопроса {questionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка удаления вариантов ответов: {ex.Message}");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    // Модель для вариантов ответа
    public class AnswerOptionModel : INotifyPropertyChanged
    {
        private int _answerId;
        public int AnswerId
        {
            get => _answerId;
            set { _answerId = value; OnPropertyChanged(); }
        }

        private string _answerText = string.Empty;
        public string AnswerText
        {
            get => _answerText;
            set { _answerText = value; OnPropertyChanged(); }
        }

        private bool _isCorrect;
        public bool IsCorrect
        {
            get => _isCorrect;
            set { _isCorrect = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}