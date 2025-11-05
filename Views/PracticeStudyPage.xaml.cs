using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class PracticeStudyPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _lessonId;
        private int _courseId;
        private string _answerType = "text";
        private string _expectedAnswer = "";
        private string _description = "";
        private FileResult _selectedFile;

        public PracticeStudyPage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _lessonId = lessonId;

            LoadPracticeContent();
        }

        private async void LoadPracticeContent()
        {
            try
            {
                // Получаем данные практического задания
                var practiceData = await _dbService.GetPracticeExerciseAsync(_lessonId);

                if (practiceData != null)
                {
                    TitleLabel.Text = "Практическое задание";

                    // Используем безопасное получение свойств
                    _description = practiceData.Description ?? "Описание задания";
                    _expectedAnswer = practiceData.ExpectedOutput ?? "";
                    _answerType = practiceData.AnswerType ?? "text"; 

                    DescriptionLabel.Text = _description;

                    // Настраиваем интерфейс в зависимости от типа ответа
                    SetupAnswerInterface();

                    // Загружаем стартовый код если есть
                    if (!string.IsNullOrEmpty(practiceData.StarterCode))
                    {
                        StarterCodeEditor.Text = practiceData.StarterCode;
                        CodeSection.IsVisible = true;
                    }

                    // Показываем подсказку если есть
                    if (!string.IsNullOrEmpty(practiceData.Hint))
                    {
                        HintLabel.Text = practiceData.Hint;
                        HintSection.IsVisible = true;
                    }

                    // Получаем ID курса
                    var courseId = await _dbService.GetCourseIdByLessonAsync(_lessonId);
                    if (courseId.HasValue) _courseId = courseId.Value;
                }
                else
                {
                    DescriptionLabel.Text = "Практическое задание не найдено.";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить задание: {ex.Message}", "OK");
            }
        }

        private void SetupAnswerInterface()
        {
            // Скрываем все секции ответов
            TextAnswerSection.IsVisible = false;
            CodeAnswerSection.IsVisible = false;
            FileAnswerSection.IsVisible = false;

            // Показываем нужную секцию в зависимости от типа ответа
            switch (_answerType.ToLower())
            {
                case "text":
                    TextAnswerSection.IsVisible = true;
                    break;
                case "code":
                    CodeAnswerSection.IsVisible = true;
                    break;
                case "file":
                    FileAnswerSection.IsVisible = true;
                    break;
                default:
                    TextAnswerSection.IsVisible = true;
                    break;
            }
        }

        private async void OnCopyCodeClicked(object sender, EventArgs e)
        {
            await Clipboard.Default.SetTextAsync(StarterCodeEditor.Text);
            await DisplayAlert("Успех", "Код скопирован в буфер обмена", "OK");
        }

        private async void OnSelectFileClicked(object sender, EventArgs e)
        {
            try
            {
                var fileResult = await FilePicker.Default.PickAsync();
                if (fileResult != null)
                {
                    _selectedFile = fileResult;
                    SelectedFileLabel.Text = $"Выбран файл: {fileResult.FileName}";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выбрать файл: {ex.Message}", "OK");
            }
        }

        private async void OnRunCodeClicked(object sender, EventArgs e)
        {
            try
            {
                // В реальном приложении нужно вызвать соответствующий метод DatabaseService
                // для выполнения кода и проверки результата
                var userCode = CodeAnswerEditor.Text?.Trim();
                if (string.IsNullOrEmpty(userCode))
                {
                    await DisplayAlert("Ошибка", "Введите код для выполнения", "OK");
                    return;
                }

                // Здесь будет вызов сервиса выполнения кода
                await DisplayAlert("Информация", "Запуск кода будет реализован в будущих версиях", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка выполнения кода: {ex.Message}", "OK");
            }
        }

        private async void OnCheckAnswerClicked(object sender, EventArgs e)
        {
            try
            {
                bool isCorrect = false;
                string feedback = "";

                switch (_answerType.ToLower())
                {
                    case "text":
                        isCorrect = await CheckTextAnswer();
                        feedback = isCorrect ? "Текст ответа верный!" : "Текст ответа не соответствует ожидаемому.";
                        break;
                    case "code":
                        isCorrect = await CheckCodeAnswer();
                        feedback = isCorrect ? "Код работает корректно!" : "Код требует доработки.";
                        break;
                    case "file":
                        isCorrect = await CheckFileAnswer();
                        feedback = isCorrect ? "Файл принят!" : "Проверьте содержимое файла.";
                        break;
                }

                ShowResult(isCorrect, feedback);

                // Обновляем прогресс
                if (isCorrect && _courseId > 0)
                {
                    // В реальном приложении нужно вызвать соответствующий метод DatabaseService
                    await _dbService.UpdateProgressWithScoreAsync(_currentUser.UserId, _courseId, _lessonId, "completed", 100);

                    // Начисляем бонусные монеты за выполнение задания
                    await _dbService.AddGameCurrencyAsync(_currentUser.UserId, 50, "practice_completion");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка проверки: {ex.Message}", "OK");
            }
        }

        private async Task<bool> CheckTextAnswer()
        {
            var userAnswer = TextAnswerEditor.Text?.Trim() ?? "";
            var expected = _expectedAnswer?.Trim() ?? "";

            return userAnswer.Equals(expected, StringComparison.OrdinalIgnoreCase) ||
                   userAnswer.Contains(expected, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> CheckCodeAnswer()
        {
            var userCode = CodeAnswerEditor.Text?.Trim() ?? "";

            // Базовая проверка - код не должен быть пустым
            if (string.IsNullOrEmpty(userCode))
                return false;

            // В реальном приложении здесь будет вызов сервиса выполнения и проверки кода
            return userCode.Length > 10; // Временная простая проверка
        }

        private async Task<bool> CheckFileAnswer()
        {
            return _selectedFile != null;
        }

        private void ShowResult(bool isCorrect, string feedback)
        {
            ResultSection.IsVisible = true;

            if (isCorrect)
            {
                ResultSection.BackgroundColor = Color.FromArgb("#E8F5E8");
                ResultSection.Stroke = Color.FromArgb("#4CAF50");
                ResultLabel.Text = "✅ Задание выполнено успешно!";
                ResultLabel.TextColor = Color.FromArgb("#4CAF50");
            }
            else
            {
                ResultSection.BackgroundColor = Color.FromArgb("#FFEBEE");
                ResultSection.Stroke = Color.FromArgb("#F44336");
                ResultLabel.Text = "❌ Задание требует доработки";
                ResultLabel.TextColor = Color.FromArgb("#F44336");
            }

            FeedbackLabel.Text = feedback;
        }

        private async void OnNextClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Подтверждение",
                "Вы уверены, что хотите выйти? Прогресс не будет сохранен.", "Да", "Нет");

            if (confirm)
            {
                await Navigation.PopAsync();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Очистка ресурсов если нужно
        }
    }
}