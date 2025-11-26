using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class ContestResultsPage : ContentPage
    {
        private readonly int _contestId;
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        public bool CanGrade { get; private set; }

        public ContestResultsPage(int contestId, User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _contestId = contestId;
            _currentUser = currentUser;
            _dbService = dbService;

            // Разрешить оценивание только преподавателям и администраторам
            CanGrade = _currentUser.RoleId == 2 || string.Equals(_currentUser.RoleName, "Teacher", StringComparison.OrdinalIgnoreCase)
                       || _currentUser.RoleId == 3 || string.Equals(_currentUser.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSubmissions();
        }

        private async void LoadSubmissions()
        {
            try
            {
                var items = await _dbService.GetContestSubmissionsForContestAsync(_contestId);
                SubmissionsCollection.ItemsSource = items;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить работы: {ex.Message}", "OK");
            }
        }

        private async void OnDownloadClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string url)
            {
                try
                {
                    if (string.IsNullOrEmpty(url))
                    {
                        await DisplayAlert("Ошибка", "Файл не найден", "OK");
                        return;
                    }

                    var fileService = new FileService();
                    string fileName = Path.GetFileName(url);
                    
                    // Если это локальный путь
                    if (System.IO.File.Exists(url))
                    {
                        // Копируем файл в папку загрузок (рабочий стол на Windows)
                        string downloadPath;
                        if (DeviceInfo.Platform == DevicePlatform.WinUI)
                        {
                            // На Windows сохраняем в папку Downloads
                            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                            if (!Directory.Exists(downloadsPath))
                            {
                                downloadsPath = FileSystem.AppDataDirectory;
                            }
                            downloadPath = Path.Combine(downloadsPath, fileName);
                        }
                        else
                        {
                            // На других платформах используем AppDataDirectory
                            downloadPath = Path.Combine(FileSystem.AppDataDirectory, "Downloads", fileName);
                            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath)!);
                        }

                        System.IO.File.Copy(url, downloadPath, true);
                        await DisplayAlert("Успех", $"Файл сохранен: {downloadPath}", "OK");
                        
                        // Открываем файл
                        await Launcher.OpenAsync(new OpenFileRequest
                        {
                            File = new ReadOnlyFile(downloadPath)
                        });
                    }
                    else if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        // Если это URL, скачиваем файл
                        bool success = await fileService.DownloadFileFromUrlAsync(url, fileName);
                        if (success)
                        {
                            await DisplayAlert("Успех", $"Файл {fileName} скачан", "OK");
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Не удалось скачать файл", "OK");
                        }
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Неверный путь к файлу", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось скачать файл: {ex.Message}", "OK");
                }
            }
        }

        private async void OnGradeClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ContestSubmission sub)
            {
                if (!CanGrade)
                {
                    await DisplayAlert("Доступ запрещен", "Оценивать могут только преподаватели и администраторы", "OK");
                    return;
                }

                // Проверяем период конкурса перед оценкой
                var contest = await _dbService.GetContestByIdAsync(_contestId);
                if (contest != null)
                {
                    var now = DateTime.Now;
                    if (now < contest.EndDate)
                    {
                        await DisplayAlert("Оценка недоступна", 
                            $"Конкурс еще не завершен. Оценка работ будет доступна после {contest.EndDate:dd.MM.yyyy HH:mm}", 
                            "OK");
                        return;
                    }
                }

                string scoreStr = await DisplayPromptAsync("Оценка", "Введите балл (0-100)", initialValue: sub.TeacherScore?.ToString() ?? "0");
                if (int.TryParse(scoreStr, out int score) && score >= 0 && score <= 100)
                {
                    string? comment = await DisplayPromptAsync("Комментарий", "Замечания преподавателя", initialValue: sub.TeacherComment ?? "");
                    // Используем версию с teacherId для отслеживания, кто оценил
                    bool ok = await _dbService.GradeContestSubmissionAsync(sub.SubmissionId, _currentUser.UserId, score, comment ?? "");
                    if (ok)
                    {
                        await DisplayAlert("Успех", "Оценка сохранена", "OK");
                        LoadSubmissions();
                    }
                    else
                    {
                        // Более информативное сообщение об ошибке
                        if (contest != null && DateTime.Now < contest.EndDate)
                        {
                            await DisplayAlert("Ошибка", 
                                $"Не удалось сохранить оценку. Конкурс еще не завершен. Оценка доступна после {contest.EndDate:dd.MM.yyyy HH:mm}", 
                                "OK");
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Не удалось сохранить оценку. Проверьте подключение к базе данных.", "OK");
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(scoreStr))
                {
                    await DisplayAlert("Ошибка", "Оценка должна быть числом от 0 до 100", "OK");
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}