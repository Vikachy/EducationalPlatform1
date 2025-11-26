using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace EducationalPlatform.Views
{
    public partial class TheoryStudyPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private readonly int _lessonId;
        private int _courseId;
        private List<CourseLesson> _allLessons = new();
        private int _currentLessonIndex;

        public ObservableCollection<AttachmentViewModel> Attachments { get; set; } = new();

        public TheoryStudyPage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _fileService = new FileService();
            _lessonId = lessonId;

            BindingContext = this;
            
            // Устанавливаем ItemsSource для CollectionView
            AttachmentsCollection.ItemsSource = Attachments;
            
            LoadTheoryContent();
        }

        private async void LoadTheoryContent()
        {
            try
            {
                // Загружаем содержимое урока
                ContentLabel.Text = "Загрузка...";

                // Получаем ID курса
                var courseId = await _dbService.GetCourseIdByLessonAsync(_lessonId);
                if (courseId.HasValue)
                {
                    _courseId = courseId.Value;

                    // Загружаем все уроки курса
                    _allLessons = await _dbService.GetCourseLessonsAsync(_courseId);
                    var currentLesson = _allLessons.FirstOrDefault(l => l.LessonId == _lessonId);

                    if (currentLesson != null)
                    {
                        TitleLabel.Text = currentLesson.Title;

                        // Загружаем текстовое содержимое урока
                        var lessonContent = await _dbService.GetLessonContentAsync(_lessonId);
                        ContentLabel.Text = lessonContent ?? "Содержимое урока не найдено.";

                        // Загружаем прикрепленные файлы
                        await LoadAttachments();

                        _currentLessonIndex = _allLessons.FindIndex(l => l.LessonId == _lessonId);
                        UpdateNavigationButtons();
                    }
                    else
                    {
                        ContentLabel.Text = "Урок не найден.";
                    }
                }
                else
                {
                    ContentLabel.Text = "Урок не найден.";
                }
            }
            catch (Exception ex)
            {
                ContentLabel.Text = $"Ошибка загрузки: {ex.Message}";
                await DisplayAlert("Ошибка", $"Не удалось загрузить урок: {ex.Message}", "OK");
            }
        }

        private async Task LoadAttachments()
        {
            try
            {
                Console.WriteLine($"🔄 Загружаем вложения для урока {_lessonId}");
                
                var attachments = await GetLessonAttachmentsAsync(_lessonId);
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Attachments.Clear();

                    if (attachments != null && attachments.Any())
                    {
                        Console.WriteLine($"📎 Найдено {attachments.Count} вложений");
                        
                        foreach (var attachment in attachments)
                        {
                            Attachments.Add(new AttachmentViewModel
                            {
                                AttachmentId = attachment.AttachmentId,
                                FileName = attachment.FileName,
                                FileSize = attachment.FileSize,
                                FilePath = attachment.FilePath,
                                FileIcon = _fileService.GetFileIcon(attachment.FileType)
                            });
                        }

                        AttachmentsSection.IsVisible = true;
                        AttachmentsCollection.ItemsSource = null; // Сбрасываем для обновления
                        AttachmentsCollection.ItemsSource = Attachments;
                        
                        Console.WriteLine($"✅ Вложения загружены и отображены");
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️ Вложения не найдены");
                        AttachmentsSection.IsVisible = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки вложений: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AttachmentsSection.IsVisible = false;
                });
            }
        }

        // Получаем вложения из базы данных
        private async Task<List<LessonAttachment>> GetLessonAttachmentsAsync(int lessonId)
        {
            try
            {
                return await _dbService.GetLessonAttachmentsAsync(lessonId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения вложений урока: {ex.Message}");
                return new List<LessonAttachment>();
            }
        }

        private void UpdateNavigationButtons()
        {
            PrevButton.IsVisible = _currentLessonIndex > 0;
            NextButton.IsVisible = _currentLessonIndex < _allLessons.Count - 1;
        }

        private async void OnAttachmentTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is AttachmentViewModel attachment)
            {
                await HandleAttachmentAction(attachment);
            }
        }

        private async void OnOpenAttachmentClicked(object sender, EventArgs e)
        {
            // Кнопка 📥 теперь сразу скачивает файл в «Загрузки»,
            // без дополнительных вопросов, чтобы поведение было предсказуемым.
            if (sender is Button btn && btn.CommandParameter is AttachmentViewModel attachment)
            {
                var success = await DownloadAttachmentToDownloads(attachment.FilePath, attachment.FileName);
                if (success)
                {
                    await DisplayAlert("Успех", $"Файл {attachment.FileName} скачан в папку Загрузки", "OK");
                }
                else
                {
                    await DisplayAlert("Ошибка", $"Не удалось скачать файл {attachment.FileName}", "OK");
                }
            }
        }

        private async Task HandleAttachmentAction(AttachmentViewModel attachment)
        {
            try
            {
                if (string.IsNullOrEmpty(attachment.FilePath))
                {
                    await DisplayAlert("Ошибка", "Файл не найден", "OK");
                    return;
                }

                // Показываем опции: скачать или открыть
                var action = await DisplayActionSheet(
                    $"Файл: {attachment.FileName}",
                    "Отмена",
                    null,
                    "📥 Скачать в папку Загрузки",
                    "📁 Открыть файл");

                if (action == "📥 Скачать в папку Загрузки")
                {
                    var success = await DownloadAttachmentToDownloads(attachment.FilePath, attachment.FileName);
                    if (success)
                    {
                        await DisplayAlert("Успех", $"Файл {attachment.FileName} скачан в папку Загрузки", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", $"Не удалось скачать файл {attachment.FileName}", "OK");
                    }
                }
                else if (action == "📁 Открыть файл")
                {
                    // Открываем файл
                    var success = await OpenAttachmentFile(attachment.FilePath, attachment.FileName);
                    if (!success)
                    {
                        await DisplayAlert("Ошибка", $"Не удалось открыть файл {attachment.FileName}", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось обработать файл: {ex.Message}", "OK");
            }
        }

        private async Task<bool> DownloadAttachmentToDownloads(string filePath, string fileName)
        {
            try
            {
                Console.WriteLine($"📥 Начинаем скачивание файла: {fileName} из {filePath}");

                var resolvedPath = await _fileService.ResolveFilePath(filePath, fileName, "TheoryFiles");

                if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
                {
                    Console.WriteLine($"❌ Файл не найден: {resolvedPath}");
                    await DisplayAlert("Ошибка", $"Файл не найден: {fileName}", "OK");
                    return false;
                }

                var success = await _fileService.DownloadFileAsync(resolvedPath, fileName);
                
                if (success)
                {
                    Console.WriteLine($"✅ Файл успешно скачан: {fileName}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось скачать файл: {fileName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка скачивания файла: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Ошибка", $"Не удалось скачать файл: {ex.Message}", "OK");
                return false;
            }
        }

        private async Task<bool> OpenAttachmentFile(string filePath, string fileName)
        {
            try
            {
                var resolvedPath = await _fileService.ResolveFilePath(filePath, fileName, "TheoryFiles");

                if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
                {
                    return false;
                }

                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(resolvedPath)
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка открытия файла: {ex.Message}");
                return false;
            }
        }

        private async void OnPrevClicked(object sender, EventArgs e)
        {
            if (_currentLessonIndex > 0)
            {
                var prevLesson = _allLessons[_currentLessonIndex - 1];
                await Navigation.PushAsync(new TheoryStudyPage(_currentUser, _dbService, _settingsService, prevLesson.LessonId));
                Navigation.RemovePage(this);
            }
        }

        private async void OnNextClicked(object sender, EventArgs e)
        {
            if (_currentLessonIndex < _allLessons.Count - 1)
            {
                var nextLesson = _allLessons[_currentLessonIndex + 1];

                // В зависимости от типа следующего урока переходим на соответствующую страницу
                if (nextLesson.LessonType == "theory")
                {
                    await Navigation.PushAsync(new TheoryStudyPage(_currentUser, _dbService, _settingsService, nextLesson.LessonId));
                }
                else if (nextLesson.LessonType == "practice")
                {
                    await Navigation.PushAsync(new PracticePage(_currentUser, _dbService, _settingsService, _courseId, nextLesson.LessonId, nextLesson.Title));
                }
                else if (nextLesson.LessonType == "test")
                {
                     await Navigation.PushAsync(new TestStudyPage(_currentUser, _dbService, _settingsService, nextLesson.LessonId));
                }

                Navigation.RemovePage(this);
            }
            else
            {
                // Курс завершен
                await _dbService.UpdateProgressAsync(_currentUser.UserId, _courseId, "completed");
                await DisplayAlert("Поздравляем!", "Вы завершили изучение курса!", "OK");
                await Navigation.PopAsync();
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            // Обновляем прогресс для курса
            if (_courseId > 0)
            {
                await _dbService.UpdateProgressAsync(_currentUser.UserId, _courseId, "in_progress");
            }
        }
    }

    // ViewModel для отображения вложений
    public class AttachmentViewModel
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileIcon { get; set; } = "📄";
    }
}
