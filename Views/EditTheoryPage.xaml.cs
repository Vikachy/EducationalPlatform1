using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace EducationalPlatform.Views
{
    public partial class EditTheoryPage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private readonly int _lessonId;
        private CourseLesson _lesson;

        public ObservableCollection<TheoryFileAttachment> Attachments { get; set; } = new();

        public EditTheoryPage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
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

            LoadLessonData();
            LoadAttachments();
        }

        private async void LoadLessonData()
        {
            try
            {
                var courseId = await _dbService.GetCourseIdByLessonAsync(_lessonId);
                if (courseId.HasValue)
                {
                    var lessons = await _dbService.GetCourseLessonsAsync(courseId.Value);
                    _lesson = lessons.FirstOrDefault(l => l.LessonId == _lessonId);

                    if (_lesson != null)
                    {
                        TitleEntry.Text = _lesson.Title;
                        OrderEntry.Text = _lesson.LessonOrder.ToString();

                        var content = await _dbService.GetLessonContentAsync(_lessonId);
                        ContentEditor.Text = content ?? "";
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Урок не найден", "OK");
                        await Navigation.PopAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные урока: {ex.Message}", "OK");
            }
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
                        Attachments.Add(new TheoryFileAttachment
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
                        var attachment = new TheoryFileAttachment
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
            if (sender is Button btn && btn.CommandParameter is TheoryFileAttachment attachment)
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

        private async Task RemoveAttachment(TheoryFileAttachment attachment)
        {
            try
            {
                var success = await _dbService.DeleteLessonAttachmentAsync(attachment.AttachmentId);
                if (success)
                {
                    // Удаляем локальный файл если он существует
                    var resolvedPath = await _fileService.ResolveFilePath(attachment.FilePath, attachment.FileName, "LessonFiles");
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
                    await DisplayAlert("Ошибка", "Введите название урока", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(ContentEditor.Text))
                {
                    await DisplayAlert("Ошибка", "Введите содержание урока", "OK");
                    return;
                }

                int order = 1;
                if (!string.IsNullOrWhiteSpace(OrderEntry.Text) && int.TryParse(OrderEntry.Text, out int parsedOrder))
                {
                    order = parsedOrder;
                }

                bool success = await _dbService.UpdateTheoryLessonAsync(
                    _lessonId,
                    TitleEntry.Text.Trim(),
                    ContentEditor.Text.Trim(),
                    order
                );

                if (success)
                {
                    await DisplayAlert("Успех", "Теоретический урок успешно обновлен", "OK");
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