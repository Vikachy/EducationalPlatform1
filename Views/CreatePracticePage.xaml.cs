using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace EducationalPlatform.Views
{
    public partial class CreatePracticePage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private readonly int _courseId;

        // Используем EnhancedFileAttachment из Models
        public ObservableCollection<EnhancedFileAttachment> Attachments { get; set; } = new();

        public CreatePracticePage(User user, DatabaseService dbService, SettingsService settingsService, int courseId)
        {
            InitializeComponent();
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _fileService = ServiceHelper.GetService<FileService>();
            _courseId = courseId;

            AnswerTypePicker.SelectedIndex = 0;
            AnswerTypePicker.SelectedIndexChanged += OnAnswerTypeChanged;

            // Инициализируем секцию прикрепленных файлов как в теории
            BindingContext = this;
            AttachmentsCollection.ItemsSource = Attachments;
            Attachments.CollectionChanged += (s, e) =>
            {
                AttachmentsCollection.IsVisible = Attachments.Any();
                ClearAllButton.IsVisible = Attachments.Any();
                UploadProgressSection.IsVisible = false;
            };

            OnAnswerTypeChanged(null, EventArgs.Empty);
        }

        private void OnAnswerTypeChanged(object sender, EventArgs e)
        {
            var selectedType = AnswerTypePicker.SelectedItem as string;
            CodeSection.IsVisible = selectedType == "code";
            FileSettingsSection.IsVisible = selectedType == "file";
        }

        // ВЫБОР ФАЙЛОВ ДЛЯ ПРИКРЕПЛЕНИЯ (как в теории)
        private async void OnSelectFilesClicked(object sender, EventArgs e)
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
            UploadProgressSection.IsVisible = true;
            UploadProgressBar.Progress = 0;
            UploadStatusLabel.Text = "Подготовка файлов...";

            int processed = 0;
            int total = files.Count();

            foreach (var file in files)
            {
                try
                {
                    UploadStatusLabel.Text = $"Обработка: {file.FileName}";

                    // Создаем объект вложения со статусом загрузки
                    var attachment = new EnhancedFileAttachment
                    {
                        FileName = file.FileName,
                        FileIcon = _fileService.GetFileIcon(Path.GetExtension(file.FileName)),
                        IsUploading = true,
                        StatusIcon = "⏳"
                    };

                    Attachments.Add(attachment);

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
                        Console.WriteLine($"Не удалось определить размер файла: {ex.Message}");
                    }

                    if (fileSize > 50 * 1024 * 1024)
                    {
                        await DisplayAlert("Предупреждение", $"Файл {file.FileName} слишком большой (максимум 50 МБ)", "OK");
                        Attachments.Remove(attachment);
                        processed++;
                        continue;
                    }

                    attachment.FileSize = fileSize;

                    // Читаем файл в байты
                    byte[] fileBytes;
                    using (var stream = await file.OpenReadAsync())
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }

                    // Сохраняем файл в памяти для последующего сохранения
                    attachment.FileBytes = fileBytes;
                    attachment.IsUploading = false;
                    attachment.StatusIcon = "✅";

                    processed++;
                    UploadProgressBar.Progress = (double)processed / total;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось обработать файл {file.FileName}: {ex.Message}", "OK");
                    processed++;
                }
            }

            UploadProgressSection.IsVisible = false;
            UploadProgressBar.Progress = 0;
            UploadStatusLabel.Text = "";

            if (Attachments.Any(a => !a.IsUploading))
            {
                await DisplayAlert("Готово", $"Успешно обработано {Attachments.Count(a => !a.IsUploading)} файлов", "OK");
            }
        }

        private void OnRemoveAttachmentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is EnhancedFileAttachment attachment)
            {
                Attachments.Remove(attachment);
            }
        }

        private async void OnClearAllFilesClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Подтверждение",
                "Удалить все прикрепленные файлы?", "Да", "Нет");

            if (confirm)
            {
                Attachments.Clear();
            }
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TitleEntry.Text))
                {
                    await DisplayAlert("Ошибка", "Введите название задания", "OK");
                    return;
                }

                var answerType = AnswerTypePicker.SelectedItem as string ?? "text";

                // Настройки для файлов-ответов студента
                var maxFileSize = 10;
                var allowedTypes = new List<string>();

                if (answerType == "file")
                {
                    if (!int.TryParse(MaxFileSizeEntry.Text, out maxFileSize) || maxFileSize <= 0)
                    {
                        await DisplayAlert("Ошибка", "Укажите корректный максимальный размер файла", "OK");
                        return;
                    }

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
                }

                var starterCode = answerType == "code" ? StarterCodeEditor.Text?.Trim() : null;

                var lessonId = await _dbService.AddPracticeWithAnswerTypeAsync(
                    _courseId,
                    TitleEntry.Text.Trim(),
                    DescriptionEditor.Text?.Trim() ?? string.Empty,
                    answerType,
                    starterCode,
                    ExpectedAnswerEditor.Text?.Trim(),
                    HintEditor.Text?.Trim(),
                    maxFileSize,
                    string.Join(";", allowedTypes)
                );

                if (lessonId.HasValue && lessonId.Value > 0)
                {
                    // Если есть прикрепленные файлы – сохраняем их как вложения урока
                    if (Attachments.Any())
                    {
                        foreach (var attachment in Attachments.Where(a => a.FileBytes != null).ToList())
                        {
                            try
                            {
                                var fileType = Path.GetExtension(attachment.FileName);
                                var fileSizeFormatted = FormatFileSize(attachment.FileSize);

                                // Временно сохраняем как вложения урока, пока нет метода для практики
                                var savedAttachment = await _dbService.AddLessonAttachmentAsync(
                                    lessonId.Value,
                                    attachment.FileName,
                                    fileType,
                                    fileSizeFormatted,
                                    attachment.FileBytes);

                                if (savedAttachment != null)
                                {
                                    Console.WriteLine($"✅ Файл прикреплен: {attachment.FileName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Ошибка прикрепления файла {attachment.FileName}: {ex.Message}");
                            }
                        }
                    }

                    await DisplayAlert("Успех", "Практическое задание создано!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось создать практическое задание", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка создания: {ex.Message}", "OK");
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