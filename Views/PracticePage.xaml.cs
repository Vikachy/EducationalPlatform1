using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class PracticePage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private readonly int _courseId;
        private readonly int _lessonId;
        private readonly string _lessonTitle;

        private string _answerType = "text";
        private string _expectedAnswer = "";
        private FileResult? _selectedFile;
        private byte[]? _selectedFileBytes;
        private bool _isSubmitting = false;

        private Grid? _loadingOverlay;
        private ActivityIndicator? _loadingIndicator;
        private Label? _loadingLabel;
        private Border? _uploadProgressSection;
        private ProgressBar? _uploadProgressBar;
        private Label? _uploadStatusLabel;
        private CollectionView? _attachmentsCollection;
        private Border? _filePreviewSection;
        private Label? _fileIconLabel;
        private Label? _selectedFileNameLabel;
        private Label? _selectedFileSizeLabel;
        private Button? _submitButton;

        private Label? _titleLabel;
        private Label? _descriptionLabel;
        private Border? _codeSection;
        private Editor? _starterCodeEditor;
        private Label? _hintLabel;
        private Border? _hintSection;
        private VerticalStackLayout? _textAnswerSection;
        private VerticalStackLayout? _codeAnswerSection;
        private VerticalStackLayout? _fileAnswerSection;
        private Editor? _textAnswerEditor;
        private Editor? _codeAnswerEditor;
        private Border? _resultSection;
        private Label? _resultIconLabel;
        private Label? _resultLabel;
        private Label? _feedbackLabel;
        private Border? _gradeSection;
        private Label? _gradeScoreLabel;
        private Label? _teacherCommentLabel;
        private Label? _gradedByLabel;
        private Border? _attachmentsSection;

        public ObservableCollection<AttachmentViewModel> Attachments { get; set; } = new();

        private string _pageTitle;
        public string PageTitle
        {
            get => _pageTitle;
            set
            {
                _pageTitle = value;
                OnPropertyChanged();
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PracticePage(
    User currentUser,
    DatabaseService dbService,
    SettingsService settingsService,
    int courseId,
    int lessonId,
    string lessonTitle)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации PracticePage: {ex.Message}");
            }

            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _fileService = new FileService();
            _courseId = courseId;
            _lessonId = lessonId;
            _lessonTitle = lessonTitle ?? "Практическое задание";

            PageTitle = _lessonTitle;
            Title = PageTitle;

            InitializeControls();

            BindingContext = this;

            if (_attachmentsCollection != null)
                _attachmentsCollection.ItemsSource = Attachments;

            this.SizeChanged += (s, e) =>
            {
                SetupAnswerInterface();
            };

            _ = LoadPracticeContentAsync();
            _ = LoadAttachmentsAsync();
            _ = LoadGradeIfExistsAsync();
        }

        private void InitializeControls()
        {
            _loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
            _loadingIndicator = this.FindByName<ActivityIndicator>("LoadingIndicator");
            _loadingLabel = this.FindByName<Label>("LoadingLabel");
            _uploadProgressSection = this.FindByName<Border>("UploadProgressSection");
            _uploadProgressBar = this.FindByName<ProgressBar>("UploadProgressBar");
            _uploadStatusLabel = this.FindByName<Label>("UploadStatusLabel");
            _attachmentsCollection = this.FindByName<CollectionView>("AttachmentsCollection");
            _filePreviewSection = this.FindByName<Border>("FilePreviewSection");
            _fileIconLabel = this.FindByName<Label>("FileIconLabel");
            _selectedFileNameLabel = this.FindByName<Label>("SelectedFileNameLabel");
            _selectedFileSizeLabel = this.FindByName<Label>("SelectedFileSizeLabel");
            _submitButton = this.FindByName<Button>("SubmitButton");

            _titleLabel = this.FindByName<Label>("TitleLabel");
            _descriptionLabel = this.FindByName<Label>("DescriptionLabel");
            _codeSection = this.FindByName<Border>("CodeSection");
            _starterCodeEditor = this.FindByName<Editor>("StarterCodeEditor");
            _hintLabel = this.FindByName<Label>("HintLabel");
            _hintSection = this.FindByName<Border>("HintSection");
            _textAnswerSection = this.FindByName<VerticalStackLayout>("TextAnswerSection");
            _codeAnswerSection = this.FindByName<VerticalStackLayout>("CodeAnswerSection");
            _fileAnswerSection = this.FindByName<VerticalStackLayout>("FileAnswerSection");
            _textAnswerEditor = this.FindByName<Editor>("TextAnswerEditor");
            _codeAnswerEditor = this.FindByName<Editor>("CodeAnswerEditor");
            _resultSection = this.FindByName<Border>("ResultSection");
            _resultIconLabel = this.FindByName<Label>("ResultIconLabel");
            _resultLabel = this.FindByName<Label>("ResultLabel");
            _feedbackLabel = this.FindByName<Label>("FeedbackLabel");
            _gradeSection = this.FindByName<Border>("GradeSection");
            _gradeScoreLabel = this.FindByName<Label>("GradeScoreLabel");
            _teacherCommentLabel = this.FindByName<Label>("TeacherCommentLabel");
            _gradedByLabel = this.FindByName<Label>("GradedByLabel");
            _attachmentsSection = this.FindByName<Border>("AttachmentsSection");

            Console.WriteLine($"📌 Проверка элементов управления:");
            Console.WriteLine($"   TitleLabel: {(_titleLabel != null ? "✅" : "❌")}");
            Console.WriteLine($"   DescriptionLabel: {(_descriptionLabel != null ? "✅" : "❌")}");
            Console.WriteLine($"   TextAnswerSection: {(_textAnswerSection != null ? "✅" : "❌")}");
            Console.WriteLine($"   CodeAnswerSection: {(_codeAnswerSection != null ? "✅" : "❌")}");
            Console.WriteLine($"   FileAnswerSection: {(_fileAnswerSection != null ? "✅" : "❌")}");
            Console.WriteLine($"   HintSection: {(_hintSection != null ? "✅" : "❌")}");
            Console.WriteLine($"   SubmitButton: {(_submitButton != null ? "✅" : "❌")}");
        }

        private async Task LoadPracticeContentAsync()
        {
            try
            {
                ShowLoading(true, "Загрузка задания...");

                var practice = await _dbService.GetPracticeExerciseAsync(_lessonId);

                Console.WriteLine($"📌 Загружена практика: ID={_lessonId}");
                Console.WriteLine($"   Practice объект: {(practice == null ? "null" : "не null")}");

                if (practice == null)
                {
                    if (_descriptionLabel != null)
                    {
                        _descriptionLabel.Text = "Задание не найдено.";
                        _descriptionLabel.TextColor = Colors.Red;
                    }
                    ShowLoading(false);
                    return;
                }

                Console.WriteLine($"   Title: {practice.Title}");
                Console.WriteLine($"   Description: {practice.Description}");
                Console.WriteLine($"   AnswerType: {practice.AnswerType}");
                Console.WriteLine($"   Hint: {practice.Hint}");
                Console.WriteLine($"   StarterCode: {(string.IsNullOrEmpty(practice.StarterCode) ? "null" : "есть")}");

                if (!string.IsNullOrEmpty(practice.Title))
                {
                    PageTitle = practice.Title;
                    Title = PageTitle;
                    if (_titleLabel != null)
                    {
                        _titleLabel.Text = practice.Title;
                        _titleLabel.TextColor = Colors.White;
                    }
                }

                if (_descriptionLabel != null)
                {
                    _descriptionLabel.Text = !string.IsNullOrEmpty(practice.Description)
                        ? practice.Description
                        : "Описание отсутствует";
                    _descriptionLabel.TextColor = Color.FromArgb("#333");
                }

                _expectedAnswer = practice.ExpectedOutput ?? "";
                _answerType = practice.AnswerType?.ToLower() ?? "text";

                Console.WriteLine($"📌 Тип ответа: {_answerType}");

                SetupAnswerInterface();

                if (!string.IsNullOrEmpty(practice.StarterCode))
                {
                    if (_starterCodeEditor != null)
                    {
                        _starterCodeEditor.Text = practice.StarterCode;
                        Console.WriteLine("✅ Стартовый код загружен");
                    }
                    if (_codeSection != null)
                    {
                        _codeSection.IsVisible = true;
                        Console.WriteLine("✅ Секция кода показана");
                    }
                }
                else
                {
                    if (_codeSection != null)
                        _codeSection.IsVisible = false;
                }

                if (!string.IsNullOrEmpty(practice.Hint))
                {
                    if (_hintLabel != null)
                    {
                        _hintLabel.Text = practice.Hint;
                        Console.WriteLine("✅ Подсказка загружена");
                    }
                    if (_hintSection != null)
                    {
                        _hintSection.IsVisible = true;
                        Console.WriteLine("✅ Секция подсказки показана");
                    }
                }
                else
                {
                    if (_hintSection != null)
                        _hintSection.IsVisible = false;
                }

                OnPropertyChanged(nameof(PageTitle));
                ShowLoading(false);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    this.InvalidateMeasure();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки: {ex.Message}");
                await DisplayAlert("Ошибка загрузки", ex.Message, "OK");
                if (_descriptionLabel != null)
                {
                    _descriptionLabel.Text = "Не удалось загрузить задание";
                    _descriptionLabel.TextColor = Colors.Red;
                }
                ShowLoading(false);
            }
        }

        private void SetupAnswerInterface()
        {
            if (_textAnswerSection != null)
                _textAnswerSection.IsVisible = false;

            if (_codeAnswerSection != null)
                _codeAnswerSection.IsVisible = false;

            if (_fileAnswerSection != null)
                _fileAnswerSection.IsVisible = false;

            switch (_answerType.ToLower())
            {
                case "text":
                    if (_textAnswerSection != null)
                        _textAnswerSection.IsVisible = true;
                    Console.WriteLine("📝 Показана секция для текстового ответа");
                    break;

                case "code":
                    if (_codeAnswerSection != null)
                        _codeAnswerSection.IsVisible = true;
                    Console.WriteLine("💻 Показана секция для кода");
                    break;

                case "file":
                    if (_fileAnswerSection != null)
                        _fileAnswerSection.IsVisible = true;
                    Console.WriteLine("📎 Показана секция для загрузки файла");

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_fileAnswerSection != null)
                        {
                            _fileAnswerSection.IsVisible = true;
                            _fileAnswerSection.Opacity = 1;
                            _fileAnswerSection.InputTransparent = false;

                            var parent = _fileAnswerSection.Parent as VisualElement;
                            parent?.InvalidateMeasure();
                        }
                    });
                    break;

                default:
                    Console.WriteLine($"❓ Неизвестный тип ответа: {_answerType}");
                    break;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                (this as VisualElement)?.InvalidateMeasure();
            });
        }

        private void ShowLoading(bool show, string message = "Загрузка...")
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_loadingOverlay != null)
                {
                    _loadingOverlay.IsVisible = show;
                    _loadingOverlay.InputTransparent = !show;
                }

                if (_loadingIndicator != null)
                    _loadingIndicator.IsRunning = show;

                if (_loadingLabel != null && !string.IsNullOrEmpty(message))
                    _loadingLabel.Text = message;

                if (_submitButton != null)
                    _submitButton.IsEnabled = !show;
            });
        }

        private async void OnSelectFileClicked(object sender, EventArgs e)
        {
            try
            {
                var options = new PickOptions
                {
                    PickerTitle = "Выберите файл с решением",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".zip", ".pdf", ".jpg", ".jpeg", ".png", ".txt", ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt" } },
                        { DevicePlatform.Android, new[] { "*/*" } },
                        { DevicePlatform.iOS, new[] { "public.data" } }
                    })
                };

                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    _selectedFile = result;

                    using var stream = await result.OpenReadAsync();
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    _selectedFileBytes = ms.ToArray();

                    if (_selectedFileNameLabel != null)
                        _selectedFileNameLabel.Text = result.FileName;

                    if (_selectedFileSizeLabel != null)
                        _selectedFileSizeLabel.Text = FormatFileSize(_selectedFileBytes.Length);

                    if (_fileIconLabel != null)
                        _fileIconLabel.Text = GetFileIcon(Path.GetExtension(result.FileName));

                    if (_filePreviewSection != null)
                        _filePreviewSection.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выбрать файл: {ex.Message}", "OK");
            }
        }

        private string GetFileIcon(string extension)
        {
            return extension?.ToLower() switch
            {
                ".pdf" => "📄",
                ".doc" or ".docx" => "📝",
                ".xls" or ".xlsx" => "📊",
                ".ppt" or ".pptx" => "📽️",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "🖼️",
                ".zip" or ".rar" or ".7z" => "🗜️",
                ".txt" => "📃",
                ".mp4" or ".avi" or ".mov" => "🎬",
                _ => "📎"
            };
        }

        private void OnRemoveFileClicked(object sender, EventArgs e)
        {
            _selectedFile = null;
            _selectedFileBytes = null;
            if (_filePreviewSection != null)
                _filePreviewSection.IsVisible = false;
        }

        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            if (_isSubmitting) return;

            try
            {
                _isSubmitting = true;

                if (!await ValidateSubmission())
                {
                    _isSubmitting = false;
                    return;
                }

                ShowLoading(true, "Отправка работы...");

                bool success = await SaveSubmission();
                if (success)
                {
                    ShowResult(true, "✅ Работа успешно отправлена на проверку!");

                    if (_courseId > 0)
                    {
                        await _dbService.UpdateProgressAsync(_currentUser.UserId, _courseId, "in_progress");
                    }

                    if (_submitButton != null)
                        _submitButton.IsEnabled = false;
                }
                else
                {
                    ShowResult(false, "❌ Не удалось отправить работу");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка отправки", ex.Message, "OK");
                ShowResult(false, $"Ошибка: {ex.Message}");
            }
            finally
            {
                _isSubmitting = false;
                ShowLoading(false);
            }
        }

        private async Task<bool> ValidateSubmission()
        {
            switch (_answerType.ToLower())
            {
                case "text":
                    if (string.IsNullOrWhiteSpace(_textAnswerEditor?.Text))
                    {
                        await DisplayAlert("Ошибка", "Введите текстовый ответ", "OK");
                        return false;
                    }
                    break;

                case "code":
                    if (string.IsNullOrWhiteSpace(_codeAnswerEditor?.Text))
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

                    if (_selectedFileBytes.Length > 50 * 1024 * 1024)
                    {
                        await DisplayAlert("Ошибка", "Файл слишком большой (максимум 50 МБ)", "OK");
                        return false;
                    }

                    Console.WriteLine($"📎 Выбран файл: {_selectedFile.FileName}, размер: {_selectedFileBytes.Length} байт");
                    break;

                default:
                    await DisplayAlert("Ошибка", "Неизвестный тип ответа", "OK");
                    return false;
            }
            return true;
        }

        private async Task<bool> SaveSubmission()
        {
            try
            {
                string? answerText = null;
                string? answerFilePath = null;

                switch (_answerType.ToLower())
                {
                    case "text":
                        answerText = _textAnswerEditor?.Text?.Trim();
                        break;

                    case "code":
                        answerText = _codeAnswerEditor?.Text?.Trim();
                        break;

                    case "file":
                        answerFilePath = await SaveSubmissionFile();
                        if (string.IsNullOrEmpty(answerFilePath))
                        {
                            return false;
                        }
                        break;
                }

                bool success = await _dbService.SavePracticeSubmissionAsync(
                    _lessonId,
                    _currentUser.UserId,
                    answerText,
                    answerFilePath
                );

                if (success)
                {
                    Console.WriteLine($"✅ Работа отправлена: LessonId={_lessonId}, StudentId={_currentUser.UserId}");
                    return true;
                }
                else
                {
                    Console.WriteLine("❌ Не удалось сохранить работу в БД");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения: {ex.Message}");
                return false;
            }
        }

        private async Task<string?> SaveSubmissionFile()
        {
            try
            {
                var submissionsFolder = Path.Combine(FileSystem.AppDataDirectory, "PracticeSubmissions");
                if (!Directory.Exists(submissionsFolder))
                    Directory.CreateDirectory(submissionsFolder);

                string fileExtension = Path.GetExtension(_selectedFile!.FileName);
                string fileName = $"submission_{_currentUser.UserId}_{_lessonId}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";
                string fullPath = Path.Combine(submissionsFolder, fileName);

                await File.WriteAllBytesAsync(fullPath, _selectedFileBytes!);

                Console.WriteLine($"✅ Файл сохранен: {fullPath}");
                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения файла: {ex.Message}");
                return null;
            }
        }

        private void ShowResult(bool success, string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_resultSection != null)
                {
                    _resultSection.IsVisible = true;
                    _resultSection.BackgroundColor = success ? Color.FromArgb("#E8F5E8") : Color.FromArgb("#FFEBEE");
                    _resultSection.Stroke = success ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
                }

                if (_resultIconLabel != null)
                    _resultIconLabel.Text = success ? "✅" : "❌";

                if (_resultLabel != null)
                {
                    _resultLabel.Text = success ? "Успех!" : "Ошибка";
                    _resultLabel.TextColor = success ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
                }

                if (_feedbackLabel != null)
                    _feedbackLabel.Text = message;
            });
        }

        private async Task LoadGradeIfExistsAsync()
        {
            try
            {
                var submissions = await _dbService.GetStudentSubmissionsAsync(_currentUser.UserId);
                var currentSubmission = submissions?.FirstOrDefault(s => s.LessonId == _lessonId && s.Status == "graded");

                if (currentSubmission != null && _gradeSection != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _gradeSection.IsVisible = true;

                        if (_gradeScoreLabel != null)
                            _gradeScoreLabel.Text = $"{currentSubmission.TeacherScore} / 100";

                        if (_teacherCommentLabel != null)
                            _teacherCommentLabel.Text = currentSubmission.TeacherComment ?? "Без комментария";

                        if (_gradedByLabel != null)
                            _gradedByLabel.Text = currentSubmission.TeacherName ?? "Преподаватель";

                        if (_submitButton != null)
                            _submitButton.IsEnabled = false;
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки оценки: {ex.Message}");
            }
        }

        private async void OnRunCodeClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Запуск кода", "Функция выполнения кода будет добавлена позже", "OK");
        }

        private void OnClearCodeClicked(object sender, EventArgs e)
        {
            if (_codeAnswerEditor != null)
                _codeAnswerEditor.Text = "";
        }

        private async void OnCopyCodeClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_starterCodeEditor?.Text))
                return;

            await Clipboard.SetTextAsync(_starterCodeEditor.Text);
            await DisplayAlert("Успех", "Стартовый код скопирован в буфер обмена", "OK");
        }

        private async Task LoadAttachmentsAsync()
        {
            try
            {
                var attachments = await _dbService.GetLessonAttachmentsAsync(_lessonId);

                MainThread.BeginInvokeOnMainThread(() =>
                {
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

                        if (_attachmentsSection != null)
                            _attachmentsSection.IsVisible = true;
                    }
                    else
                    {
                        if (_attachmentsSection != null)
                            _attachmentsSection.IsVisible = false;
                    }
                });
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
                "📥 Скачать",
                "📄 Открыть"
            );

            if (choice == "📥 Скачать")
            {
                await DownloadAttachment(vm);
            }
            else if (choice == "📄 Открыть")
            {
                await OpenAttachment(vm);
            }
        }

        private async Task DownloadAttachment(AttachmentViewModel vm)
        {
            try
            {
                ShowLoading(true, "Скачивание...");

                var path = await _fileService.ResolveFilePath(vm.FilePath, vm.FileName, "PracticeFiles");
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    await DisplayAlert("Ошибка", "Файл не найден", "OK");
                    return;
                }

                var downloadsFolder = Path.Combine(FileSystem.AppDataDirectory, "Downloads");
                if (!Directory.Exists(downloadsFolder))
                    Directory.CreateDirectory(downloadsFolder);

                var destPath = Path.Combine(downloadsFolder, vm.FileName);
                File.Copy(path, destPath, true);

                await DisplayAlert("Успех", $"Файл сохранен в папку Downloads", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async Task OpenAttachment(AttachmentViewModel vm)
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

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} Б";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.0} КБ";
            return $"{bytes / (1024.0 * 1024.0):0.0} МБ";
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}