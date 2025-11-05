using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Timers;

namespace EducationalPlatform.Views
{
    public partial class TestStudyPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _lessonId;
        private int _courseId;
        private int _testId;
        private List<Question> _questions;
        private int _currentQuestionIndex = 0;
        private Dictionary<int, object> _userAnswers = new();
        private Dictionary<CheckBox, int> _checkBoxAnswerMap = new(); // Связь CheckBox -> AnswerId
        private System.Timers.Timer _timer;
        private TimeSpan _timeLeft;
        private int _attemptId;

        public TestStudyPage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _lessonId = lessonId;

            StartTest();
        }

        private async void StartTest()
        {
            try
            {
                // Получаем информацию о тесте
                var testMeta = await _dbService.GetTestMetaByLessonAsync(_lessonId);
                if (testMeta != null)
                {
                    _testId = testMeta.TestId;
                    TitleLabel.Text = testMeta.Title;

                    // Получаем вопросы теста
                    _questions = await GetTestQuestionsAsync(_testId);

                    if (!_questions.Any())
                    {
                        await DisplayAlert("Ошибка", "В тесте нет вопросов", "OK");
                        await Navigation.PopAsync();
                        return;
                    }

                    // Получаем ID курса
                    var courseId = await GetCourseIdByLessonAsync(_lessonId);
                    if (courseId.HasValue) _courseId = courseId.Value;

                    // Начинаем попытку теста
                    _attemptId = await StartTestAttemptAsync(_testId, _currentUser.UserId, null) ?? 0;

                    // Запускаем таймер
                    StartTimer(testMeta.TimeLimitMinutes);

                    // Показываем первый вопрос
                    ShowQuestion(0);
                }
                else
                {
                    await DisplayAlert("Ошибка", "Тест не найден", "OK");
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось начать тест: {ex.Message}", "OK");
                await Navigation.PopAsync();
            }
        }

        private void StartTimer(int timeLimitMinutes)
        {
            // Добавляем проверку на корректное значение времени
            if (timeLimitMinutes <= 0)
            {
                timeLimitMinutes = 30; // значение по умолчанию
            }

            _timeLeft = TimeSpan.FromMinutes(timeLimitMinutes);
            UpdateTimerDisplay();

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _timeLeft = _timeLeft.Subtract(TimeSpan.FromSeconds(1));

                if (_timeLeft.TotalSeconds <= 0)
                {
                    _timer.Stop();
                    FinishTest(); // Автоматическое завершение при истечении времени
                }
                else
                {
                    UpdateTimerDisplay();
                }
            });
        }

        private void UpdateTimerDisplay()
        {
            TimerLabel.Text = $"Время: {_timeLeft:mm\\:ss}";
            TimeLeftLabel.Text = $"{_timeLeft:mm\\:ss}";
        }

        private void ShowQuestion(int questionIndex)
        {
            if (_questions == null || questionIndex < 0 || questionIndex >= _questions.Count)
                return;

            _currentQuestionIndex = questionIndex;
            var question = _questions[questionIndex];

            // Обновляем прогресс
            ProgressLabel.Text = $"{questionIndex + 1} из {_questions.Count}";

            // Показываем текст вопроса
            QuestionTextLabel.Text = question.QuestionText;

            // Очищаем предыдущие варианты ответов
            AnswerOptionsStack.Children.Clear();
            TextAnswerSection.IsVisible = false;
            _checkBoxAnswerMap.Clear(); // Очищаем карту связей

            // Показываем варианты ответов в зависимости от типа вопроса
            switch (question.QuestionType.ToLower())
            {
                case "single":
                    ShowSingleChoiceOptions(question);
                    break;
                case "multiple":
                    ShowMultipleChoiceOptions(question);
                    break;
                case "text":
                    ShowTextAnswerOption(question);
                    break;
            }

            // Восстанавливаем сохраненный ответ
            RestoreUserAnswer(question.QuestionId);

            // Обновляем кнопки навигации
            UpdateNavigationButtons();
        }

        private void ShowSingleChoiceOptions(Question question)
        {
            foreach (var option in question.AnswerOptions)
            {
                var frame = new Frame
                {
                    BackgroundColor = Colors.Transparent,
                    BorderColor = Colors.LightGray,
                    Padding = 10,
                    Margin = new Thickness(0, 5)
                };

                var radioButton = new RadioButton
                {
                    Content = option.AnswerText,
                    Value = option.AnswerId,
                    FontSize = 16
                };
                radioButton.CheckedChanged += (s, e) => OnSingleAnswerSelected(option.AnswerId, e.Value);

                frame.Content = radioButton;
                AnswerOptionsStack.Children.Add(frame);
            }
        }

        private void ShowMultipleChoiceOptions(Question question)
        {
            foreach (var option in question.AnswerOptions)
            {
                var frame = new Frame
                {
                    BackgroundColor = Colors.Transparent,
                    BorderColor = Colors.LightGray,
                    Padding = 10,
                    Margin = new Thickness(0, 5)
                };

                var checkBox = new CheckBox
                {
                    IsChecked = false
                };

                var label = new Label
                {
                    Text = option.AnswerText,
                    FontSize = 16,
                    VerticalOptions = LayoutOptions.Center
                };

                var stackLayout = new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children = { checkBox, label }
                };

                // Сохраняем связь между CheckBox и AnswerId
                _checkBoxAnswerMap[checkBox] = option.AnswerId;

                checkBox.CheckedChanged += (s, e) => OnMultipleAnswerSelected(option.AnswerId, e.Value);

                frame.Content = stackLayout;
                AnswerOptionsStack.Children.Add(frame);
            }
        }

        private void ShowTextAnswerOption(Question question)
        {
            TextAnswerSection.IsVisible = true;
            TextAnswerEditor.Text = "";
        }

        private void RestoreUserAnswer(int questionId)
        {
            if (_userAnswers.ContainsKey(questionId))
            {
                var savedAnswer = _userAnswers[questionId];

                if (savedAnswer is int singleAnswer)
                {
                    // Восстанавливаем одиночный выбор
                    foreach (var child in AnswerOptionsStack.Children)
                    {
                        if (child is Frame frame && frame.Content is RadioButton radioButton)
                        {
                            if ((int)radioButton.Value == singleAnswer)
                            {
                                radioButton.IsChecked = true;
                                break;
                            }
                        }
                    }
                }
                else if (savedAnswer is List<int> multipleAnswers)
                {
                    // Восстанавливаем множественный выбор
                    foreach (var child in AnswerOptionsStack.Children)
                    {
                        if (child is Frame frame && frame.Content is HorizontalStackLayout stackLayout)
                        {
                            if (stackLayout.Children[0] is CheckBox checkBox)
                            {
                                if (_checkBoxAnswerMap.TryGetValue(checkBox, out int answerId))
                                {
                                    if (multipleAnswers.Contains(answerId))
                                    {
                                        checkBox.IsChecked = true;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (savedAnswer is string textAnswer)
                {
                    TextAnswerEditor.Text = textAnswer;
                }
            }
        }

        private void OnSingleAnswerSelected(int answerId, bool isChecked)
        {
            if (isChecked)
            {
                var questionId = _questions[_currentQuestionIndex].QuestionId;
                _userAnswers[questionId] = answerId;
            }
        }

        private void OnMultipleAnswerSelected(int answerId, bool isChecked)
        {
            var questionId = _questions[_currentQuestionIndex].QuestionId;

            if (!_userAnswers.ContainsKey(questionId))
            {
                _userAnswers[questionId] = new List<int>();
            }

            var selectedAnswers = (List<int>)_userAnswers[questionId];

            if (isChecked)
            {
                if (!selectedAnswers.Contains(answerId))
                    selectedAnswers.Add(answerId);
            }
            else
            {
                selectedAnswers.Remove(answerId);
            }
        }

        private void UpdateNavigationButtons()
        {
            PrevQuestionButton.IsVisible = _currentQuestionIndex > 0;
            NextQuestionButton.IsVisible = _currentQuestionIndex < _questions.Count - 1;
            FinishButton.IsVisible = _currentQuestionIndex == _questions.Count - 1;
        }

        private void OnPrevQuestionClicked(object sender, EventArgs e)
        {
            if (_currentQuestionIndex > 0)
            {
                ShowQuestion(_currentQuestionIndex - 1);
            }
        }

        private void OnNextQuestionClicked(object sender, EventArgs e)
        {
            if (_currentQuestionIndex < _questions.Count - 1)
            {
                ShowQuestion(_currentQuestionIndex + 1);
            }
        }

        private async void OnFinishTestClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Завершение теста",
                "Вы уверены, что хотите завершить тест?", "Да", "Нет");

            if (confirm)
            {
                FinishTest();
            }
        }

        private async void FinishTest()
        {
            try
            {
                _timer?.Stop();

                // Вычисляем результат с проверкой правильных ответов
                int score = await CalculateScoreWithValidation();

                // Сохраняем попытку
                await CompleteTestAttemptAsync(_attemptId, score);

                // Сохраняем ответы студента в базу данных
                await SaveStudentAnswersAsync();

                // Обновляем прогресс курса
                if (_courseId > 0)
                {
                    var status = score >= 60 ? "completed" : "in_progress";
                    await UpdateProgressWithScoreAsync(_currentUser.UserId, _courseId, _lessonId, status, score);
                }

                // Показываем результаты
                ShowTestResults(score);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка завершения теста: {ex.Message}", "OK");
            }
        }

        private async Task<int> CalculateScoreWithValidation()
        {
            int totalScore = 0;
            int maxScore = _questions.Sum(q => q.Score);

            foreach (var question in _questions)
            {
                if (_userAnswers.ContainsKey(question.QuestionId))
                {
                    bool isCorrect = await CheckAnswerCorrectness(question, _userAnswers[question.QuestionId]);
                    if (isCorrect)
                    {
                        totalScore += question.Score;
                    }
                }
            }

            return (int)((double)totalScore / maxScore * 100);
        }

        private async Task<bool> CheckAnswerCorrectness(Question question, object userAnswer)
        {
            try
            {
                switch (question.QuestionType.ToLower())
                {
                    case "single":
                        if (userAnswer is int selectedAnswerId)
                        {
                            var correctAnswer = question.AnswerOptions.FirstOrDefault(a => a.IsCorrect);
                            return correctAnswer != null && correctAnswer.AnswerId == selectedAnswerId;
                        }
                        break;

                    case "multiple":
                        if (userAnswer is List<int> selectedAnswerIds)
                        {
                            var correctAnswers = question.AnswerOptions.Where(a => a.IsCorrect).Select(a => a.AnswerId).ToList();

                            // Для множественного выбора: все правильные должны быть выбраны и никаких лишних
                            return correctAnswers.Count == selectedAnswerIds.Count &&
                                   correctAnswers.All(ca => selectedAnswerIds.Contains(ca));
                        }
                        break;

                    case "text":
                        if (userAnswer is string textAnswer)
                        {
                            // Для текстовых ответов проверяем по ключевым словам или точному совпадению
                            var correctAnswer = question.AnswerOptions.FirstOrDefault(a => a.IsCorrect);
                            if (correctAnswer != null)
                            {
                                // Простая проверка: точное совпадение или наличие ключевых слов
                                return textAnswer.Trim().Equals(correctAnswer.AnswerText?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                       textAnswer.ToLower().Contains(correctAnswer.AnswerText?.ToLower() ?? "");
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки ответа: {ex.Message}");
            }

            return false;
        }

        private async Task SaveStudentAnswersAsync()
        {
            try
            {
                foreach (var question in _questions)
                {
                    if (_userAnswers.ContainsKey(question.QuestionId))
                    {
                        var userAnswer = _userAnswers[question.QuestionId];
                        bool isCorrect = await CheckAnswerCorrectness(question, userAnswer);

                        // Сохраняем ответ студента в базу данных
                        await SaveStudentAnswerToDatabase(question.QuestionId, userAnswer, isCorrect);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения ответов: {ex.Message}");
            }
        }

        private async Task SaveStudentAnswerToDatabase(int questionId, object userAnswer, bool isCorrect)
        {
            try
            {
                // В реальном приложении нужно вызвать соответствующий метод DatabaseService
                await _dbService.SaveStudentAnswerAsync(_attemptId, questionId, userAnswer, isCorrect);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения ответа в БД: {ex.Message}");
            }
        }

        private void ShowTestResults(int score)
        {
            // Скрываем вопросы и показываем результаты
            AnswerOptionsStack.IsVisible = false;
            TextAnswerSection.IsVisible = false;
            PrevQuestionButton.IsVisible = false;
            NextQuestionButton.IsVisible = false;
            FinishButton.IsVisible = false;

            ResultSection.IsVisible = true;
            ScoreLabel.Text = $"Ваш результат: {score}/100";

            if (score >= 60)
            {
                ResultMessageLabel.Text = "Поздравляем! Вы успешно прошли тест.";
                ResultSection.BackgroundColor = Color.FromArgb("#E8F5E8");
            }
            else
            {
                ResultMessageLabel.Text = "Попробуйте еще раз. Для успешного прохождения нужно набрать 60 баллов.";
                ResultSection.BackgroundColor = Color.FromArgb("#FFEBEE");
            }
        }

        private async void OnReturnToCourseClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Подтверждение",
                "Вы уверены, что хотите выйти? Прогресс теста не будет сохранен.", "Да", "Нет");

            if (confirm)
            {
                _timer?.Stop();
                await Navigation.PopAsync();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _timer?.Stop();
            _timer?.Dispose();
        }

        // РЕАЛЬНЫЕ МЕТОДЫ РАБОТЫ С БАЗОЙ ДАННЫХ
        private async Task<List<Question>> GetTestQuestionsAsync(int testId)
        {
            try
            {
                return await _dbService.GetTestQuestionsAsync(testId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки вопросов: {ex.Message}");
                return new List<Question>();
            }
        }

        private async Task<int?> GetCourseIdByLessonAsync(int lessonId)
        {
            try
            {
                return await _dbService.GetCourseIdByLessonAsync(lessonId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения ID курса: {ex.Message}");
                return null;
            }
        }

        private async Task<int?> StartTestAttemptAsync(int testId, int userId, int? groupId)
        {
            try
            {
                return await _dbService.StartTestAttemptAsync(testId, userId, groupId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка начала попытки: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> CompleteTestAttemptAsync(int attemptId, int score)
        {
            try
            {
                return await _dbService.CompleteTestAttemptAsync(attemptId, score);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка завершения попытки: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> UpdateProgressWithScoreAsync(int userId, int courseId, int lessonId, string status, int score)
        {
            try
            {
                return await _dbService.UpdateProgressWithScoreAsync(userId, courseId, lessonId, status, score);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления прогресса: {ex.Message}");
                return false;
            }
        }
    }
}