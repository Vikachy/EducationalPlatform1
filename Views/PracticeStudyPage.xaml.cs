using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

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

        private readonly FileService _fileService;
        public ObservableCollection<AttachmentViewModel> Attachments { get; set; } = new();

        public PracticeStudyPage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _fileService = new FileService();
            _lessonId = lessonId;

            BindingContext = this;
            
            // Устанавливаем ItemsSource для CollectionView
            AttachmentsCollection.ItemsSource = Attachments;

            LoadPracticeContent();
            LoadAttachments();
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

        private async Task LoadAttachments()
        {
            try
            {
                Console.WriteLine($"🔄 Загружаем вложения для практики {_lessonId}");
                
                var attachments = await _dbService.GetLessonAttachmentsAsync(_lessonId);
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Attachments.Clear();

                    if (attachments != null && attachments.Any())
                    {
                        Console.WriteLine($"📎 Найдено {attachments.Count} вложений");
                        
                        foreach (var attachment in attachments)
                        {
                            Attachments.Add(new AttachmentViewModel
                            {
                                AttachmentId = attachment.AttachmentId,
                                FileName = attachment.FileName,
                                FileSize = attachment.FileSize,
                                FilePath = attachment.FilePath,
                                FileIcon = _fileService.GetFileIcon(attachment.FileType)
                            });
                        }
                        
                        AttachmentsSection.IsVisible = true;
                        AttachmentsCollection.ItemsSource = null; // Сбрасываем для обновления
                        AttachmentsCollection.ItemsSource = Attachments;
                        
                        Console.WriteLine($"✅ Вложения загружены и отображены");
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️ Вложения не найдены");
                        AttachmentsSection.IsVisible = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки вложений практики: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AttachmentsSection.IsVisible = false;
                });
            }
        }

        private async void OnAttachmentTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is AttachmentViewModel attachment)
            {
                await HandleAttachmentAction(attachment);
            }
        }

        private async void OnOpenAttachmentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is AttachmentViewModel attachment)
            {
                await HandleAttachmentAction(attachment);
            }
        }

        private async Task HandleAttachmentAction(AttachmentViewModel attachment)
        {
            try
            {
                if (string.IsNullOrEmpty(attachment.FilePath))
                {
                    await DisplayAlert("Ошибка", "Файл не найден", "OK");
                    return;
                }

                // Показываем опции: скачать или открыть
                var action = await DisplayActionSheet(
                    $"Файл: {attachment.FileName}",
                    "Отмена",
                    null,
                    "📥 Скачать в папку Загрузки",
                    "📁 Открыть файл");

                if (action == "📥 Скачать в папку Загрузки")
                {
                    var success = await DownloadAttachmentToDownloads(attachment.FilePath, attachment.FileName);
                    if (success)
                    {
                        await DisplayAlert("Успех", $"Файл {attachment.FileName} скачан в папку Загрузки", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", $"Не удалось скачать файл {attachment.FileName}", "OK");
                    }
                }
                else if (action == "📁 Открыть файл")
                {
                    var success = await OpenAttachmentFile(attachment.FilePath, attachment.FileName);
                    if (!success)
                    {
                        await DisplayAlert("Ошибка", $"Не удалось открыть файл {attachment.FileName}", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось обработать файл: {ex.Message}", "OK");
            }
        }

        private async Task<bool> DownloadAttachmentToDownloads(string filePath, string fileName)
        {
            try
            {
                Console.WriteLine($"📥 Начинаем скачивание файла: {fileName} из {filePath}");

                var resolvedPath = await _fileService.ResolveFilePath(filePath, fileName, "PracticeFiles");

                if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
                {
                    Console.WriteLine($"❌ Файл не найден: {resolvedPath}");
                    await DisplayAlert("Ошибка", $"Файл не найден: {fileName}", "OK");
                    return false;
                }

                var success = await _fileService.DownloadFileAsync(resolvedPath, fileName);
                
                if (success)
                {
                    Console.WriteLine($"✅ Файл успешно скачан: {fileName}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось скачать файл: {fileName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка скачивания файла: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Ошибка", $"Не удалось скачать файл: {ex.Message}", "OK");
                return false;
            }
        }

        private async Task<bool> OpenAttachmentFile(string filePath, string fileName)
        {
            try
            {
                var resolvedPath = await _fileService.ResolveFilePath(filePath, fileName, "PracticeFiles");

                if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
                {
                    return false;
                }

                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(resolvedPath)
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка открытия файла: {ex.Message}");
                return false;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Очистка ресурсов если нужно
        }
    }
}