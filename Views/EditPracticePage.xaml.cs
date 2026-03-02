using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class EditPracticePage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private readonly int _lessonId;
        private PracticeDto? _practice;

        // Элементы управления
        private Grid? _loadingOverlay;
        private ActivityIndicator? _loadingIndicator;
        private Label? _loadingLabel;
        private Entry? _titleEntry;
        private Editor? _descriptionEditor;
        private Picker? _answerTypePicker;
        private StackLayout? _codeSection;
        private Editor? _starterCodeEditor;
        private Editor? _expectedAnswerEditor;
        private StackLayout? _fileSettingsSection;
        private Entry? _maxFileSizeEntry;
        private CheckBox? _zipCheckBox;
        private CheckBox? _imageCheckBox;
        private CheckBox? _pdfCheckBox;
        private CheckBox? _docCheckBox;
        private CheckBox? _txtCheckBox;
        private CheckBox? _powerPointCheckBox;
        private Editor? _hintEditor;
        private CollectionView? _attachmentsCollection;
        private Button? _clearAllButton;
        private ProgressBar? _uploadProgressBar;
        private Label? _uploadStatusLabel;

        public ObservableCollection<PracticeFileAttachment> Attachments { get; set; } = new();

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public EditPracticePage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации EditPracticePage: {ex.Message}");
            }

            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _fileService = ServiceHelper.GetService<FileService>();
            _lessonId = lessonId;

            // Инициализируем элементы управления
            InitializeControls();

            if (_attachmentsCollection != null)
                _attachmentsCollection.ItemsSource = Attachments;

            if (_answerTypePicker != null)
                _answerTypePicker.SelectedIndexChanged += OnAnswerTypeChanged;

            Attachments.CollectionChanged += (s, e) =>
            {
                if (_clearAllButton != null)
                    _clearAllButton.IsVisible = Attachments.Any();
                if (_attachmentsCollection != null)
                    _attachmentsCollection.IsVisible = Attachments.Any();
            };

            LoadPracticeData();
            LoadAttachments();
        }

        private void InitializeControls()
        {
            _loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
            _loadingIndicator = this.FindByName<ActivityIndicator>("LoadingIndicator");
            _loadingLabel = this.FindByName<Label>("LoadingLabel");
            _titleEntry = this.FindByName<Entry>("TitleEntry");
            _descriptionEditor = this.FindByName<Editor>("DescriptionEditor");
            _answerTypePicker = this.FindByName<Picker>("AnswerTypePicker");
            _codeSection = this.FindByName<StackLayout>("CodeSection");
            _starterCodeEditor = this.FindByName<Editor>("StarterCodeEditor");
            _expectedAnswerEditor = this.FindByName<Editor>("ExpectedAnswerEditor");
            _fileSettingsSection = this.FindByName<StackLayout>("FileSettingsSection");
            _maxFileSizeEntry = this.FindByName<Entry>("MaxFileSizeEntry");
            _zipCheckBox = this.FindByName<CheckBox>("ZipCheckBox");
            _imageCheckBox = this.FindByName<CheckBox>("ImageCheckBox");
            _pdfCheckBox = this.FindByName<CheckBox>("PdfCheckBox");
            _docCheckBox = this.FindByName<CheckBox>("DocCheckBox");
            _txtCheckBox = this.FindByName<CheckBox>("TxtCheckBox");
            _powerPointCheckBox = this.FindByName<CheckBox>("PowerPointCheckBox");
            _hintEditor = this.FindByName<Editor>("HintEditor");
            _attachmentsCollection = this.FindByName<CollectionView>("AttachmentsCollection");
            _clearAllButton = this.FindByName<Button>("ClearAllButton");
            _uploadProgressBar = this.FindByName<ProgressBar>("UploadProgressBar");
            _uploadStatusLabel = this.FindByName<Label>("UploadStatusLabel");

            // Устанавливаем начальные значения чекбоксов
            if (_zipCheckBox != null) _zipCheckBox.IsChecked = true;
            if (_imageCheckBox != null) _imageCheckBox.IsChecked = true;
            if (_pdfCheckBox != null) _pdfCheckBox.IsChecked = true;
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
            });
        }

        private async void LoadPracticeData()
        {
            try
            {
                ShowLoading(true, "Загрузка данных...");
                _practice = await _dbService.GetPracticeExerciseWithLessonDataAsync(_lessonId);

                if (_practice != null)
                {
                    if (_titleEntry != null) _titleEntry.Text = _practice.Title;
                    if (_descriptionEditor != null) _descriptionEditor.Text = _practice.Description;

                    if (_answerTypePicker != null && !string.IsNullOrEmpty(_practice.AnswerType))
                    {
                        var index = _answerTypePicker.Items.IndexOf(_practice.AnswerType);
                        if (index >= 0)
                            _answerTypePicker.SelectedIndex = index;
                    }

                    if (_starterCodeEditor != null) _starterCodeEditor.Text = _practice.StarterCode;
                    if (_expectedAnswerEditor != null) _expectedAnswerEditor.Text = _practice.ExpectedOutput;
                    if (_hintEditor != null) _hintEditor.Text = _practice.Hint;

                    // Загружаем настройки файлов если они есть
                    if (_practice.AnswerType == "file" && _practice.MaxFileSize > 0)
                    {
                        if (_maxFileSizeEntry != null)
                            _maxFileSizeEntry.Text = _practice.MaxFileSize.ToString();

                        if (!string.IsNullOrEmpty(_practice.AllowedFileTypes) && _zipCheckBox != null && _imageCheckBox != null && _pdfCheckBox != null)
                        {
                            var types = _practice.AllowedFileTypes.Split(';', StringSplitOptions.RemoveEmptyEntries);
                            if (_zipCheckBox != null) _zipCheckBox.IsChecked = types.Contains(".zip");
                            if (_imageCheckBox != null) _imageCheckBox.IsChecked = types.Any(t => t == ".jpg" || t == ".jpeg" || t == ".png" || t == ".gif");
                            if (_pdfCheckBox != null) _pdfCheckBox.IsChecked = types.Contains(".pdf");
                            if (_docCheckBox != null) _docCheckBox.IsChecked = types.Contains(".doc") || types.Contains(".docx");
                            if (_txtCheckBox != null) _txtCheckBox.IsChecked = types.Contains(".txt");
                            if (_powerPointCheckBox != null) _powerPointCheckBox.IsChecked = types.Contains(".ppt") || types.Contains(".pptx");
                        }
                    }

                    OnAnswerTypeChanged(null, EventArgs.Empty);
                }
                else
                {
                    await DisplayAlert("Ошибка", "Практическое задание не найдено", "OK");
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные задания: {ex.Message}", "OK");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void OnAnswerTypeChanged(object? sender, EventArgs? e)
        {
            var selectedType = _answerTypePicker?.SelectedItem as string;

            if (_codeSection != null)
                _codeSection.IsVisible = selectedType == "code";

            if (_fileSettingsSection != null)
                _fileSettingsSection.IsVisible = selectedType == "file";
        }

        private async void LoadAttachments()
        {
            try
            {
                var attachments = await _dbService.GetLessonAttachmentsAsync(_lessonId);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Attachments.Clear();

                    if (attachments != null && attachments.Any())
                    {
                        foreach (var att in attachments)
                        {
                            Attachments.Add(new PracticeFileAttachment
                            {
                                AttachmentId = att.AttachmentId,
                                FileName = att.FileName,
                                FilePath = att.FilePath,
                                FileSize = ParseFileSize(att.FileSize),
                                SizeFormatted = att.FileSize,
                                UploadDate = att.UploadDate,
                                FileType = att.FileType,
                                FileIcon = _fileService.GetFileIcon(att.FileType)
                            });
                        }

                        if (_attachmentsCollection != null)
                            _attachmentsCollection.IsVisible = true;
                        if (_clearAllButton != null)
                            _clearAllButton.IsVisible = true;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки вложений: {ex.Message}");
            }
        }

        private async void OnAttachFilesClicked(object sender, EventArgs e)
        {
            try
            {
                var options = new PickOptions
                {
                    PickerTitle = "Выберите файлы для прикрепления",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".zip", ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".png", ".jpg", ".jpeg", ".txt", ".mp4" } },
                        { DevicePlatform.Android, new[] { "*/*" } },
                        { DevicePlatform.iOS, new[] { "public.data" } }
                    })
                };

                var results = await FilePicker.Default.PickMultipleAsync(options);
                if (results != null && results.Any())
                {
                    await ProcessSelectedFiles(results);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выбрать файлы: {ex.Message}", "OK");
            }
        }

        private async Task ProcessSelectedFiles(IEnumerable<FileResult> files)
        {
            if (_uploadProgressBar != null)
            {
                _uploadProgressBar.IsVisible = true;
                _uploadProgressBar.Progress = 0;
            }
            if (_uploadStatusLabel != null)
            {
                _uploadStatusLabel.IsVisible = true;
                _uploadStatusLabel.Text = "Обработка файлов...";
            }

            int processed = 0;
            int total = files.Count();

            foreach (var file in files)
            {
                try
                {
                    if (_uploadStatusLabel != null)
                        _uploadStatusLabel.Text = $"Обработка: {file.FileName}";

                    // Проверяем размер файла
                    long fileSize = 0;
                    using (var stream = await file.OpenReadAsync())
                    {
                        fileSize = stream.Length;
                    }

                    if (fileSize > 50 * 1024 * 1024)
                    {
                        await DisplayAlert("Предупреждение", $"Файл {file.FileName} слишком большой (максимум 50 МБ)", "OK");
                        processed++;
                        continue;
                    }

                    // Читаем файл в байты
                    byte[] fileBytes;
                    using (var stream = await file.OpenReadAsync())
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }

                    var addedAttachment = await _dbService.AddLessonAttachmentAsync(
                        _lessonId,
                        file.FileName,
                        Path.GetExtension(file.FileName),
                        FormatFileSize(fileBytes.Length),
                        fileBytes);

                    if (addedAttachment != null)
                    {
                        var attachment = new PracticeFileAttachment
                        {
                            AttachmentId = addedAttachment.AttachmentId,
                            FileName = addedAttachment.FileName,
                            FilePath = addedAttachment.FilePath,
                            FileSize = fileBytes.Length,
                            SizeFormatted = FormatFileSize(fileBytes.Length),
                            UploadDate = addedAttachment.UploadDate,
                            FileType = addedAttachment.FileType,
                            FileIcon = _fileService.GetFileIcon(addedAttachment.FileType)
                        };

                        Attachments.Add(attachment);
                    }

                    processed++;
                    if (_uploadProgressBar != null)
                        _uploadProgressBar.Progress = (double)processed / total;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось обработать файл {file.FileName}: {ex.Message}", "OK");
                    processed++;
                }
            }

            if (_uploadProgressBar != null)
                _uploadProgressBar.IsVisible = false;
            if (_uploadStatusLabel != null)
                _uploadStatusLabel.IsVisible = false;
        }

        private async void OnRemoveAttachmentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is PracticeFileAttachment attachment)
            {
                bool confirm = await DisplayAlert("Подтверждение",
                    $"Удалить файл {attachment.FileName}?", "Да", "Нет");

                if (confirm)
                {
                    await RemoveAttachment(attachment);
                }
            }
        }

        private async void OnClearAllFilesClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Подтверждение",
                "Удалить все прикрепленные файлы?", "Да", "Нет");

            if (confirm)
            {
                foreach (var attachment in Attachments.ToList())
                {
                    await RemoveAttachment(attachment);
                }
            }
        }

        private async Task RemoveAttachment(PracticeFileAttachment attachment)
        {
            try
            {
                var success = await _dbService.DeleteLessonAttachmentAsync(attachment.AttachmentId);
                if (success)
                {
                    Attachments.Remove(attachment);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка удаления файла: {ex.Message}", "OK");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (_titleEntry == null || string.IsNullOrWhiteSpace(_titleEntry.Text))
                {
                    await DisplayAlert("Ошибка", "Введите название задания", "OK");
                    return;
                }

                if (_practice == null)
                {
                    await DisplayAlert("Ошибка", "Задание не найдено", "OK");
                    return;
                }

                var answerType = _answerTypePicker?.SelectedItem?.ToString() ?? "text";

                // Настройки для файлов
                var maxFileSize = 10;
                var allowedFileTypes = "";

                if (answerType == "file")
                {
                    if (_maxFileSizeEntry != null && !int.TryParse(_maxFileSizeEntry.Text, out maxFileSize))
                    {
                        maxFileSize = 10;
                    }

                    var allowedTypes = new List<string>();
                    if (_zipCheckBox?.IsChecked == true) allowedTypes.Add(".zip");
                    if (_imageCheckBox?.IsChecked == true)
                    {
                        allowedTypes.Add(".jpg");
                        allowedTypes.Add(".jpeg");
                        allowedTypes.Add(".png");
                        allowedTypes.Add(".gif");
                    }
                    if (_pdfCheckBox?.IsChecked == true) allowedTypes.Add(".pdf");
                    if (_docCheckBox?.IsChecked == true)
                    {
                        allowedTypes.Add(".doc");
                        allowedTypes.Add(".docx");
                    }
                    if (_txtCheckBox?.IsChecked == true) allowedTypes.Add(".txt");
                    if (_powerPointCheckBox?.IsChecked == true)
                    {
                        allowedTypes.Add(".ppt");
                        allowedTypes.Add(".pptx");
                    }

                    if (!allowedTypes.Any())
                    {
                        await DisplayAlert("Ошибка", "Выберите хотя бы один разрешенный тип файла", "OK");
                        return;
                    }

                    allowedFileTypes = string.Join(";", allowedTypes);
                }

                // Обновляем объект практики
                _practice.Title = _titleEntry.Text;
                _practice.Description = _descriptionEditor?.Text;
                _practice.AnswerType = answerType;
                _practice.StarterCode = _starterCodeEditor?.Text;
                _practice.ExpectedOutput = _expectedAnswerEditor?.Text;
                _practice.Hint = _hintEditor?.Text;
                _practice.MaxFileSize = maxFileSize;
                _practice.AllowedFileTypes = allowedFileTypes;

                ShowLoading(true, "Сохранение...");

                var success = await _dbService.UpdatePracticeExerciseAsync(_practice);

                if (success)
                {
                    await DisplayAlert("Успех", "Практическое задание успешно обновлено", "OK");
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
            finally
            {
                ShowLoading(false);
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private long ParseFileSize(string fileSize)
        {
            if (string.IsNullOrEmpty(fileSize)) return 0;

            try
            {
                var sizeText = fileSize.Replace(" Б", "").Replace(" КБ", "").Replace(" МБ", "").Replace(" ГБ", "")
                                      .Replace(" B", "").Replace(" KB", "").Replace(" MB", "").Replace(" GB", "")
                                      .Trim();

                if (double.TryParse(sizeText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double size))
                {
                    if (fileSize.Contains("КБ") || fileSize.Contains("KB")) return (long)(size * 1024);
                    if (fileSize.Contains("МБ") || fileSize.Contains("MB")) return (long)(size * 1024 * 1024);
                    if (fileSize.Contains("ГБ") || fileSize.Contains("GB")) return (long)(size * 1024 * 1024 * 1024);
                    return (long)size;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга размера файла: {ex.Message}");
            }

            return 0;
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} Б";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.0} КБ";
            return $"{bytes / (1024.0 * 1024.0):0.0} МБ";
        }
    }
}