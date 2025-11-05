using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Maui.Platform;
using Microsoft.Maui;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class TheoryStudyPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _lessonId;
        private int _courseId;
        private List<CourseLesson> _allLessons;
        private int _currentLessonIndex;

        public ObservableCollection<string> Attachments { get; set; } = new();

        public TheoryStudyPage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _lessonId = lessonId;

            BindingContext = this;
            AttachmentsCollection.ItemsSource = Attachments;

            LoadTheoryContent();
        }

        private async void LoadTheoryContent()
        {
            try
            {
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

                        // Парсим прикрепленные файлы из содержания
                        ParseAttachments(lessonContent);

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
                await DisplayAlert("Ошибка", $"Не удалось загрузить теорию: {ex.Message}", "OK");
            }
        }

        private void ParseAttachments(string content)
        {
            if (string.IsNullOrEmpty(content)) return;

            // Ищем ссылки в содержании (простой парсинг)
            var lines = content.Split('\n');
            bool inAttachmentsSection = false;

            foreach (var line in lines)
            {
                if (line.Contains("Прикрепленные файлы:"))
                {
                    inAttachmentsSection = true;
                    continue;
                }

                if (inAttachmentsSection)
                {
                    if (line.Trim().StartsWith("•") && Uri.IsWellFormedUriString(line.Trim().Substring(1).Trim(), UriKind.Absolute))
                    {
                        Attachments.Add(line.Trim().Substring(1).Trim());
                    }
                    else if (string.IsNullOrWhiteSpace(line))
                    {
                        break; // Конец секции прикрепленных файлов
                    }
                }
            }

            AttachmentsSection.IsVisible = Attachments.Any();
        }

        private void UpdateNavigationButtons()
        {
            PrevButton.IsVisible = _currentLessonIndex > 0;
            NextButton.IsVisible = _currentLessonIndex < _allLessons.Count - 1;
        }

        private async void OnOpenAttachmentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string url)
            {
                try
                {
                    await Launcher.OpenAsync(new Uri(url));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть ссылку: {ex.Message}", "OK");
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
                    await Navigation.PushAsync(new PracticeStudyPage(_currentUser, _dbService, _settingsService, nextLesson.LessonId));
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