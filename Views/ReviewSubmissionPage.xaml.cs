using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace EducationalPlatform.Views
{
    public partial class ReviewSubmissionPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly PracticeSubmission _submission;

        public ReviewSubmissionPage(
            User currentUser,
            DatabaseService dbService,
            SettingsService settingsService,
            PracticeSubmission submission)
        {
            InitializeComponent();

            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _submission = submission;

            LoadSubmissionData();

            ScoreSlider.ValueChanged += (s, e) =>
            {
                ScoreEntry.Text = e.NewValue.ToString("0");
            };

            ScoreEntry.TextChanged += (s, e) =>
            {
                if (int.TryParse(e.NewTextValue, out int value))
                {
                    if (value >= 0 && value <= 100)
                    {
                        ScoreSlider.Value = value;
                    }
                }
            };
        }

        private void LoadSubmissionData()
        {
            StudentNameLabel.Text = _submission.StudentName ?? "Неизвестно";
            CourseNameLabel.Text = _submission.CourseName ?? "Неизвестно";
            LessonTitleLabel.Text = _submission.LessonTitle ?? "Неизвестно";
            SubmissionDateLabel.Text = _submission.SubmissionDate.ToString("dd.MM.yyyy HH:mm");

            // Определяем тип ответа
            if (!string.IsNullOrEmpty(_submission.SubmissionText))
            {
                TextAnswerFrame.IsVisible = true;
                TextAnswerLabel.Text = _submission.SubmissionText;
            }
            else if (!string.IsNullOrEmpty(_submission.SubmissionFileUrl))
            {
                FileAnswerFrame.IsVisible = true;
                FileNameLabel.Text = Path.GetFileName(_submission.SubmissionFileUrl);

                // Получаем размер файла
                try
                {
                    if (File.Exists(_submission.SubmissionFileUrl))
                    {
                        var fileInfo = new FileInfo(_submission.SubmissionFileUrl);
                        FileSizeLabel.Text = FormatFileSize(fileInfo.Length);
                    }
                    else
                    {
                        FileSizeLabel.Text = "Файл готов к скачиванию";
                    }
                }
                catch
                {
                    FileSizeLabel.Text = "Файл готов к скачиванию";
                }
            }

            // Если работа уже оценена, показываем оценку
            if (_submission.TeacherScore.HasValue)
            {
                ScoreEntry.Text = _submission.TeacherScore.ToString();
                ScoreSlider.Value = _submission.TeacherScore.Value;
                CommentEditor.Text = _submission.TeacherComment;
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} Б";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.0} КБ";
            return $"{bytes / (1024.0 * 1024.0):0.0} МБ";
        }

        private async void OnDownloadFileClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_submission.SubmissionFileUrl))
                {
                    await DisplayAlert("Ошибка", "Файл не найден", "OK");
                    return;
                }

                if (!File.Exists(_submission.SubmissionFileUrl))
                {
                    await DisplayAlert("Ошибка", "Файл не найден на устройстве", "OK");
                    return;
                }

                var fileName = Path.GetFileName(_submission.SubmissionFileUrl);

                // Копируем в папку Downloads
                var downloadsFolder = Path.Combine(FileSystem.AppDataDirectory, "Downloads");
                if (!Directory.Exists(downloadsFolder))
                    Directory.CreateDirectory(downloadsFolder);

                var destPath = Path.Combine(downloadsFolder, fileName);

                // Если файл уже существует, добавляем номер
                int counter = 1;
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);

                while (File.Exists(destPath))
                {
                    destPath = Path.Combine(downloadsFolder, $"{fileNameWithoutExt}_{counter++}{extension}");
                }

                File.Copy(_submission.SubmissionFileUrl, destPath);

                await DisplayAlert("Успех", $"Файл сохранен: {Path.GetFileName(destPath)}", "OK");

                // Предлагаем открыть файл
                bool open = await DisplayAlert("Открыть", "Открыть файл?", "Да", "Нет");
                if (open)
                {
                    await Launcher.Default.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(destPath)
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось скачать файл: {ex.Message}", "OK");
            }
        }

        private async void OnSaveGradeClicked(object sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(ScoreEntry.Text, out int score) || score < 0 || score > 100)
                {
                    await DisplayAlert("Ошибка", "Введите корректную оценку от 0 до 100", "OK");
                    return;
                }

                // Получаем проходной балл для этого урока
                int passingScore = 60; // По умолчанию
                var testMeta = await _dbService.GetTestMetaByLessonAsync(_submission.LessonId);
                if (testMeta != null)
                {
                    passingScore = testMeta.PassingScore;
                }

                var success = await _dbService.GradePracticeSubmissionAsync(
                    _submission.SubmissionId,
                    _currentUser.UserId,
                    score,
                    CommentEditor.Text?.Trim()
                );

                if (success)
                {
                    // НАЧИСЛЯЕМ ВАЛЮТУ если студент набрал проходной балл
                    if (score >= passingScore)
                    {
                        bool awarded = await _dbService.AwardCurrencyForCompletionAsync(
                            _submission.StudentId,
                            _submission.LessonId,
                            "practice",
                            score,
                            passingScore);

                        if (awarded)
                        {
                            Console.WriteLine($"💰 Студенту {_submission.StudentId} начислена награда за практику: {score} баллов (проходной: {passingScore})");
                        }
                    }

                    await DisplayAlert("Успех", "Оценка сохранена", "OK");

                    // Отправляем уведомление об обновлении
                    MessagingCenter.Send(this, "SubmissionGraded", _submission.SubmissionId);

                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось сохранить оценку", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}