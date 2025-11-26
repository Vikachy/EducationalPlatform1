using EducationalPlatform.Services;
using EducationalPlatform.Models;
using System.Text.Json;

namespace EducationalPlatform.Views
{
    public partial class PracticePage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private readonly int _courseId;
        private readonly int _lessonId;

        private PracticeDto? _exercise;

        public PracticePage(User user, DatabaseService dbService, SettingsService settingsService, int courseId, int lessonId, string lessonTitle)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _fileService = new FileService();
            _courseId = courseId;
            _lessonId = lessonId;
            TitleLabel.Text = lessonTitle;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPracticeData();
        }

        private async Task LoadPracticeData()
        {
            try
            {
                _exercise = await _dbService.GetPracticeExerciseWithLessonDataAsync(_lessonId);

                if (_exercise != null)
                {
                    TitleLabel.Text = _exercise.Title ?? "Практическое задание";
                    DescriptionLabel.Text = _exercise.Description ?? "Описание отсутствует";
                    CodeEditor.Text = _exercise.StarterCode ?? string.Empty;

                    // Показываем подсказку если есть
                    if (!string.IsNullOrEmpty(_exercise.Hint))
                    {
                        HintLabel.Text = _exercise.Hint;
                        HintSection.IsVisible = true;
                    }

                    // Загружаем прикрепленные файлы
                    await LoadAttachments();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить задание: {ex.Message}", "OK");
            }
        }

        private async Task LoadAttachments()
        {
            try
            {
                // Реальная загрузка вложений из базы данных
                var attachments = await _dbService.GetLessonAttachmentsAsync(_lessonId);
                if (attachments != null && attachments.Any())
                {
                    // Добавляем иконки файлов и подготавливаем модель для отображения
                    var attachmentsWithIcons = attachments.Select(a => new AttachmentViewModel
                    {
                        FileName = a.FileName,
                        FileSize = a.FileSize,
                        FilePath = a.FilePath,
                        FileIcon = _fileService.GetFileIcon(a.FileType)
                    }).ToList();

                    AttachmentsCollection.ItemsSource = attachmentsWithIcons;
                    AttachmentsSection.IsVisible = true;
                }
                else
                {
                    AttachmentsSection.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки вложений: {ex.Message}");
                AttachmentsSection.IsVisible = false;
            }
        }

        private async void OnCheckClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CodeEditor.Text))
                {
                    await DisplayAlert("Ошибка", "Введите решение", "OK");
                    return;
                }

                int score = await EvaluateSolution();

                await _dbService.UpdateProgressWithScoreAsync(_currentUser.UserId, _courseId, _lessonId,
                    score >= 60 ? "completed" : "in_progress", score);

                ShowResult(score);

                if (score >= 60)
                {
                    await DisplayAlert("Поздравляем!", "Вы успешно выполнили задание!", "OK");
                    // Награда за выполнение
                    await _dbService.AddGameCurrencyAsync(_currentUser.UserId, 50, "practice_completion");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async Task<int> EvaluateSolution()
        {
            if (_exercise == null) return 0;

            int score = 0;

            if (!string.IsNullOrEmpty(_exercise.ExpectedOutput))
            {
                // Проверка по ожидаемому выводу
                var output = CodeEditor.Text?.Trim() ?? string.Empty;
                score = output.Contains(_exercise.ExpectedOutput.Trim()) ? 100 : 0;
            }
            else if (!string.IsNullOrEmpty(_exercise.TestCasesJson))
            {
                // Проверка тест-кейсов
                try
                {
                    var testCases = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(_exercise.TestCasesJson) ?? new();
                    int passed = 0;

                    foreach (var tc in testCases)
                    {
                        if (tc.TryGetValue("expected", out var exp) &&
                            tc.TryGetValue("input", out var input))
                        {
                            // Простая проверка - можно расширить для реального выполнения кода
                            if ((CodeEditor.Text ?? string.Empty).Contains(exp) &&
                                (CodeEditor.Text ?? string.Empty).Contains(input))
                            {
                                passed++;
                            }
                        }
                    }

                    score = testCases.Count == 0 ? 0 : (int)(100.0 * passed / testCases.Count);
                }
                catch
                {
                    score = 0;
                }
            }

            return score;
        }

        private void ShowResult(int score)
        {
            ResultSection.IsVisible = true;

            if (score >= 60)
            {
                ResultLabel.Text = $"✅ Задание выполнено успешно! Оценка: {score}%";
                ResultLabel.TextColor = Color.FromArgb("#28A745");
            }
            else if (score >= 40)
            {
                ResultLabel.Text = $"⚠️ Задание выполнено частично. Оценка: {score}%";
                ResultLabel.TextColor = Color.FromArgb("#FFC107");
            }
            else
            {
                ResultLabel.Text = $"❌ Задание не пройдено. Оценка: {score}%";
                ResultLabel.TextColor = Color.FromArgb("#DC3545");
            }
        }

        private async void OnDownloadAttachmentClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is AttachmentViewModel attachment)
            {
                try
                {
                    var filePath = attachment.FilePath;
                    var fileName = attachment.FileName;

                    // Восстанавливаем фактический путь к файлу (с учётом data URL и разных платформ)
                    var resolvedPath = await _fileService.ResolveFilePath(filePath, fileName, "PracticeFiles");
                    if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
                    {
                        await DisplayAlert("Ошибка", $"Файл не найден: {fileName}", "OK");
                        return;
                    }

                    var success = await _fileService.DownloadFileAsync(resolvedPath, fileName);

                    if (success)
                    {
                        await DisplayAlert("Успех", $"Файл {fileName} скачан", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось скачать файл", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Ошибка при скачивании: {ex.Message}", "OK");
                }
            }
        }
    }

}


