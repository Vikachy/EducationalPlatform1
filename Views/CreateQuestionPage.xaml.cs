using EducationalPlatform.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class CreateQuestionPage : ContentPage, INotifyPropertyChanged
    {
        private readonly ObservableCollection<ExtendedQuestionCreationModel> _questions;
        private readonly ExtendedQuestionCreationModel? _editingQuestion;
        private readonly bool _isEditing;
        private readonly Action<ExtendedQuestionCreationModel>? _onQuestionSaved;

        // Элементы управления
        private Editor? _questionEditor;
        private Picker? _questionTypePicker;
        private Entry? _scoreEntry;
        private CollectionView? _answersCollection;
        private Border? _answerOptionsSection;
        private Border? _textAnswerSection;
        private Editor? _textCorrectAnswerEditor;

        public ObservableCollection<ExtendedAnswerOptionModel> AnswerOptions { get; set; } = new();

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CreateQuestionPage(ObservableCollection<ExtendedQuestionCreationModel> questions,
                                ExtendedQuestionCreationModel? question = null,
                                Action<ExtendedQuestionCreationModel>? onQuestionSaved = null)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
            }

            _questions = questions;
            _onQuestionSaved = onQuestionSaved;

            // Инициализируем элементы управления
            _questionEditor = this.FindByName<Editor>("QuestionEditor");
            _questionTypePicker = this.FindByName<Picker>("QuestionTypePicker");
            _scoreEntry = this.FindByName<Entry>("ScoreEntry");
            _answersCollection = this.FindByName<CollectionView>("AnswersCollection");
            _answerOptionsSection = this.FindByName<Border>("AnswerOptionsSection");
            _textAnswerSection = this.FindByName<Border>("TextAnswerSection");
            _textCorrectAnswerEditor = this.FindByName<Editor>("TextCorrectAnswerEditor");

            if (question != null)
            {
                _isEditing = true;
                _editingQuestion = question;
                LoadQuestionData();
            }
            else
            {
                if (_questionTypePicker != null)
                    _questionTypePicker.SelectedIndex = 0;
            }

            BindingContext = this;

            if (_answersCollection != null)
                _answersCollection.ItemsSource = AnswerOptions;

            if (_questionTypePicker != null)
                _questionTypePicker.SelectedIndexChanged += OnQuestionTypeChanged;

            OnQuestionTypeChanged(null, EventArgs.Empty);
        }

        private void LoadQuestionData()
        {
            if (_editingQuestion == null) return;

            if (_questionEditor != null)
                _questionEditor.Text = _editingQuestion.QuestionText;

            if (_questionTypePicker != null)
            {
                var index = _questionTypePicker.Items.IndexOf(_editingQuestion.QuestionType);
                if (index >= 0)
                    _questionTypePicker.SelectedIndex = index;
            }

            if (_scoreEntry != null)
                _scoreEntry.Text = _editingQuestion.Score.ToString();

            // Загружаем варианты ответов
            AnswerOptions.Clear();
            foreach (var answer in _editingQuestion.AnswerOptions)
            {
                AnswerOptions.Add(new ExtendedAnswerOptionModel
                {
                    AnswerText = answer.AnswerText,
                    IsCorrect = answer.IsCorrect
                });
            }

            // Для текстового ответа загружаем правильный ответ
            if (_editingQuestion.QuestionType == "text" && _editingQuestion.AnswerOptions.Count == 1)
            {
                if (_textCorrectAnswerEditor != null)
                    _textCorrectAnswerEditor.Text = _editingQuestion.AnswerOptions[0].AnswerText;
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
            AnswerOptions.Add(new ExtendedAnswerOptionModel
            {
                AnswerText = "",
                IsCorrect = false
            });
        }

        private void OnDeleteAnswerClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ExtendedAnswerOptionModel answer)
            {
                AnswerOptions.Remove(answer);
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_questionEditor?.Text))
                {
                    await DisplayAlert("Ошибка", "Введите текст вопроса", "OK");
                    return;
                }

                if (!int.TryParse(_scoreEntry?.Text, out int score) || score <= 0)
                {
                    score = 1;
                }

                var questionType = _questionTypePicker?.SelectedItem as string ?? "single";

                // Создаем вопрос
                var question = new ExtendedQuestionCreationModel
                {
                    QuestionText = _questionEditor.Text.Trim(),
                    QuestionType = questionType,
                    Score = score
                };

                // Для типа "text" добавляем правильный ответ как вариант ответа
                if (questionType == "text")
                {
                    var correctAnswer = _textCorrectAnswerEditor?.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(correctAnswer))
                    {
                        await DisplayAlert("Ошибка", "Введите правильный ответ", "OK");
                        return;
                    }

                    question.AnswerOptions.Add(new ExtendedAnswerOptionModel
                    {
                        AnswerText = correctAnswer,
                        IsCorrect = true
                    });
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
                            question.AnswerOptions.Add(new ExtendedAnswerOptionModel
                            {
                                AnswerText = answer.AnswerText.Trim(),
                                IsCorrect = answer.IsCorrect
                            });
                        }
                    }
                }

                // Обновляем или добавляем вопрос
                if (_isEditing && _editingQuestion != null)
                {
                    var index = _questions.IndexOf(_editingQuestion);
                    if (index >= 0)
                    {
                        _questions[index] = question;
                    }
                }
                else
                {
                    _questions.Add(question);
                }

                _onQuestionSaved?.Invoke(question);
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка сохранения: {ex.Message}", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}