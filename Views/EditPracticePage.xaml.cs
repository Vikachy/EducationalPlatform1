using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace EducationalPlatform.Views
{
    public partial class EditPracticePage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private readonly int _lessonId;
        private PracticeDto _practice;

        public ObservableCollection<PracticeFileAttachment> Attachments { get; set; } = new();

        public EditPracticePage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
        {
            InitializeComponent();
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _fileService = ServiceHelper.GetService<FileService>();
            _lessonId = lessonId;

            BindingContext = this;
            AttachmentsCollection.ItemsSource = Attachments;

            // Обновляем видимость кнопок при изменении коллекции
            Attachments.CollectionChanged += (s, e) =>
            {
                ClearAllButton.IsVisible = Attachments.Any();
                AttachmentsCollection.IsVisible = Attachments.Any();
            };

            AnswerTypePicker.SelectedIndexChanged += OnAnswerTypeChanged;

            LoadPracticeData();
            LoadAttachments();
        }

        private async void LoadPracticeData()
        {
            try
            {
                _practice = await _dbService.GetPracticeExerciseWithLessonDataAsync(_lessonId);

                if (_practice != null)
                {
                    TitleEntry.Text = _practice.Title;
                    DescriptionEditor.Text = _practice.Description;

                    if (!string.IsNullOrEmpty(_practice.AnswerType))
                    {
                        var index = AnswerTypePicker.Items.IndexOf(_practice.AnswerType);
                        if (index >= 0)
                            AnswerTypePicker.SelectedIndex = index;
                    }

                    StarterCodeEditor.Text = _practice.StarterCode;
                    ExpectedAnswerEditor.Text = _practice.ExpectedOutput;
                    HintEditor.Text = _practice.Hint;

                    // Загружаем настройки файлов если они есть
                    if (_practice.AnswerType == "file")
                    {
                        if (_practice.MaxFileSize > 0)
                            MaxFileSizeEntry.Text = _practice.MaxFileSize.ToString();

                        if (!string.IsNullOrEmpty(_practice.AllowedFileTypes))
                        {
                            var types = _practice.AllowedFileTypes.Split(';');
                            ZipCheckBox.IsChecked = types.Contains(".zip");
                            ImageCheckBox.IsChecked = types.Any(t => t == ".jpg" || t == ".jpeg" || t == ".png" || t == ".gif");
                            PdfCheckBox.IsChecked = types.Contains(".pdf");
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
        }

        private void OnAnswerTypeChanged(object sender, EventArgs e)
        {
            var selectedType = AnswerTypePicker.SelectedItem as string;
            CodeSection.IsVisible = selectedType == "code";
            FileSettingsSection.IsVisible = selectedType == "file";
        }

        private async void LoadAttachments()
        {
            try
            {
                Attachments.Clear();
                var attachments = await _dbService.GetLessonAttachmentsAsync(_lessonId);

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
                            UploadDate = att.UploadDate,
                            FileType = att.FileType
                        });
                    }

                    Console.WriteLine($"✅ Загружено {Attachments.Count} вложений");
                }
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
                var fileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".zip", ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".png", ".jpg", ".jpeg", ".txt", ".mp4" } },
                        { DevicePlatform.macOS, new[] { ".zip", ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".png", ".jpg", ".jpeg", ".txt", ".mp4" } },
                        { DevicePlatform.Android, new[] { "*/*" } },
                        { DevicePlatform.iOS, new[] { "public.data" } }
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Выберите файлы для прикрепления",
                    FileTypes = fileTypes
                };

                var results = await PickFilesSequentially(options);

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

        private async Task<List<FileResult>> PickFilesSequentially(PickOptions options)
        {
            var files = new List<FileResult>();
            bool continueSelecting = true;

            while (continueSelecting && files.Count < 10)
            {
                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    files.Add(result);

                    if (files.Count < 10)
                    {
                        continueSelecting = await DisplayAlert("Файлы",
                            $"Добавлено файлов: {files.Count}. Добавить еще?", "Да", "Нет");
                    }
                    else
                    {
                        await DisplayAlert("Информация", "Достигнут лимит в 10 файлов", "OK");
                        continueSelecting = false;
                    }
                }
                else
                {
                    continueSelecting = false;
                }
            }

            return files;
        }

        private async Task ProcessSelectedFiles(IEnumerable<FileResult> files)
        {
            UploadProgressBar.IsVisible = true;
            UploadStatusLabel.IsVisible = true;
            UploadProgressBar.Progress = 0;

            int processed = 0;
            int total = files.Count();

            foreach (var file in files)
            {
                try
                {
                    UploadStatusLabel.Text = $"Обработка: {file.FileName}";

                    // Проверяем размер файла
                    long fileSize = 0;
                    try
                    {
                        if (!string.IsNullOrEmpty(file.FullPath) && File.Exists(file.FullPath))
                        {
                            var fileInfo = new FileInfo(file.FullPath);
                            fileSize = fileInfo.Length;
                        }
                        else
                        {
                            using var stream = await file.OpenReadAsync();
                            fileSize = stream.Length;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Не удалось определить размер файла: {ex.Message}");
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
                            UploadDate = addedAttachment.UploadDate,
                            FileType = addedAttachment.FileType
                        };

                        Attachments.Add(attachment);
                        Console.WriteLine($"✅ Файл прикреплен: {file.FileName}");
                    }

                    processed++;
                    UploadProgressBar.Progress = (double)processed / total;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось обработать файл {file.FileName}: {ex.Message}", "OK");
                    processed++;
                }
            }

            UploadProgressBar.IsVisible = false;
            UploadStatusLabel.IsVisible = false;
            UploadProgressBar.Progress = 0;
            UploadStatusLabel.Text = "";
        }

        private async void OnRemoveAttachmentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is PracticeFileAttachment attachment)
            {
                try
                {
                    bool confirm = await DisplayAlert("Подтверждение",
                        $"Удалить файл {attachment.FileName}?", "Да", "Нет");

                    if (confirm)
                    {
                        await RemoveAttachment(attachment);
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось удалить файл: {ex.Message}", "OK");
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
                Attachments.Clear();
            }
        }

        private async Task RemoveAttachment(PracticeFileAttachment attachment)
        {
            try
            {
                var success = await _dbService.DeleteLessonAttachmentAsync(attachment.AttachmentId);
                if (success)
                {
                    // Удаляем локальный файл если он существует
                    var resolvedPath = await _fileService.ResolveFilePath(attachment.FilePath, attachment.FileName, "PracticeFiles");
                    if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
                    {
                        File.Delete(resolvedPath);
                    }

                    Attachments.Remove(attachment);
                    await DisplayAlert("Успех", $"Файл {attachment.FileName} удален", "OK");
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось удалить файл из базы данных", "OK");
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
                if (string.IsNullOrWhiteSpace(TitleEntry.Text))
                {
                    await DisplayAlert("Ошибка", "Введите название задания", "OK");
                    return;
                }

                if (_practice == null)
                {
                    await DisplayAlert("Ошибка", "Задание не найдено", "OK");
                    return;
                }

                var answerType = AnswerTypePicker.SelectedItem?.ToString() ?? "text";

                // Настройки для файлов
                var maxFileSize = 10;
                var allowedFileTypes = "";

                if (answerType == "file")
                {
                    if (!int.TryParse(MaxFileSizeEntry.Text, out maxFileSize) || maxFileSize <= 0)
                    {
                        await DisplayAlert("Ошибка", "Укажите корректный максимальный размер файла", "OK");
                        return;
                    }

                    var allowedTypes = new List<string>();
                    if (ZipCheckBox.IsChecked) allowedTypes.Add(".zip");
                    if (ImageCheckBox.IsChecked)
                    {
                        allowedTypes.Add(".jpg");
                        allowedTypes.Add(".jpeg");
                        allowedTypes.Add(".png");
                        allowedTypes.Add(".gif");
                    }
                    if (PdfCheckBox.IsChecked) allowedTypes.Add(".pdf");

                    if (!allowedTypes.Any())
                    {
                        await DisplayAlert("Ошибка", "Выберите хотя бы один разрешенный тип файла", "OK");
                        return;
                    }

                    allowedFileTypes = string.Join(";", allowedTypes);
                }

                _practice.Title = TitleEntry.Text;
                _practice.Description = DescriptionEditor.Text;
                _practice.AnswerType = answerType;
                _practice.StarterCode = StarterCodeEditor.Text;
                _practice.ExpectedOutput = ExpectedAnswerEditor.Text;
                _practice.Hint = HintEditor.Text;
                _practice.MaxFileSize = maxFileSize;
                _practice.AllowedFileTypes = allowedFileTypes;

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