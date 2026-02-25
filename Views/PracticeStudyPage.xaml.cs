using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using System.IO;

#if ANDROID
using Android.Content;
using Android.OS;
#endif

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
        private FileResult _selectedFile;
        private byte[] _selectedFileBytes;

        public ObservableCollection<AttachmentViewModel> Attachments { get; set; } = new();

        public string PageTitle { get; set; } = "Практическое задание";

        public PracticeStudyPage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
        {
            InitializeComponent();

            _currentUser = user ?? throw new ArgumentNullException(nameof(user));
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _fileService = new FileService();
            _lessonId = lessonId;

            BindingContext = this;
            AttachmentsCollection.ItemsSource = Attachments;

            Title = PageTitle;
            SetupAnswerInterface();
            LoadPracticeContent();
            LoadAttachments();
        }

        private async void LoadPracticeContent()
        {
            try
            {
                var practice = await _dbService.GetPracticeExerciseAsync(_lessonId);
                if (practice == null)
                {
                    DescriptionLabel.Text = "Задание не найдено.";
                    return;
                }

                PageTitle = practice.Title ?? "Практическое задание";
                Title = PageTitle;
                TitleLabel.Text = PageTitle;
                DescriptionLabel.Text = practice.Description ?? "Описание отсутствует";
                _expectedAnswer = practice.ExpectedOutput ?? "";
                _answerType = practice.AnswerType?.ToLower() ?? "text";

                SetupAnswerInterface();

                if (!string.IsNullOrEmpty(practice.StarterCode))
                {
                    StarterCodeEditor.Text = practice.StarterCode;
                    CodeSection.IsVisible = true;
                }

                if (!string.IsNullOrEmpty(practice.Hint))
                {
                    HintLabel.Text = practice.Hint;
                    HintSection.IsVisible = true;
                }

                var courseId = await _dbService.GetCourseIdByLessonAsync(_lessonId);
                if (courseId.HasValue) _courseId = courseId.Value;

                OnPropertyChanged(nameof(PageTitle));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка загрузки", ex.Message, "OK");
                DescriptionLabel.Text = "Не удалось загрузить задание";
            }
        }

        private void SetupAnswerInterface()
        {
            TextAnswerSection.IsVisible = _answerType == "text";
            CodeAnswerSection.IsVisible = _answerType == "code";
            FileAnswerSection.IsVisible = _answerType == "file";
        }

        private async void OnSelectFileClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите файл с решением",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".zip", ".pdf", ".jpg", ".jpeg", ".png", ".txt", ".docx" } },
                        { DevicePlatform.Android, new[] { "*/*" } },
                        { DevicePlatform.iOS, new[] { "public.data" } }
                    })
                });

                if (result != null)
                {
                    _selectedFile = result;

                    using var stream = await result.OpenReadAsync();
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    _selectedFileBytes = ms.ToArray();

                    SelectedFileNameLabel.Text = result.FileName;
                    SelectedFileSizeLabel.Text = FormatFileSize(_selectedFileBytes.Length);
                    FilePreviewSection.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выбрать файл\n{ex.Message}", "OK");
            }
        }

        private void OnRemoveFileClicked(object sender, EventArgs e)
        {
            _selectedFile = null;
            _selectedFileBytes = null;
            FilePreviewSection.IsVisible = false;
        }

        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            try
            {
                if (!await ValidateSubmission()) return;

                bool success = await SaveSubmission();
                if (success)
                {
                    ShowResult(true, "Работа успешно отправлена на проверку преподавателю!");
                    if (_courseId > 0)
                    {
                        await _dbService.UpdateProgressAsync(_currentUser.UserId, _courseId, "in_progress");
                    }
                }
                else
                {
                    ShowResult(false, "Не удалось отправить работу");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка отправки", ex.Message, "OK");
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
                        await DisplayAlert("Ошибка", "Напишите код решения", "OK");
                        return false;
                    }
                    break;

                case "file":
                    if (_selectedFileBytes == null || _selectedFile == null)
                    {
                        await DisplayAlert("Ошибка", "Выберите файл с решением", "OK");
                        return false;
                    }
                    break;

                default:
                    await DisplayAlert("Ошибка", "Неизвестный тип ответа", "OK");
                    return false;
            }
            return true;
        }

        private async Task<bool> SaveSubmission()
        {
            string answerText = null;
            string answerFilePath = null;

            switch (_answerType.ToLower())
            {
                case "text":
                    answerText = TextAnswerEditor.Text?.Trim();
                    break;

                case "code":
                    answerText = CodeAnswerEditor.Text?.Trim();
                    break;

                case "file":
                    answerFilePath = await SaveSubmissionFile();
                    if (string.IsNullOrEmpty(answerFilePath)) return false;
                    break;
            }

            return await _dbService.SavePracticeSubmissionAsync(
                _lessonId,
                _currentUser.UserId,
                answerText,
                answerFilePath
            );
        }

        private async Task<string> SaveSubmissionFile()
        {
            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "PracticeSubmissions");
                Directory.CreateDirectory(folder);

                var extension = Path.GetExtension(_selectedFile.FileName);
                var fileName = $"submission_{_currentUser.UserId}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                var fullPath = Path.Combine(folder, fileName);

                await File.WriteAllBytesAsync(fullPath, _selectedFileBytes);

                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения файла: {ex.Message}");
                return null;
            }
        }

        private void ShowResult(bool success, string message)
        {
            ResultSection.IsVisible = true;

            ResultSection.BackgroundColor = success ? Color.FromArgb("#E8F5E8") : Color.FromArgb("#FFEBEE");
            ResultSection.BorderColor = success ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");

            ResultLabel.Text = success ? "✅ Успех!" : "❌ Ошибка";
            ResultLabel.TextColor = success ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
            FeedbackLabel.Text = message;
        }

        private async void OnRunCodeClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Запуск кода", "Функция выполнения кода будет добавлена позже", "OK");
        }

        private void OnClearCodeClicked(object sender, EventArgs e)
        {
            CodeAnswerEditor.Text = "";
        }

        private async void OnCopyCodeClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(StarterCodeEditor.Text)) return;

            await Clipboard.SetTextAsync(StarterCodeEditor.Text);
            await DisplayAlert("Успех", "Стартовый код скопирован в буфер обмена", "OK");
        }

        // Методы работы с вложениями задания
        private async void LoadAttachments()
        {
            try
            {
                var attachments = await _dbService.GetLessonAttachmentsAsync(_lessonId);
                Attachments.Clear();

                if (attachments?.Any() == true)
                {
                    foreach (var att in attachments)
                    {
                        Attachments.Add(new AttachmentViewModel
                        {
                            AttachmentId = att.AttachmentId,
                            FileName = att.FileName,
                            FileSize = att.FileSize,
                            FilePath = att.FilePath,
                            FileIcon = _fileService.GetFileIcon(att.FileType),
                            FileType = att.FileType ?? ""
                        });
                    }
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
            }
        }

        private async void OnAttachmentTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is AttachmentViewModel vm)
            {
                await HandleAttachmentAction(vm);
            }
        }

        private async void OnOpenAttachmentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is AttachmentViewModel vm)
            {
                await HandleAttachmentAction(vm);
            }
        }

        private async Task HandleAttachmentAction(AttachmentViewModel vm)
        {
            var choice = await DisplayActionSheet(
                vm.FileName,
                "Отмена",
                null,
                "📥 Скачать в Загрузки",
                "📄 Открыть"
            );

            if (choice == "📥 Скачать в Загрузки")
            {
                var success = await DownloadAttachmentToDownloads(vm.FilePath, vm.FileName);
                if (success)
                {
                    await DisplayAlert("Успех", $"Файл {vm.FileName} сохранён в Загрузки", "OK");
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось сохранить файл", "OK");
                }
            }
            else if (choice == "📄 Открыть")
            {
                await TryOpenFile(vm);
            }
        }

        private async Task<bool> DownloadAttachmentToDownloads(string filePath, string fileName)
        {
            try
            {
                // Получаем путь к файлу внутри приложения
                var sourcePath = await _fileService.ResolveFilePath(filePath, fileName, "PracticeFiles");
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                {
                    Console.WriteLine($"Файл не найден по пути: {sourcePath}");
                    return false;
                }

                // Определяем папку Загрузки и копируем файл
                string destPath = await GetDownloadsPath(fileName);
                if (string.IsNullOrEmpty(destPath))
                {
                    Console.WriteLine("Не удалось определить папку Загрузки");
                    return false;
                }

                File.Copy(sourcePath, destPath, true);
                Console.WriteLine($"Файл скопирован в: {destPath}");

                // На Android — уведомляем систему, чтобы файл появился в файловом менеджере
#if ANDROID
                try
                {
                    var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                    if (context != null)
                    {
                        var intent = new Intent(Intent.ActionMediaScannerScanFile);
                        intent.SetData(Android.Net.Uri.FromFile(new Java.IO.File(destPath)));
                        context.SendBroadcast(intent);
                        Console.WriteLine("MediaScanner уведомление отправлено");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка MediaScanner: {ex.Message}");
                }
#endif

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка скачивания: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        private async Task<string> GetDownloadsPath(string fileName)
        {
            string downloadsFolder;

#if ANDROID
            downloadsFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
            if (string.IsNullOrEmpty(downloadsFolder))
            {
                downloadsFolder = Path.Combine(Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath ?? "", "Download");
            }
#elif WINDOWS
            downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
#elif IOS || MACCATALYST
            downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Downloads");
#else
            downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif

            if (string.IsNullOrEmpty(downloadsFolder) || !Directory.Exists(downloadsFolder))
            {
                try
                {
                    Directory.CreateDirectory(downloadsFolder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Не удалось создать папку Загрузки: {ex.Message}");
                    return null;
                }
            }

            // Уникальное имя файла
            string path = Path.Combine(downloadsFolder, fileName);
            int counter = 1;
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);

            while (File.Exists(path))
            {
                path = Path.Combine(downloadsFolder, $"{baseName} ({counter++}){ext}");
            }

            return path;
        }

        private async Task TryOpenFile(AttachmentViewModel vm)
        {
            try
            {
                var path = await _fileService.ResolveFilePath(vm.FilePath, vm.FileName, "PracticeFiles");
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    await DisplayAlert("Ошибка", "Файл не найден", "OK");
                    return;
                }

                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(path)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка открытия", ex.Message, "OK");
            }
        }

        private async void OnNextClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} Б";
            if (bytes < 1024 * 1024) return $"{(bytes / 1024.0):0.0} КБ";
            return $"{(bytes / (1024.0 * 1024.0)):0.0} МБ";
        }
    }
}