using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Maui;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class CreatePracticePage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _courseId;

        // Используем PracticeFileAttachment из Models
        public ObservableCollection<PracticeFileAttachment> Attachments { get; set; } = new();

        public CreatePracticePage(User user, DatabaseService dbService, SettingsService settingsService, int courseId)
        {
            InitializeComponent();
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseId = courseId;

            AnswerTypePicker.SelectedIndex = 0;
            AnswerTypePicker.SelectedIndexChanged += OnAnswerTypeChanged;

            // Инициализируем секцию прикрепленных файлов
            AttachmentsCollection.ItemsSource = Attachments;
            Attachments.CollectionChanged += (s, e) =>
            {
                AttachmentsCollection.IsVisible = Attachments.Any();
                ClearAllButton.IsVisible = Attachments.Any();
            };

            OnAnswerTypeChanged(null, EventArgs.Empty);
        }

        private void OnAnswerTypeChanged(object sender, EventArgs e)
        {
            var selectedType = AnswerTypePicker.SelectedItem as string;
            CodeSection.IsVisible = selectedType == "code";
            FileSettingsSection.IsVisible = selectedType == "file";
        }

        // ВЫБОР ФАЙЛОВ ДЛЯ ПРИКРЕПЛЕНИЯ
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

                var files = await PickFilesSequentially(options);
                if (files != null && files.Any())
                {
                    await ProcessSelectedFiles(files);
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

                    var attachment = new PracticeFileAttachment
                    {
                        FileName = file.FileName,
                        FileBytes = fileBytes,
                        FileSize = fileBytes.Length,
                        UploadDate = DateTime.Now,
                        FileType = Path.GetExtension(file.FileName)
                    };

                    Attachments.Add(attachment);

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
            UploadStatusLabel.Text = string.Empty;
        }

        private void OnRemoveAttachmentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is PracticeFileAttachment attachment)
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
                        foreach (var attachment in Attachments.ToList())
                        {
                            try
                            {
                                var addedAttachment = await _dbService.AddLessonAttachmentAsync(
                                    lessonId.Value,
                                    attachment.FileName,
                                    attachment.FileType,
                                    FormatFileSize(attachment.FileSize),
                                    attachment.FileBytes);

                                if (addedAttachment != null)
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