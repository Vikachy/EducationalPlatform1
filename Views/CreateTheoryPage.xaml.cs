using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace EducationalPlatform.Views
{
    public partial class CreateTheoryPage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private readonly int _courseId;

        public ObservableCollection<TheoryFileAttachment> Attachments { get; set; } = new();

        public CreateTheoryPage(User user, DatabaseService dbService, SettingsService settingsService, int courseId)
        {
            InitializeComponent();
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _fileService = ServiceHelper.GetService<FileService>();
            _courseId = courseId;

            BindingContext = this;
            AttachmentsCollection.ItemsSource = Attachments;

            Attachments.CollectionChanged += (s, e) =>
            {
                ClearAllButton.IsVisible = Attachments.Any();
                AttachmentsCollection.IsVisible = Attachments.Any();
            };
        }

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

                    var attachment = new TheoryFileAttachment
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
            UploadStatusLabel.Text = "";
        }

        private void OnRemoveAttachmentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is TheoryFileAttachment attachment)
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
                    await DisplayAlert("Ошибка", "Введите название урока", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(ContentEditor.Text))
                {
                    await DisplayAlert("Ошибка", "Введите содержание урока", "OK");
                    return;
                }

                var order = string.IsNullOrWhiteSpace(OrderEntry.Text) ? 1 : int.Parse(OrderEntry.Text);

                // Создаем урок
                var lessonId = await _dbService.AddSimpleTheoryAsync(
                    _courseId,
                    TitleEntry.Text.Trim(),
                    ContentEditor.Text.Trim(),
                    order
                );

                if (lessonId.HasValue)
                {
                    // Сохраняем прикрепленные файлы
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
                            catch (Exception fileEx)
                            {
                                Console.WriteLine($"❌ Ошибка прикрепления файла {attachment.FileName}: {fileEx.Message}");
                            }
                        }
                    }

                    await DisplayAlert("Успех", "Теоретический урок создан!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось создать урок", "OK");
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