using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class ContestPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;

        public ObservableCollection<Contest> ActiveContests { get; set; } = new();
        public ObservableCollection<Contest> CompletedContests { get; set; } = new();
        public ObservableCollection<ContestSubmission> MySubmissions { get; set; } = new();

        private string _title;
        public new string Title
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

        // Свойства для XAML
        public bool IsStudent => _currentUser.RoleId == 1;
        public bool CanManageContest => _currentUser.RoleId == 2 || _currentUser.RoleId == 3 || _currentUser.RoleId == 4; // Teacher, Admin, ContentManager

        public ContestPage(User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации ContestPage: {ex.Message}");
            }

            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            BindingContext = this;

            // Устанавливаем заголовок
            UpdateTitle();

            // Подписываемся на изменение языка
            _localizationService.LanguageChanged += OnLanguageChanged;

            // Загружаем данные
            Task.Run(async () => await LoadContestsAsync());
            UpdateTexts();
        }

        private void UpdateTitle()
        {
            Title = _localizationService?.GetText("Contests") ?? "Конкурсы";
        }

        private void OnLanguageChanged(object? sender, string language)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                UpdateTitle();
                UpdateTexts();
                // Перезагружаем данные для обновления текста
                Task.Run(async () => await LoadContestsAsync());
            });
        }

        private void UpdateTexts()
        {
            try
            {
                var headerLabel = this.FindByName<Label>("HeaderLabel");
                if (headerLabel != null)
                    headerLabel.Text = _localizationService.GetText("ProgrammingContests");

                var activeLabel = this.FindByName<Label>("ActiveContestsLabel");
                if (activeLabel != null)
                    activeLabel.Text = _localizationService.GetText("ActiveContests");

                var completedLabel = this.FindByName<Label>("CompletedContestsLabel");
                if (completedLabel != null)
                    completedLabel.Text = _localizationService.GetText("CompletedContests");

                var mySubmissionsLabel = this.FindByName<Label>("MySubmissionsLabel");
                if (mySubmissionsLabel != null)
                    mySubmissionsLabel.Text = _localizationService.GetText("MySubmissions");

                var createButton = this.FindByName<Button>("CreateContestButton");
                if (createButton != null)
                {
                    createButton.Text = "➕";
                    createButton.IsVisible = CanManageContest;
                }

                var submitButton = this.FindByName<Button>("CreateSubmissionButton");
                if (submitButton != null)
                {
                    submitButton.Text = _localizationService.GetText("SubmitProject");
                    submitButton.IsVisible = IsStudent;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления текстов: {ex.Message}");
            }
        }

        private async Task LoadContestsAsync()
        {
            try
            {
                // Загружаем активные конкурсы
                var activeContests = await _dbService.GetActiveContestsAsync();

                // Загружаем завершенные конкурсы
                var completedContests = await _dbService.GetCompletedContestsAsync();

                // Загружаем мои заявки
                var mySubmissions = await _dbService.GetUserContestSubmissionsAsync(_currentUser.UserId);

                // Обновляем UI в главном потоке
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ActiveContests.Clear();
                    foreach (var contest in activeContests)
                    {
                        ActiveContests.Add(contest);
                    }

                    CompletedContests.Clear();
                    foreach (var contest in completedContests)
                    {
                        CompletedContests.Add(contest);
                    }

                    MySubmissions.Clear();
                    foreach (var submission in mySubmissions)
                    {
                        MySubmissions.Add(submission);
                    }
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert(_localizationService.GetText("Error"),
                        $"{_localizationService.GetText("FailedToLoadContests")}: {ex.Message}",
                        _localizationService.GetText("OK"));
                });
            }
        }

        private async void OnParticipateClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int contestId)
            {
                if (!IsStudent)
                {
                    await DisplayAlert(_localizationService.GetText("AccessDenied"),
                        _localizationService.GetText("OnlyStudentsCanParticipate"),
                        _localizationService.GetText("OK"));
                    return;
                }

                var contest = ActiveContests.FirstOrDefault(c => c.ContestId == contestId);
                if (contest != null && contest.OnlyForGroups)
                {
                    bool inGroup = await _dbService.IsUserInAnyActiveGroupAsync(_currentUser.UserId);
                    if (!inGroup)
                    {
                        await DisplayAlert(_localizationService.GetText("AccessRestricted"),
                            _localizationService.GetText("ContestOnlyForGroups"),
                            _localizationService.GetText("OK"));
                        return;
                    }
                }
                await Navigation.PushAsync(new ContestSubmissionPage(contestId, _currentUser, _dbService, _settingsService));
            }
        }

        private async void OnViewResultsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int contestId)
            {
                await Navigation.PushAsync(new ContestResultsPage(contestId, _currentUser, _dbService, _settingsService));
            }
        }

        private async void OnCreateSubmissionClicked(object sender, EventArgs e)
        {
            if (!IsStudent)
            {
                await DisplayAlert(_localizationService.GetText("AccessDenied"),
                    _localizationService.GetText("OnlyStudentsCanParticipate"),
                    _localizationService.GetText("OK"));
                return;
            }

            var activeContests = ActiveContests.ToList();
            if (activeContests.Count == 0)
            {
                await DisplayAlert(_localizationService.GetText("Info"),
                    _localizationService.GetText("NoActiveContests"),
                    _localizationService.GetText("OK"));
                return;
            }

            var contestNames = activeContests.Select(c => c.ContestName).ToArray();
            var selectedContest = await DisplayActionSheet(_localizationService.GetText("SelectContest"),
                _localizationService.GetText("Cancel"), null, contestNames);

            if (selectedContest != null && selectedContest != _localizationService.GetText("Cancel"))
            {
                var contest = activeContests.FirstOrDefault(c => c.ContestName == selectedContest);
                if (contest != null)
                {
                    if (contest.OnlyForGroups)
                    {
                        bool inGroup = await _dbService.IsUserInAnyActiveGroupAsync(_currentUser.UserId);
                        if (!inGroup)
                        {
                            await DisplayAlert(_localizationService.GetText("AccessRestricted"),
                                _localizationService.GetText("ContestOnlyForGroups"),
                                _localizationService.GetText("OK"));
                            return;
                        }
                    }
                    await Navigation.PushAsync(new ContestSubmissionPage(contest.ContestId, _currentUser, _dbService, _settingsService));
                }
            }
        }

        private async void OnCreateContestClicked(object sender, EventArgs e)
        {
            if (!CanManageContest)
            {
                await DisplayAlert(_localizationService.GetText("AccessDenied"),
                    _localizationService.GetText("OnlyTeachersCanCreateContests"),
                    _localizationService.GetText("OK"));
                return;
            }

            await Navigation.PushAsync(new CreateContestPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnEditContestClicked(object sender, EventArgs e)
        {
            if (!CanManageContest)
            {
                await DisplayAlert(_localizationService.GetText("AccessDenied"),
                    _localizationService.GetText("OnlyTeachersCanEditContests"),
                    _localizationService.GetText("OK"));
                return;
            }

            if (sender is Button button && button.CommandParameter is int contestId)
            {
                try
                {
                    // Показываем индикатор загрузки
                    var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                    if (loadingOverlay != null)
                        loadingOverlay.IsVisible = true;

                    // Ищем конкурс в активных или завершенных
                    var contest = ActiveContests.FirstOrDefault(c => c.ContestId == contestId) ??
                                 CompletedContests.FirstOrDefault(c => c.ContestId == contestId);

                    if (contest == null)
                    {
                        // Если не нашли в кэше, загружаем из БД
                        contest = await _dbService.GetContestByIdAsync(contestId);
                    }

                    if (contest != null)
                    {
                        // Проверяем права на редактирование
                        bool canEdit = _currentUser.RoleId == 3 || // Admin
                                      (_currentUser.RoleId == 2 && contest.CreatedByUserId == _currentUser.UserId); // Teacher who created

                        if (!canEdit)
                        {
                            await DisplayAlert(_localizationService.GetText("AccessDenied"),
                                _localizationService.GetText("OnlyCreatorCanEdit"),
                                _localizationService.GetText("OK"));
                            return;
                        }

                        // Переходим на страницу редактирования
                        await Navigation.PushAsync(new EditContestPage(contest, _currentUser, _dbService, _settingsService));
                    }
                    else
                    {
                        await DisplayAlert(_localizationService.GetText("Error"),
                            _localizationService.GetText("ContestNotFound"),
                            _localizationService.GetText("OK"));
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert(_localizationService.GetText("Error"),
                        ex.Message,
                        _localizationService.GetText("OK"));
                }
                finally
                {
                    var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                    if (loadingOverlay != null)
                        loadingOverlay.IsVisible = false;
                }
            }
        }

        private async void OnDeleteContestClicked(object sender, EventArgs e)
        {
            if (!CanManageContest)
            {
                await DisplayAlert(_localizationService.GetText("AccessDenied"),
                    _localizationService.GetText("OnlyTeachersCanDeleteContests"),
                    _localizationService.GetText("OK"));
                return;
            }

            if (sender is Button button && button.CommandParameter is int contestId)
            {
                var contest = ActiveContests.FirstOrDefault(c => c.ContestId == contestId) ??
                             CompletedContests.FirstOrDefault(c => c.ContestId == contestId);

                if (contest != null)
                {
                    // Проверяем, является ли текущий пользователь создателем конкурса
                    if (contest.CreatedByUserId != _currentUser.UserId && _currentUser.RoleId != 3) // Admin может удалять любые
                    {
                        await DisplayAlert(_localizationService.GetText("AccessDenied"),
                            _localizationService.GetText("OnlyCreatorCanDelete"),
                            _localizationService.GetText("OK"));
                        return;
                    }

                    bool confirm = await DisplayAlert(_localizationService.GetText("Confirm"),
                        string.Format(_localizationService.GetText("ConfirmDeleteContest"), contest.ContestName),
                        _localizationService.GetText("Yes"),
                        _localizationService.GetText("No"));

                    if (confirm)
                    {
                        bool success = await _dbService.DeleteContestAsync(contestId, _currentUser.UserId);

                        if (success)
                        {
                            await DisplayAlert(_localizationService.GetText("Success"),
                                _localizationService.GetText("ContestDeleted"),
                                _localizationService.GetText("OK"));

                            await LoadContestsAsync();
                        }
                        else
                        {
                            await DisplayAlert(_localizationService.GetText("Error"),
                                _localizationService.GetText("FailedToDeleteContest"),
                                _localizationService.GetText("OK"));
                        }
                    }
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnMyResultsClicked(object sender, EventArgs e)
        {
            if (MySubmissions.Count == 0)
            {
                await DisplayAlert(_localizationService.GetText("Info"),
                    _localizationService.GetText("NoSubmissions"),
                    _localizationService.GetText("OK"));
            }
            else
            {
                // Показываем список моих заявок
                var submissions = MySubmissions.Take(5).Select(s =>
                    $"{s.ContestName}: {s.ProjectName} - {s.StatusDisplay}").ToList();

                var message = string.Join("\n", submissions);
                if (MySubmissions.Count > 5)
                {
                    message += $"\n... и еще {MySubmissions.Count - 5}";
                }

                await DisplayAlert(_localizationService.GetText("MySubmissions"),
                    message,
                    _localizationService.GetText("OK"));
            }
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadContestsAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Перезагружаем данные при каждом появлении страницы
            Task.Run(async () => await LoadContestsAsync());
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _localizationService.LanguageChanged -= OnLanguageChanged;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}