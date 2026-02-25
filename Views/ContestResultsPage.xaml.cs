using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class ContestResultsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly int _contestId;
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;

        private bool _canGrade;
        private string _title = string.Empty;
        private string _contestInfo = string.Empty;

        public bool CanGrade
        {
            get => _canGrade;
            set
            {
                if (_canGrade != value)
                {
                    _canGrade = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ContestInfo
        {
            get => _contestInfo;
            set
            {
                if (_contestInfo != value)
                {
                    _contestInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public ContestResultsPage(int contestId, User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации ContestResultsPage: {ex.Message}");
            }

            _contestId = contestId;
            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            // Определяем права на оценивание:
            // RoleId: 1 - Student, 2 - Teacher, 3 - Admin, 4 - ContentManager
            CanGrade = _currentUser.RoleId == 2 || _currentUser.RoleId == 3 || _currentUser.RoleId == 4;

            Console.WriteLine($"🔍 Права на оценивание: {CanGrade} (RoleId: {_currentUser.RoleId})");

            BindingContext = this;
            UpdateTexts();

            // Загружаем информацию о конкурсе
            Task.Run(async () => await LoadContestInfoAsync());
        }

        private void UpdateTexts()
        {
            try
            {
                Title = _localizationService.GetText("ContestResults") ?? "Результаты конкурса";

                var headerLabel = this.FindByName<Label>("HeaderLabel");
                if (headerLabel != null)
                    headerLabel.Text = _localizationService.GetText("ContestResults") ?? "Результаты конкурса";

                var emptyLabel = this.FindByName<Label>("EmptyLabel");
                if (emptyLabel != null)
                    emptyLabel.Text = _localizationService.GetText("NoSubmissions") ?? "Нет отправленных работ";

                var loadingLabel = this.FindByName<Label>("LoadingLabel");
                if (loadingLabel != null)
                    loadingLabel.Text = _localizationService.GetText("Loading") ?? "Загрузка...";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления текстов: {ex.Message}");
            }
        }

        private async Task LoadContestInfoAsync()
        {
            try
            {
                var contest = await _dbService.GetContestByIdAsync(_contestId);
                if (contest != null)
                {
                    ContestInfo = $"{contest.ContestName} • {contest.PrizeCurrency} 🪙";

                    var infoLabel = this.FindByName<Label>("ContestInfoLabel");
                    if (infoLabel != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            infoLabel.Text = ContestInfo;
                            infoLabel.IsVisible = true;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки информации о конкурсе: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadSubmissionsAsync();
        }

        private async Task LoadSubmissionsAsync()
        {
            try
            {
                // Показываем индикатор загрузки
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (LoadingOverlay != null)
                        LoadingOverlay.IsVisible = true;
                });

                var items = await _dbService.GetContestSubmissionsForContestAsync(_contestId);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (SubmissionsCollection != null)
                    {
                        SubmissionsCollection.ItemsSource = items;
                        Console.WriteLine($"✅ Загружено {items.Count} работ");
                    }

                    if (LoadingOverlay != null)
                        LoadingOverlay.IsVisible = false;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки работ: {ex.Message}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (LoadingOverlay != null)
                        LoadingOverlay.IsVisible = false;

                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        $"{_localizationService.GetText("FailedToLoadSubmissions") ?? "Не удалось загрузить работы"}: {ex.Message}",
                        _localizationService.GetText("OK") ?? "OK");
                });
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
                        await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                            _localizationService.GetText("FileNotFound") ?? "Файл не найден",
                            _localizationService.GetText("OK") ?? "OK");
                        return;
                    }

                    // Показываем индикатор загрузки
                    if (LoadingOverlay != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            LoadingOverlay.IsVisible = true;
                            if (LoadingLabel != null)
                                LoadingLabel.Text = _localizationService.GetText("Downloading") ?? "Скачивание...";
                        });
                    }

                    var fileService = new FileService();
                    string fileName = Path.GetFileName(url);

                    if (File.Exists(url))
                    {
                        string downloadPath;
                        if (DeviceInfo.Platform == DevicePlatform.WinUI)
                        {
                            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                            if (!Directory.Exists(downloadsPath))
                            {
                                downloadsPath = FileSystem.AppDataDirectory;
                            }
                            downloadPath = Path.Combine(downloadsPath, fileName);
                        }
                        else
                        {
                            downloadPath = Path.Combine(FileSystem.AppDataDirectory, "Downloads", fileName);
                            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath)!);
                        }

                        File.Copy(url, downloadPath, true);

                        await DisplayAlert(_localizationService.GetText("Success") ?? "Успех",
                            $"{_localizationService.GetText("FileSaved") ?? "Файл сохранен"}: {downloadPath}",
                            _localizationService.GetText("OK") ?? "OK");

                        await Launcher.OpenAsync(new OpenFileRequest
                        {
                            File = new ReadOnlyFile(downloadPath)
                        });
                    }
                    else if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        bool success = await fileService.DownloadFileFromUrlAsync(url, fileName);
                        if (success)
                        {
                            await DisplayAlert(_localizationService.GetText("Success") ?? "Успех",
                                $"{_localizationService.GetText("FileDownloaded") ?? "Файл скачан"} {fileName}",
                                _localizationService.GetText("OK") ?? "OK");
                        }
                        else
                        {
                            await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                                _localizationService.GetText("FailedToDownloadFile") ?? "Не удалось скачать файл",
                                _localizationService.GetText("OK") ?? "OK");
                        }
                    }
                    else
                    {
                        await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                            _localizationService.GetText("InvalidFilePath") ?? "Неверный путь к файлу",
                            _localizationService.GetText("OK") ?? "OK");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка скачивания: {ex.Message}");
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        $"{_localizationService.GetText("FailedToDownloadFile") ?? "Не удалось скачать файл"}: {ex.Message}",
                        _localizationService.GetText("OK") ?? "OK");
                }
                finally
                {
                    if (LoadingOverlay != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            LoadingOverlay.IsVisible = false;
                            if (LoadingLabel != null)
                                LoadingLabel.Text = _localizationService.GetText("Loading") ?? "Загрузка...";
                        });
                    }
                }
            }
        }

        private async void OnGradeClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ContestSubmission sub)
            {
                // Дополнительная проверка прав
                if (!CanGrade)
                {
                    await DisplayAlert(_localizationService.GetText("AccessDenied") ?? "Доступ запрещен",
                        _localizationService.GetText("OnlyTeachersCanGrade") ?? "Оценивать могут только преподаватели",
                        _localizationService.GetText("OK") ?? "OK");
                    return;
                }

                try
                {
                    var contest = await _dbService.GetContestByIdAsync(_contestId);
                    if (contest != null)
                    {
                        var now = DateTime.Now;
                        if (now < contest.EndDate)
                        {
                            await DisplayAlert(_localizationService.GetText("GradingUnavailable") ?? "Оценка недоступна",
                                string.Format(_localizationService.GetText("GradingAvailableAfter") ?? "Оценка будет доступна после {0}",
                                    contest.EndDate.ToString("dd.MM.yyyy HH:mm")),
                                _localizationService.GetText("OK") ?? "OK");
                            return;
                        }
                    }

                    string scoreStr = await DisplayPromptAsync(_localizationService.GetText("Grade") ?? "Оценка",
                        _localizationService.GetText("EnterScore") ?? "Введите балл (0-100)",
                        initialValue: sub.TeacherScore?.ToString() ?? "0",
                        keyboard: Keyboard.Numeric);

                    if (string.IsNullOrEmpty(scoreStr)) return;

                    if (int.TryParse(scoreStr, out int score) && score >= 0 && score <= 100)
                    {
                        string? comment = await DisplayPromptAsync(_localizationService.GetText("Comment") ?? "Комментарий",
                            _localizationService.GetText("TeacherComments") ?? "Комментарий преподавателя",
                            initialValue: sub.TeacherComment ?? "");

                        // Показываем индикатор загрузки
                        if (LoadingOverlay != null)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                LoadingOverlay.IsVisible = true;
                                if (LoadingLabel != null)
                                    LoadingLabel.Text = _localizationService.GetText("Saving") ?? "Сохранение...";
                            });
                        }

                        bool ok = await _dbService.GradeContestSubmissionAsync(sub.SubmissionId, _currentUser.UserId, score, comment ?? "");

                        if (ok)
                        {
                            await DisplayAlert(_localizationService.GetText("Success") ?? "Успех",
                                _localizationService.GetText("GradeSaved") ?? "Оценка сохранена",
                                _localizationService.GetText("OK") ?? "OK");
                            await LoadSubmissionsAsync();
                        }
                        else
                        {
                            await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                                _localizationService.GetText("FailedToSaveGrade") ?? "Не удалось сохранить оценку",
                                _localizationService.GetText("OK") ?? "OK");
                        }
                    }
                    else
                    {
                        await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                            _localizationService.GetText("ScoreMustBeNumber") ?? "Оценка должна быть числом от 0 до 100",
                            _localizationService.GetText("OK") ?? "OK");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка оценивания: {ex.Message}");
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        ex.Message,
                        _localizationService.GetText("OK") ?? "OK");
                }
                finally
                {
                    if (LoadingOverlay != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            LoadingOverlay.IsVisible = false;
                            if (LoadingLabel != null)
                                LoadingLabel.Text = _localizationService.GetText("Loading") ?? "Загрузка...";
                        });
                    }
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}