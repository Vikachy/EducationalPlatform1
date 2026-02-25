using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using System.IO;
using System.Threading.Tasks;

#if ANDROID
using Android.Content;
using Android.OS;
#endif

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

            _currentUser = user ?? throw new ArgumentNullException(nameof(user));
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _fileService = new FileService();
            _lessonId = lessonId;

            BindingContext = this;
            AttachmentsCollection.ItemsSource = Attachments;

            // Устанавливаем заголовок страницы сразу
            Title = "Изучение теории";

            LoadTheoryContent();
        }

        private async void LoadTheoryContent()
        {
            try
            {
                LoadingIndicator.IsRunning = true;
                ContentLabel.Text = "Загрузка теории...";

                var courseIdResult = await _dbService.GetCourseIdByLessonAsync(_lessonId);
                if (!courseIdResult.HasValue)
                {
                    ContentLabel.Text = "Курс не найден.";
                    return;
                }

                _courseId = courseIdResult.Value;
                _allLessons = await _dbService.GetCourseLessonsAsync(_courseId);

                var currentLesson = _allLessons.FirstOrDefault(l => l.LessonId == _lessonId);
                if (currentLesson == null)
                {
                    ContentLabel.Text = "Урок не найден.";
                    return;
                }

                // Обновляем заголовок страницы и лейбл внутри страницы
                Title = currentLesson.Title;
                TitleLabel.Text = currentLesson.Title;

                var lessonContent = await _dbService.GetLessonContentAsync(_lessonId);
                ContentLabel.Text = lessonContent ?? "Содержимое урока отсутствует.";

                await LoadAttachments();

                _currentLessonIndex = _allLessons.FindIndex(l => l.LessonId == _lessonId);
                UpdateNavigationButtons();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки теории: {ex.Message}\n{ex.StackTrace}");
                await DisplayAlert("Ошибка", $"Не удалось загрузить урок: {ex.Message}", "OK");
                ContentLabel.Text = "Ошибка загрузки контента";
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
            }
        }

        private async Task LoadAttachments()
        {
            try
            {
                var attachments = await _dbService.GetLessonAttachmentsAsync(_lessonId);
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
                            FileType = att.FileType ?? string.Empty
                        });
                    }
                    AttachmentsSection.IsVisible = true;
                }
                else
                {
                    AttachmentsSection.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки вложений: {ex.Message}");
                AttachmentsSection.IsVisible = false;
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
                await DownloadToDownloadsFolder(vm);
            }
        }

        private async Task HandleAttachmentAction(AttachmentViewModel vm)
        {
            var choice = await DisplayActionSheet(
                vm.FileName,
                "Отмена",
                null,
                "📥 Скачать в Загрузки",
                "📄 Открыть файл"
            );

            if (choice == "📥 Скачать в Загрузки")
            {
                await DownloadToDownloadsFolder(vm);
            }
            else if (choice == "📄 Открыть файл")
            {
                await TryOpenFile(vm);
            }
        }

        private async Task DownloadToDownloadsFolder(AttachmentViewModel vm)
        {
            try
            {
                string sourcePath = await _fileService.ResolveFilePath(vm.FilePath, vm.FileName, "TheoryFiles");
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                {
                    await DisplayAlert("Ошибка", "Исходный файл не найден на устройстве", "OK");
                    return;
                }

                string destinationPath = await GetDownloadsPath(vm.FileName);
                if (string.IsNullOrEmpty(destinationPath))
                {
                    await DisplayAlert("Ошибка", "Не удалось определить папку «Загрузки»", "OK");
                    return;
                }

                // Копируем файл
                File.Copy(sourcePath, destinationPath, overwrite: true);

                // На Android уведомляем систему
#if ANDROID
                var mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
                mediaScanIntent.SetData(Android.Net.Uri.FromFile(new Java.IO.File(destinationPath)));
                Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.SendBroadcast(mediaScanIntent);
#endif

                await DisplayAlert("Успех",
                    $"Файл сохранён в папку «Загрузки»:\n{Path.GetFileName(destinationPath)}",
                    "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении файла: {ex.Message}\n{ex.StackTrace}");
                await DisplayAlert("Ошибка сохранения", ex.Message, "OK");
            }
        }

        private async Task<string?> GetDownloadsPath(string fileName)
        {
            string downloadsFolder;

#if ANDROID
            downloadsFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath
                           ?? Path.Combine(Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath ?? "", "Download");
#elif WINDOWS
            downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
#elif IOS || MACCATALYST
            downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Downloads");
#else
            downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif

            if (string.IsNullOrEmpty(downloadsFolder) || !Directory.Exists(downloadsFolder))
            {
                try
                {
                    Directory.CreateDirectory(downloadsFolder);
                }
                catch
                {
                    return null;
                }
            }

            // Уникальное имя файла при конфликте
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            string fullPath = Path.Combine(downloadsFolder, fileName);
            int counter = 1;

            while (File.Exists(fullPath))
            {
                fullPath = Path.Combine(downloadsFolder, $"{baseName} ({counter++}){extension}");
            }

            return fullPath;
        }

        private async Task TryOpenFile(AttachmentViewModel vm)
        {
            try
            {
                var path = await _fileService.ResolveFilePath(vm.FilePath, vm.FileName, "TheoryFiles");
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
                await DisplayAlert("Не удалось открыть файл", ex.Message, "OK");
            }
        }

        private void UpdateNavigationButtons()
        {
            PrevButton.IsVisible = _currentLessonIndex > 0;
            NextButton.IsVisible = _currentLessonIndex < _allLessons.Count - 1;
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
                // Завершение курса
                await _dbService.UpdateProgressAsync(_currentUser.UserId, _courseId, "completed");
                await DisplayAlert("Поздравляем!", "Вы завершили изучение курса!", "OK");
                await Navigation.PopAsync();
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            try
            {
                // Безопасный возврат назад
                if (Navigation.NavigationStack.Count > 1)
                {
                    await Navigation.PopAsync();
                }
                else
                {
                    // Если стек пустой — возвращаемся на главную через Shell или корневую страницу
                    await Shell.Current.GoToAsync("//Dashboard");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при возврате назад: {ex.Message}");
                await DisplayAlert("Ошибка", "Не удалось вернуться назад", "OK");
            }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();

            if (_courseId > 0)
            {
                await _dbService.UpdateProgressAsync(_currentUser.UserId, _courseId, "in_progress");
            }
        }
    }

    public class AttachmentViewModel
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileIcon { get; set; } = "📄";
        public string FileType { get; set; } = string.Empty;
    }
}