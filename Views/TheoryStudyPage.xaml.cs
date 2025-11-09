using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

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
                Attachments.Clear();

                var attachments = await GetLessonAttachmentsAsync(_lessonId);
                if (attachments != null && attachments.Any())
                {
                    foreach (var attachment in attachments)
                    {
                        Attachments.Add(new AttachmentViewModel
                        {
                            FileName = attachment.FileName,
                            FileSize = attachment.FileSize,
                            FilePath = attachment.FilePath,
                            FileIcon = _fileService.GetFileIcon(attachment.FileType)
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

        private async void OnOpenAttachmentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is AttachmentViewModel attachment)
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
                        "📥 Скачать",
                        "📁 Открыть");

                    if (action == "📥 Скачать")
                    {
                        // Скачиваем файл
                        var success = await _fileService.DownloadFileFromUrlAsync(attachment.FilePath, attachment.FileName);
                        if (success)
                        {
                            await DisplayAlert("Успех", $"Файл {attachment.FileName} скачан", "OK");
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", $"Не удалось скачать файл {attachment.FileName}", "OK");
                        }
                    }
                    else if (action == "📁 Открыть")
                    {
                        // Открываем файл
                        var success = await _fileService.DownloadAndOpenFileAsync(attachment.FilePath, attachment.FileName);
                        if (success)
                        {
                            await DisplayAlert("Успех", $"Файл {attachment.FileName} открыт", "OK");
                        }
                        else
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
}
