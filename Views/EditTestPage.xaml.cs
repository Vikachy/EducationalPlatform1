using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class EditTestPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _lessonId;
        private int _testId;

        // Элементы управления
        private Entry? _testTitleEntry;
        private Editor? _testDescriptionEditor;
        private Entry? _timeLimitEntry;
        private Entry? _passingScoreEntry;
        private CollectionView? _questionsCollection;

        public ObservableCollection<Question> Questions { get; set; } = new();

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

        public EditTestPage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации EditTestPage: {ex.Message}");
            }

            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _lessonId = lessonId;

            // Инициализируем элементы управления
            _testTitleEntry = this.FindByName<Entry>("TestTitleEntry");
            _testDescriptionEditor = this.FindByName<Editor>("TestDescriptionEditor");
            _timeLimitEntry = this.FindByName<Entry>("TimeLimitEntry");
            _passingScoreEntry = this.FindByName<Entry>("PassingScoreEntry");
            _questionsCollection = this.FindByName<CollectionView>("QuestionsCollection");

            BindingContext = this;

            // Подписываемся на событие появления страницы
            this.Appearing += OnPageAppearing;
        }

        private async void OnPageAppearing(object? sender, EventArgs e)
        {
            await LoadTestDataAsync();
        }

        private async Task LoadTestDataAsync()
        {
            try
            {
                // Получаем метаданные теста
                var testMeta = await _dbService.GetTestMetaByLessonAsync(_lessonId);
                if (testMeta != null)
                {
                    _testId = testMeta.TestId;
                    TestTitle = testMeta.Title;
                    TestDescription = testMeta.Description ?? "";
                    TimeLimit = testMeta.TimeLimitMinutes;
                    PassingScore = testMeta.PassingScore;

                    // Загружаем вопросы теста
                    var questions = await _dbService.GetTestQuestionsAsync(_testId);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Questions.Clear();
                        foreach (var question in questions)
                        {
                            Questions.Add(question);
                        }

                        if (_questionsCollection != null)
                        {
                            _questionsCollection.ItemsSource = null;
                            _questionsCollection.ItemsSource = Questions;
                        }

                        Console.WriteLine($"✅ Загружено {Questions.Count} вопросов для теста {_testId}");
                    });
                }
                else
                {
                    await DisplayAlert("Ошибка", "Тест не найден", "OK");
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные теста: {ex.Message}", "OK");
            }
        }

        private async void OnAddQuestionClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EditQuestionPage(_user, _dbService, _settingsService, _testId));
        }

        private async void OnEditQuestionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Question question)
            {
                await Navigation.PushAsync(new EditQuestionPage(_user, _dbService, _settingsService, _testId, question));
            }
        }

        private async void OnDeleteQuestionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Question question)
            {
                var result = await DisplayAlert("Подтверждение",
                    $"Вы уверены, что хотите удалить вопрос '{question.QuestionText}'?",
                    "Да", "Нет");

                if (result)
                {
                    var success = await _dbService.DeleteQuestionAsync(question.QuestionId);
                    if (success)
                    {
                        Questions.Remove(question);
                        await DisplayAlert("Успех", "Вопрос удален", "OK");

                        if (_questionsCollection != null)
                        {
                            _questionsCollection.ItemsSource = null;
                            _questionsCollection.ItemsSource = Questions;
                        }
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

                // Обновляем тест
                var success = await _dbService.UpdateTestMetaAsync(
                    _testId,
                    title.Trim(),
                    description?.Trim() ?? "",
                    timeLimit,
                    passingScore
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

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            this.Appearing -= OnPageAppearing;
        }
    }
}