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
        private List<CourseLesson> _allLessons;
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
                // Показываем индикатор загрузки
                ContentLabel.Text = "Загрузка...";

                // Получаем курс урока
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

                        // Получаем полное содержание урока
                        var lessonContent = await _dbService.GetLessonContentAsync(_lessonId);
                        ContentLabel.Text = lessonContent ?? "Содержание урока пока не добавлено.";

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
                    ContentLabel.Text = "Курс не найден.";
                }
            }
            catch (Exception ex)
            {
                ContentLabel.Text = $"Ошибка загрузки: {ex.Message}";
                await DisplayAlert("Ошибка", $"Не удалось загрузить теорию: {ex.Message}", "OK");
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

        // Временная реализация до добавления метода в DatabaseService
        private async Task<List<LessonAttachment>> GetLessonAttachmentsAsync(int lessonId)
        {
            try
            {
                // TODO: Замените на реальный вызов к БД когда будет готов метод
                // return await _dbService.GetLessonAttachmentsAsync(lessonId);

                // Временная заглушка с тестовыми данными
                return new List<LessonAttachment>
                {
                    new LessonAttachment
                    {
                        FileName = "lecture_notes.pdf",
                        FileSize = "3.2 MB",
                        FilePath = "https://example.com/files/lecture_notes.pdf", // Используйте реальные URL
                        FileType = ".pdf"
                    },
                    new LessonAttachment
                    {
                        FileName = "presentation.pptx",
                        FileSize = "5.1 MB",
                        FilePath = "https://example.com/files/presentation.pptx",
                        FileType = ".pptx"
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки вложений урока: {ex.Message}");
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

                    // Показываем индикатор загрузки
                    await DisplayAlert("Информация", $"Начинаем загрузку: {attachment.FileName}", "OK");

                    // Используем FileService для скачивания файла
                    var success = await _fileService.DownloadAndOpenFileAsync(attachment.FilePath, attachment.FileName);

                    if (success)
                    {
                        await DisplayAlert("Успех", $"Файл {attachment.FileName} успешно скачан и открыт", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", $"Не удалось скачать файл {attachment.FileName}", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть файл: {ex.Message}", "OK");
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
                await DisplayAlert("Поздравляем!", "Вы завершили изучение этого раздела!", "OK");
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
            // Обновляем прогресс при выходе
            if (_courseId > 0)
            {
                await _dbService.UpdateProgressAsync(_currentUser.UserId, _courseId, "in_progress");
            }
        }
    }
}