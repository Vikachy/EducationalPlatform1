using EducationalPlatform.Models;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class CreateQuestionPage : ContentPage
    {
        private readonly ObservableCollection<ExtendedQuestionCreationModel> _questions;
        private readonly ExtendedQuestionCreationModel _editingQuestion;
        private readonly bool _isEditing;
        private readonly Action<ExtendedQuestionCreationModel> _onQuestionSaved;

        public ObservableCollection<ExtendedAnswerOptionModel> AnswerOptions { get; set; } = new();

        public CreateQuestionPage(ObservableCollection<ExtendedQuestionCreationModel> questions,
                                ExtendedQuestionCreationModel? question = null,
                                Action<ExtendedQuestionCreationModel>? onQuestionSaved = null)
        {
            InitializeComponent();
            _questions = questions;
            _onQuestionSaved = onQuestionSaved;

            if (question != null)
            {
                _isEditing = true;
                _editingQuestion = question;
                LoadQuestionData();
            }
            else
            {
                QuestionTypePicker.SelectedIndex = 0;
            }

            BindingContext = this;
            AnswersCollection.ItemsSource = AnswerOptions;
            QuestionTypePicker.SelectedIndexChanged += OnQuestionTypeChanged;

            OnQuestionTypeChanged(null, null);
        }

        private void LoadQuestionData()
        {
            QuestionEditor.Text = _editingQuestion.QuestionText;
            QuestionTypePicker.SelectedItem = _editingQuestion.QuestionType;
            ScoreEntry.Text = _editingQuestion.Score.ToString();

            foreach (var answer in _editingQuestion.AnswerOptions)
            {
                AnswerOptions.Add(new ExtendedAnswerOptionModel
                {
                    AnswerText = answer.AnswerText,
                    IsCorrect = answer.IsCorrect
                });
            }
        }

        private void OnQuestionTypeChanged(object sender, EventArgs e)
        {
            var type = QuestionTypePicker.SelectedItem as string;
            AnswerOptionsSection.IsVisible = type != "text";
        }

        private void OnAddAnswerClicked(object sender, EventArgs e)
        {
            AnswerOptions.Add(new ExtendedAnswerOptionModel());
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
                if (string.IsNullOrWhiteSpace(QuestionEditor.Text))
                {
                    await DisplayAlert("������", "������� ����� �������", "OK");
                    return;
                }

                var questionType = QuestionTypePicker.SelectedItem as string ?? "single";

                if ((questionType == "single" || questionType == "multiple") && !AnswerOptions.Any())
                {
                    await DisplayAlert("������", "�������� �������� �������", "OK");
                    return;
                }

                if ((questionType == "single" || questionType == "multiple") && !AnswerOptions.Any(a => a.IsCorrect))
                {
                    await DisplayAlert("������", "�������� ���� �� ���� ���������� �����", "OK");
                    return;
                }

                var question = new ExtendedQuestionCreationModel
                {
                    QuestionText = QuestionEditor.Text.Trim(),
                    QuestionType = questionType,
                    Score = int.TryParse(ScoreEntry.Text, out int score) ? score : 1
                };

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

                if (_isEditing)
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

                // �������� callback ���� �� ����
                _onQuestionSaved?.Invoke(question);

                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"������ ����������: {ex.Message}", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}