using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace EducationalPlatform.Views
{
    public partial class PracticeStudyPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private readonly int _lessonId;
        private int _courseId;
        private string _answerType = "text";
        private string _expectedAnswer = "";
        private string _description = "";
        private FileResult _selectedFile;
        private byte[] _selectedFileBytes;

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
                    TitleLabel.Text = practiceData.Title ?? "Практическое задание";
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

        private async void OnSelectFileClicked(object sender, EventArgs e)
        {
            try
            {
                var fileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".zip", ".pdf", ".jpg", ".jpeg", ".png" } },
                        { DevicePlatform.macOS, new[] { ".zip", ".pdf", ".jpg", ".jpeg", ".png" } },
                        { DevicePlatform.Android, new[] { "*/*" } },
                        { DevicePlatform.iOS, new[] { "public.data" } }
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Выберите файл с решением",
                    FileTypes = fileTypes
                };

                var fileResult = await FilePicker.Default.PickAsync(options);
                if (fileResult != null)
                {
                    _selectedFile = fileResult;

                    // Читаем файл в байты
                    using var stream = await fileResult.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    _selectedFileBytes = memoryStream.ToArray();

                    // Показываем информацию о файле
                    SelectedFileLabel.Text = $"Выбран файл: {fileResult.FileName}";
                    SelectedFileNameLabel.Text = fileResult.FileName;
                    SelectedFileSizeLabel.Text = FormatFileSize(_selectedFileBytes.Length);
                    FilePreviewSection.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выбрать файл: {ex.Message}", "OK");
            }
        }

        private void OnRemoveFileClicked(object sender, EventArgs e)
        {
            _selectedFile = null;
            _selectedFileBytes = null;
            SelectedFileLabel.Text = "Файл не выбран";
            FilePreviewSection.IsVisible = false;
        }

        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            try
            {
                bool isValid = await ValidateSubmission();
                if (!isValid) return;

                bool success = await SaveSubmission();
                if (success)
                {
                    await DisplayAlert("Успех", "Работа отправлена на проверку!", "OK");
                    ShowResult(true, "Ваша работа отправлена на проверку преподавателю.");

                    // Обновляем прогресс
                    if (_courseId > 0)
                    {
                        await _dbService.UpdateProgressAsync(_currentUser.UserId, _courseId, "in_progress");
                    }
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось отправить работу", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка отправки: {ex.Message}", "OK");
            }
        }

        private async Task<bool> ValidateSubmission()
        {
            switch (_answerType.ToLower())
            {
                case "text":
                    if (string.IsNullOrWhiteSpace(TextAnswerEditor.Text))
                    {
                        await DisplayAlert("Ошибка", "Введите текстовый ответ", "OK");
                        return false;
                    }
                    break;
                case "code":
                    if (string.IsNullOrWhiteSpace(CodeAnswerEditor.Text))
                    {
                        await DisplayAlert("Ошибка", "Введите код", "OK");
                        return false;
                    }
                    break;
                case "file":
                    if (_selectedFileBytes == null)
                    {
                        await DisplayAlert("Ошибка", "Выберите файл с решением", "OK");
                        return false;
                    }
                    break;
            }
            return true;
        }

        private async Task<bool> SaveSubmission()
        {
            try
            {
                string submissionText = null;
                string submissionFileUrl = null;

                switch (_answerType.ToLower())
                {
                    case "text":
                        submissionText = TextAnswerEditor.Text;
                        break;
                    case "code":
                        submissionText = CodeAnswerEditor.Text;
                        break;
                    case "file":
                        // Сохраняем файл и получаем URL
                        submissionFileUrl = await SaveSubmissionFile();
                        break;
                }

                return await _dbService.SavePracticeSubmissionAsync(
                    _lessonId,
                    _currentUser.UserId,
                    submissionText,
                    submissionFileUrl
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения работы: {ex.Message}");
                return false;
            }
        }

        private async Task<string> SaveSubmissionFile()
        {
            try
            {
                // Создаем папку для работ студентов
                var submissionsFolder = Path.Combine(FileSystem.AppDataDirectory, "PracticeSubmissions");
                if (!Directory.Exists(submissionsFolder))
                {
                    Directory.CreateDirectory(submissionsFolder);
                }

                // Генерируем уникальное имя файла
                var fileName = $"submission_{_currentUser.UserId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(_selectedFile.FileName)}";
                var fullPath = Path.Combine(submissionsFolder, fileName);

                // Сохраняем файл
                await File.WriteAllBytesAsync(fullPath, _selectedFileBytes);
                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения файла работы: {ex.Message}");
                return null;
            }
        }


        private void OnRunCodeClicked(object sender, EventArgs e)
        {
            // Логика запуска кода
            // TODO: Реализовать выполнение кода
            DisplayAlert("Информация", "Запуск кода будет реализован в будущем", "OK");
        }

        private void ShowResult(bool isSuccess, string message)
        {
            ResultSection.IsVisible = true;

            if (isSuccess)
            {
                ResultSection.BackgroundColor = Color.FromArgb("#E8F5E8");
                ResultSection.Stroke = Color.FromArgb("#4CAF50");
                ResultLabel.Text = "✅ Работа отправлена!";
                ResultLabel.TextColor = Color.FromArgb("#4CAF50");
            }
            else
            {
                ResultSection.BackgroundColor = Color.FromArgb("#FFEBEE");
                ResultSection.Stroke = Color.FromArgb("#F44336");
                ResultLabel.Text = "❌ Ошибка отправки";
                ResultLabel.TextColor = Color.FromArgb("#F44336");
            }

            FeedbackLabel.Text = message;
        }

        private async Task LoadAttachments()
        {
            try
            {
                Console.WriteLine($"🔄 Загружаем вложения для практики {_lessonId}");

                var attachments = await _dbService.GetPracticeAttachmentsAsync(_lessonId);

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
                        AttachmentsCollection.ItemsSource = null;
                        AttachmentsCollection.ItemsSource = Attachments;
                    }
                    else
                    {
                        AttachmentsSection.IsVisible = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки вложений практики: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AttachmentsSection.IsVisible = false;
                });
            }
        }

        private async void OnCopyCodeClicked(object sender, EventArgs e)
        {
            // Копирование кода в буфер обмена
            if (!string.IsNullOrEmpty(StarterCodeEditor.Text))
            {
                await Clipboard.SetTextAsync(StarterCodeEditor.Text);
                await DisplayAlert("Успех", "Код скопирован в буфер обмена", "OK");
            }
            else
            {
                await DisplayAlert("Ошибка", "Нет кода для копирования", "OK");
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
                        await DisplayAlert("Успех", $"Файл {attachment.FileName} скачан", "OK");
                    }
                }
                else if (action == "📁 Открыть файл")
                {
                    var success = await OpenAttachmentFile(attachment.FilePath, attachment.FileName);
                    if (!success)
                    {
                        await DisplayAlert("Ошибка", $"Не удалось открыть файл", "OK");
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
                var resolvedPath = await _fileService.ResolveFilePath(filePath, fileName, "PracticeFiles");
                return await _fileService.DownloadFileAsync(resolvedPath, fileName);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось скачать файл: {ex.Message}", "OK");
                return false;
            }
        }

        private async Task<bool> OpenAttachmentFile(string filePath, string fileName)
        {
            try
            {
                var resolvedPath = await _fileService.ResolveFilePath(filePath, fileName, "PracticeFiles");
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(resolvedPath)
                });
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} Б";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.0} КБ";
            return $"{bytes / (1024.0 * 1024.0):0.0} МБ";
        }

        private async void OnNextClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}