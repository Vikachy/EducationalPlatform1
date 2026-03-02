using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class CreateTestPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _courseId;

        // Элементы управления
        private Entry? _testTitleEntry;
        private Editor? _testDescriptionEditor;
        private Entry? _timeLimitEntry;
        private Entry? _passingScoreEntry;
        private CollectionView? _questionsCollectionView;

        public ObservableCollection<ExtendedQuestionCreationModel> Questions { get; set; } = new();

        private string _testTitle = string.Empty;
        public string TestTitle
        {
            get => _testTitle;
            set
            {
                _testTitle = value;
                OnPropertyChanged();
            }
        }

        private string _testDescription = string.Empty;
        public string TestDescription
        {
            get => _testDescription;
            set
            {
                _testDescription = value;
                OnPropertyChanged();
            }
        }

        private int _timeLimit = 60;
        public int TimeLimit
        {
            get => _timeLimit;
            set
            {
                _timeLimit = value;
                OnPropertyChanged();
            }
        }

        private int _passingScore = 60;
        public int PassingScore
        {
            get => _passingScore;
            set
            {
                _passingScore = value;
                OnPropertyChanged();
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CreateTestPage(User user, DatabaseService dbService, SettingsService settingsService, int courseId)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации CreateTestPage: {ex.Message}");
            }

            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseId = courseId;

            // Инициализируем элементы управления
            _testTitleEntry = this.FindByName<Entry>("TestTitleEntry");
            _testDescriptionEditor = this.FindByName<Editor>("TestDescriptionEditor");
            _timeLimitEntry = this.FindByName<Entry>("TimeLimitEntry");
            _passingScoreEntry = this.FindByName<Entry>("PassingScoreEntry");
            _questionsCollectionView = this.FindByName<CollectionView>("QuestionsCollectionView");

            BindingContext = this;

            if (_questionsCollectionView != null)
                _questionsCollectionView.ItemsSource = Questions;

            // Подписываемся на событие появления страницы
            this.Appearing += OnPageAppearing;
        }

        private void OnPageAppearing(object? sender, EventArgs e)
        {
            // Обновляем отображение вопросов при возвращении на страницу
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_questionsCollectionView != null)
                {
                    _questionsCollectionView.ItemsSource = null;
                    _questionsCollectionView.ItemsSource = Questions;
                }
                OnPropertyChanged(nameof(Questions));

                Console.WriteLine($"🔄 Страница CreateTestPage обновлена. Вопросов: {Questions.Count}");
            });
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
            Console.WriteLine($"✅ Вопрос сохранен: {question.QuestionText}");
        }

        private void OnRemoveQuestionClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ExtendedQuestionCreationModel question)
            {
                Questions.Remove(question);

                // Обновляем отображение
                if (_questionsCollectionView != null)
                {
                    _questionsCollectionView.ItemsSource = null;
                    _questionsCollectionView.ItemsSource = Questions;
                }
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
                // Получаем значения из Entry
                string title = _testTitleEntry?.Text ?? TestTitle;
                string description = _testDescriptionEditor?.Text ?? TestDescription;

                int timeLimit = 60;
                if (_timeLimitEntry != null && int.TryParse(_timeLimitEntry.Text, out int tl))
                    timeLimit = tl;
                else
                    timeLimit = TimeLimit;

                int passingScore = 60;
                if (_passingScoreEntry != null && int.TryParse(_passingScoreEntry.Text, out int ps))
                    passingScore = ps;
                else
                    passingScore = PassingScore;

                if (string.IsNullOrWhiteSpace(title))
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

                // Вызываем метод DatabaseService
                var lessonId = await _dbService.CreateTestWithQuestionsAsync(
                    _courseId,
                    title.Trim(),
                    description?.Trim() ?? "",
                    timeLimit,
                    passingScore,
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

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            this.Appearing -= OnPageAppearing;
        }
    }
}