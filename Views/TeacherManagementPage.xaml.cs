using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using Dapper;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class TeacherManagementPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        // Элементы управления
        private Grid? _loadingOverlay;
        private ActivityIndicator? _loadingIndicator;
        private Label? _loadingLabel;
        private Entry? _newCourseNameEntry;
        private Editor? _newCourseDescriptionEditor;
        private Picker? _languagePicker;
        private Picker? _difficultyPicker;
        private CheckBox? _isGroupCourseCheckBox;
        private CollectionView? _myCoursesCollectionView;
        private CollectionView? _pendingReviewsCollectionView;

        public ObservableCollection<TeacherCourse> MyCourses { get; set; } = new();
        public ObservableCollection<PracticeSubmission> PendingReviews { get; set; } = new();

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TeacherManagementPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации TeacherManagementPage: {ex.Message}");
            }

            _currentUser = user ?? throw new ArgumentNullException(nameof(user));
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            // Инициализируем элементы управления
            InitializeControls();

            BindingContext = this;

            // Загружаем данные
            Task.Run(async () => await LoadTeacherDataAsync());
        }

        private void InitializeControls()
        {
            _loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
            _loadingIndicator = this.FindByName<ActivityIndicator>("LoadingIndicator");
            _loadingLabel = this.FindByName<Label>("LoadingLabel");
            _newCourseNameEntry = this.FindByName<Entry>("NewCourseNameEntry");
            _newCourseDescriptionEditor = this.FindByName<Editor>("NewCourseDescriptionEditor");
            _languagePicker = this.FindByName<Picker>("LanguagePicker");
            _difficultyPicker = this.FindByName<Picker>("DifficultyPicker");
            _isGroupCourseCheckBox = this.FindByName<CheckBox>("IsGroupCourseCheckBox");
            _myCoursesCollectionView = this.FindByName<CollectionView>("MyCoursesCollectionView");
            _pendingReviewsCollectionView = this.FindByName<CollectionView>("PendingReviewsCollectionView");

            // Устанавливаем источники данных для CollectionView
            if (_myCoursesCollectionView != null)
                _myCoursesCollectionView.ItemsSource = MyCourses;

            if (_pendingReviewsCollectionView != null)
                _pendingReviewsCollectionView.ItemsSource = PendingReviews;
        }

        private async Task LoadTeacherDataAsync()
        {
            try
            {
                ShowLoading(true, "Загрузка данных...");

                // 1. Курсы преподавателя
                var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MyCourses.Clear();
                    foreach (var course in courses ?? Enumerable.Empty<TeacherCourse>())
                    {
                        MyCourses.Add(course);
                    }

                    // Обновляем CollectionView
                    if (_myCoursesCollectionView != null)
                    {
                        _myCoursesCollectionView.ItemsSource = null;
                        _myCoursesCollectionView.ItemsSource = MyCourses;
                    }
                });

                // 2. Работы на проверку
                await LoadPendingReviewsAsync();

                // 3. Списки для создания курса
                var languages = await _dbService.GetProgrammingLanguagesAsync();
                var difficulties = await _dbService.GetCourseDifficultiesAsync();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_languagePicker != null)
                    {
                        if (languages != null && languages.Any())
                        {
                            _languagePicker.ItemsSource = languages;
                            _languagePicker.SelectedIndex = 0;
                        }
                    }

                    if (_difficultyPicker != null)
                    {
                        if (difficulties != null && difficulties.Any())
                        {
                            _difficultyPicker.ItemsSource = difficulties;
                            _difficultyPicker.SelectedIndex = 0;
                        }
                    }

                    ShowLoading(false);
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка загрузки", ex.Message, "OK");
                });
                ShowLoading(false);
            }
        }

        private async Task LoadPendingReviewsAsync()
        {
            try
            {
                var pending = await _dbService.GetPendingPracticeSubmissionsForTeacherAsync(_currentUser.UserId);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    PendingReviews.Clear();
                    if (pending != null && pending.Any())
                    {
                        foreach (var submission in pending)
                        {
                            PendingReviews.Add(submission);
                        }
                    }

                    // Обновляем CollectionView
                    if (_pendingReviewsCollectionView != null)
                    {
                        _pendingReviewsCollectionView.ItemsSource = null;
                        _pendingReviewsCollectionView.ItemsSource = PendingReviews;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки работ: {ex.Message}");
            }
        }

        private void ShowLoading(bool show, string message = "Загрузка...")
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_loadingOverlay != null)
                {
                    _loadingOverlay.IsVisible = show;
                    _loadingOverlay.InputTransparent = !show;
                }

                if (_loadingIndicator != null)
                    _loadingIndicator.IsRunning = show;

                if (_loadingLabel != null && !string.IsNullOrEmpty(message))
                    _loadingLabel.Text = message;
            });
        }

        private async void OnCreateCourseClicked(object sender, EventArgs e)
        {
            try
            {
                // Проверяем наличие элементов управления
                if (_newCourseNameEntry == null || string.IsNullOrWhiteSpace(_newCourseNameEntry.Text))
                {
                    await DisplayAlert("Ошибка", "Введите название курса", "OK");
                    return;
                }

                if (_languagePicker == null || _languagePicker.SelectedItem == null)
                {
                    await DisplayAlert("Ошибка", "Выберите язык программирования", "OK");
                    return;
                }

                if (_difficultyPicker == null || _difficultyPicker.SelectedItem == null)
                {
                    await DisplayAlert("Ошибка", "Выберите уровень сложности", "OK");
                    return;
                }

                ShowLoading(true, "Создание курса...");

                var lang = (ProgrammingLanguage)_languagePicker.SelectedItem;
                var diff = (CourseDifficulty)_difficultyPicker.SelectedItem;

                bool success = await _dbService.CreateCourseAsync(
                    _newCourseNameEntry.Text.Trim(),
                    _newCourseDescriptionEditor?.Text?.Trim() ?? "",
                    lang.LanguageId,
                    diff.DifficultyId,
                    _currentUser.UserId,
                    _isGroupCourseCheckBox?.IsChecked ?? false);

                if (success)
                {
                    await DisplayAlert("Успех", "Курс успешно создан!", "OK");

                    // Очищаем поля
                    _newCourseNameEntry.Text = "";
                    if (_newCourseDescriptionEditor != null)
                        _newCourseDescriptionEditor.Text = "";

                    // Сбрасываем выбор
                    if (_languagePicker != null && _languagePicker.ItemsSource != null)
                        _languagePicker.SelectedIndex = 0;

                    if (_difficultyPicker != null && _difficultyPicker.ItemsSource != null)
                        _difficultyPicker.SelectedIndex = 0;

                    if (_isGroupCourseCheckBox != null)
                        _isGroupCourseCheckBox.IsChecked = false;

                    // Перезагружаем данные
                    await LoadTeacherDataAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось создать курс", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка создания курса", ex.Message, "OK");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async void OnPublishCourseClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is TeacherCourse course)
            {
                try
                {
                    bool success = await _dbService.PublishCourseAsync(course.CourseId, _currentUser.UserId);

                    if (success)
                    {
                        course.IsPublished = !course.IsPublished;

                        var index = MyCourses.IndexOf(course);
                        if (index >= 0)
                        {
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                MyCourses[index] = course;
                                if (_myCoursesCollectionView != null)
                                {
                                    _myCoursesCollectionView.ItemsSource = null;
                                    _myCoursesCollectionView.ItemsSource = MyCourses;
                                }
                            });
                        }

                        await DisplayAlert("Успех", course.IsPublished ? "Курс опубликован" : "Публикация снята", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось изменить статус", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка публикации", ex.Message, "OK");
                }
            }
        }

        private async void OnReviewWorkClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is PracticeSubmission submission)
            {
                try
                {
                    await Navigation.PushAsync(new ReviewSubmissionPage(
                        _currentUser,
                        _dbService,
                        _settingsService,
                        submission));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть страницу проверки: {ex.Message}", "OK");
                }
            }
        }

        private async void OnManageGroupsClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is TeacherCourse course)
            {
                try
                {
                    await Navigation.PushAsync(new TeacherGroupsPage(_currentUser, _dbService, _settingsService, course));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть страницу групп: {ex.Message}", "OK");
                }
            }
        }

        private async void OnCourseStatsClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is int courseId)
            {
                try
                {
                    var course = MyCourses.FirstOrDefault(c => c.CourseId == courseId);
                    if (course != null)
                    {
                        await Navigation.PushAsync(new CourseStudentsPage(_currentUser, _dbService, _settingsService, courseId, course.CourseName));
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть статистику: {ex.Message}", "OK");
                }
            }
        }

        private async void OnManageContentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is TeacherCourse course)
            {
                try
                {
                    await Navigation.PushAsync(new TeacherContentManagementPage(_currentUser, _dbService, _settingsService, course.CourseId, course.CourseName));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть управление контентом: {ex.Message}", "OK");
                }
            }
        }

        private async void OnReportsClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new TeacherReportsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть отчеты: {ex.Message}", "OK");
            }
        }

        private async void OnTestsClicked(object sender, EventArgs e)
        {
            try
            {
                if (MyCourses.Any())
                {
                    var firstCourse = MyCourses.First();
                    await Navigation.PushAsync(new CreateTestPage(_currentUser, _dbService, _settingsService, firstCourse.CourseId));
                }
                else
                {
                    await DisplayAlert("Информация", "Сначала создайте курс", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось создать тест: {ex.Message}", "OK");
            }
        }

        private async void OnGroupsClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new TeacherGroupsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть группы: {ex.Message}", "OK");
            }
        }

        private async void OnChatsClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new TeacherChatsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть чаты: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Загружаем данные при появлении страницы
            Task.Run(async () => await LoadTeacherDataAsync());

            // Автообновление каждые 30 секунд
            Device.StartTimer(TimeSpan.FromSeconds(30), () =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadPendingReviewsAsync();
                });
                return true; // Продолжаем таймер
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Здесь можно остановить таймер если нужно
        }
    }
}