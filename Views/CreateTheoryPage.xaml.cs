using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class CreateTheoryPage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _courseId;

        public ObservableCollection<FileAttachment> Attachments { get; set; } = new();

        public CreateTheoryPage(User user, DatabaseService dbService, SettingsService settingsService, int courseId)
        {
            InitializeComponent();
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseId = courseId;

            BindingContext = this;
            AttachmentsCollection.ItemsSource = Attachments;

            // Обновляем видимость кнопки очистки при изменении коллекции
            Attachments.CollectionChanged += (s, e) =>
            {
                ClearAllButton.IsVisible = Attachments.Any();
            };
        }

        private async void OnSelectFilesClicked(object sender, EventArgs e)
        {
            try
            {
                var fileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".zip", ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".png", ".jpg", ".jpeg" } },
                        { DevicePlatform.macOS, new[] { ".zip", ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".png", ".jpg", ".jpeg" } },
                        { DevicePlatform.Android, new[] { "application/zip", "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "image/png", "image/jpeg" } },
                        { DevicePlatform.iOS, new[] { "public.zip-archive", "com.adobe.pdf", "com.microsoft.word.doc", "org.openxmlformats.wordprocessingml.document", "com.microsoft.powerpoint.ppt", "org.openxmlformats.presentationml.presentation", "public.png", "public.jpeg" } }
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Выберите файлы для прикрепления",
                    FileTypes = fileTypes,
                };

                // Используем последовательный выбор файлов
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

        // Метод для последовательного выбора файлов
        private async Task<List<FileResult>> PickFilesSequentially(PickOptions options)
        {
            var files = new List<FileResult>();
            bool continueSelecting = true;

            while (continueSelecting && files.Count < 10) // Ограничим максимум 10 файлов
            {
                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    files.Add(result);

                    // Спрашиваем, хочет ли пользователь добавить еще файлы
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

                    // Проверяем размер файла (максимум 50 МБ)
                    var fileInfo = new FileInfo(file.FullPath);
                    if (fileInfo.Length > 50 * 1024 * 1024) // 50 MB
                    {
                        await DisplayAlert("Предупреждение",
                            $"Файл {file.FileName} слишком большой (максимум 50 МБ)", "OK");
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

                    // Сохраняем файл локально
                    var savedPath = await SaveFileLocally(file.FileName, fileBytes);
                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        var attachment = new FileAttachment
                        {
                            FileName = file.FileName,
                            FilePath = savedPath,
                            FileSize = fileBytes.Length,
                            UploadDate = DateTime.Now
                        };

                        Attachments.Add(attachment);
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

        private async Task<string> SaveFileLocally(string fileName, byte[] fileBytes)
        {
            try
            {
                // Создаем папку для файлов теории
                var theoryFolder = Path.Combine(FileSystem.AppDataDirectory, "TheoryFiles", _courseId.ToString());
                if (!Directory.Exists(theoryFolder))
                {
                    Directory.CreateDirectory(theoryFolder);
                }

                // Генерируем уникальное имя файла
                var fileExtension = Path.GetExtension(fileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var fullPath = Path.Combine(theoryFolder, uniqueFileName);

                // Сохраняем файл
                await File.WriteAllBytesAsync(fullPath, fileBytes);

                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения файла: {ex.Message}");
                return null;
            }
        }

        private void OnRemoveAttachmentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is FileAttachment attachment)
            {
                try
                {
                    // Удаляем файл с диска
                    if (File.Exists(attachment.FilePath))
                    {
                        File.Delete(attachment.FilePath);
                    }
                    Attachments.Remove(attachment);
                }
                catch (Exception ex)
                {
                    DisplayAlert("Ошибка", $"Не удалось удалить файл: {ex.Message}", "OK");
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
                    try
                    {
                        if (File.Exists(attachment.FilePath))
                        {
                            File.Delete(attachment.FilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка удаления файла {attachment.FileName}: {ex.Message}");
                    }
                }
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

                // Формируем контент с информацией о файлах
                var contentBuilder = new System.Text.StringBuilder();
                contentBuilder.AppendLine(ContentEditor.Text);

                if (Attachments.Any())
                {
                    contentBuilder.AppendLine("\n📎 Прикрепленные файлы:");
                    foreach (var attachment in Attachments)
                    {
                        contentBuilder.AppendLine($"• {attachment.FileName} ({attachment.SizeFormatted})");
                    }
                }

                var lessonId = await _dbService.AddSimpleTheoryAsync(
                    _courseId,
                    TitleEntry.Text.Trim(),
                    contentBuilder.ToString(),
                    order
                );

                if (lessonId.HasValue)
                {
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

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    // Модель для прикрепленных файлов
    public class FileAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }

        public string SizeFormatted
        {
            get
            {
                if (FileSize < 1024) return $"{FileSize} Б";
                if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:0.0} КБ";
                return $"{FileSize / (1024.0 * 1024.0):0.0} МБ";
            }
        }
    }
}