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

        // ИСПРАВЛЕНО: Используем модель из пространства имен Models
        public ObservableCollection<EnhancedFileAttachment> Attachments { get; set; } = new();

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

            // Обновляем видимость элементов при изменении коллекции
            Attachments.CollectionChanged += (s, e) =>
            {
                AttachmentsCollection.IsVisible = Attachments.Any();
                ClearAllButton.IsVisible = Attachments.Any();
                UploadProgressSection.IsVisible = false;
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
                    FileTypes = fileTypes,
                };

                // Используем множественный выбор файлов
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

                    // Проверяем размер файла
                    long fileSize = 0;
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

                    if (fileSize > 50 * 1024 * 1024) // 50 MB
                    {
                        await DisplayAlert("Предупреждение",
                            $"Файл {file.FileName} слишком большой (максимум 50 МБ)", "OK");
                        processed++;
                        continue;
                    }

                    // Создаем объект вложения
                    var attachment = new EnhancedFileAttachment
                    {
                        FileName = file.FileName,
                        FileSize = fileSize,
                        UploadDate = DateTime.Now,
                        FileIcon = _fileService.GetFileIcon(Path.GetExtension(file.FileName)),
                        IsUploading = true,
                        StatusIcon = "⏳"
                    };

                    Attachments.Add(attachment);

                    // Читаем файл в байты
                    byte[] fileBytes;
                    using (var stream = await file.OpenReadAsync())
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }

                    // Обновляем статус
                    attachment.IsUploading = false;
                    attachment.StatusIcon = "✅";
                    attachment.FileBytes = fileBytes; // Сохраняем байты для последующего сохранения

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
            UploadStatusLabel.Text = string.Empty;

            if (Attachments.Any(a => !a.IsUploading))
            {
                await DisplayAlert("Готово", $"Успешно обработано {Attachments.Count} файлов", "OK");
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
                    await DisplayAlert("Ошибка", "Введите название урока", "OK");
                    return;
                }

                var order = string.IsNullOrWhiteSpace(OrderEntry.Text) ? 1 : int.Parse(OrderEntry.Text);

                // Создаем урок
                var lessonId = await _dbService.AddSimpleTheoryAsync(
                    _courseId,
                    TitleEntry.Text.Trim(),
                    ContentEditor.Text?.Trim() ?? string.Empty,
                    order
                );

                if (lessonId.HasValue)
                {
                    // Сохраняем прикрепленные файлы в БД
                    if (Attachments.Any())
                    {
                        int savedCount = 0;
                        foreach (var attachment in Attachments.Where(a => a.FileBytes != null))
                        {
                            try
                            {
                                var fileType = Path.GetExtension(attachment.FileName);
                                var fileSizeFormatted = attachment.SizeFormatted;

                                var savedAttachment = await _dbService.AddLessonAttachmentAsync(
                                    lessonId.Value,
                                    attachment.FileName,
                                    fileType,
                                    fileSizeFormatted,
                                    attachment.FileBytes);

                                if (savedAttachment != null)
                                {
                                    savedCount++;
                                    Console.WriteLine($"✅ Файл сохранен: {attachment.FileName}");
                                }
                            }
                            catch (Exception fileEx)
                            {
                                Console.WriteLine($"❌ Ошибка сохранения файла {attachment.FileName}: {fileEx.Message}");
                            }
                        }

                        if (savedCount > 0)
                        {
                            await DisplayAlert("Успех",
                                $"Теоретический урок создан с {savedCount} прикрепленными файлами!", "OK");
                        }
                        else
                        {
                            await DisplayAlert("Успех",
                                "Теоретический урок создан, но файлы не были сохранены", "OK");
                        }
                    }
                    else
                    {
                        await DisplayAlert("Успех", "Теоретический урок создан!", "OK");
                    }

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

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}