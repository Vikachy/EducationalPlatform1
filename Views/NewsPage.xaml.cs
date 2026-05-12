using System.Collections.ObjectModel;
using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class NewsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;
        private string _currentFilter = "all";
        private string _searchText = "";
        private List<News> _allNews = new();

        public ObservableCollection<News> NewsItems { get; set; } = new();

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

        public bool CanManageNews => _currentUser.RoleId == 4 || _currentUser.RoleId == 3; 

        public NewsPage(User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации NewsPage: {ex.Message}");
            }

            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            BindingContext = this;

            if (CanManageNews)
            {
                CreateNewsButton.IsVisible = true;
            }

            UpdateTexts();
            LoadNews();

        }

        private void UpdateTexts()
        {
            Title = _localizationService.GetText("News") ?? "Новости";

            if (HeaderLabel != null)
                HeaderLabel.Text = _localizationService.GetText("PlatformNews") ?? "Новости платформы";

            if (SearchBar != null)
                SearchBar.Placeholder = _localizationService.GetText("SearchNews") ?? "Поиск новостей...";

            if (AllButton != null)
                AllButton.Text = _localizationService.GetText("All") ?? "📰 Все";

            if (CoursesButton != null)
                CoursesButton.Text = _localizationService.GetText("Courses") ?? "📚 Курсы";

            if (ContestsButton != null)
                ContestsButton.Text = _localizationService.GetText("Contests") ?? "🏆 Конкурсы";

            if (SystemButton != null)
                SystemButton.Text = _localizationService.GetText("System") ?? "⚙️ Система";

            if (LoadingLabel != null)
                LoadingLabel.Text = _localizationService.GetText("Loading") ?? "Загрузка...";
        }

        private async void LoadNews()
        {
            try
            {
                if (LoadingOverlay != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        LoadingOverlay.IsVisible = true;
                    });
                }

                var languageCode = _currentUser.LanguagePref ?? "ru";
                var forTeens = _currentUser.InterfaceStyle == "teen";

                _allNews = await _dbService.GetAllNewsAsync(languageCode, forTeens);

                Console.WriteLine($"✅ Загружено {_allNews.Count} новостей");

                ApplyFilters();
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        $"{_localizationService.GetText("FailedToLoadNews") ?? "Ошибка загрузки новостей"}: {ex.Message}",
                        _localizationService.GetText("OK") ?? "OK");
                });
            }
            finally
            {
                if (LoadingOverlay != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        LoadingOverlay.IsVisible = false;
                    });
                }
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = _currentFilter switch
                {
                    "courses" => _allNews.Where(n => n.Category == "courses").ToList(),
                    "contests" => _allNews.Where(n => n.Category == "contests").ToList(),
                    "system" => _allNews.Where(n => n.Category == "system").ToList(),
                    _ => _allNews.ToList()
                };

                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    var searchLower = _searchText.ToLower();
                    filtered = filtered.Where(n =>
                        n.Title.ToLower().Contains(searchLower) ||
                        n.Content.ToLower().Contains(searchLower) ||
                        (n.Summary != null && n.Summary.ToLower().Contains(searchLower))
                    ).ToList();
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    NewsItems.Clear();
                    foreach (var item in filtered)
                    {
                        NewsItems.Add(item);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка применения фильтров: {ex.Message}");
            }
        }

        private async void OnReadMoreClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int newsId)
            {
                try
                {
                    if (LoadingOverlay != null)
                        LoadingOverlay.IsVisible = true;

                    var news = await _dbService.GetNewsByIdAsync(newsId);

                    if (news != null)
                    {
                        await Navigation.PushAsync(new NewsDetailPage(news, _currentUser, _dbService, _settingsService));
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        ex.Message,
                        _localizationService.GetText("OK") ?? "OK");
                }
                finally
                {
                    if (LoadingOverlay != null)
                        LoadingOverlay.IsVisible = false;
                }
            }
        }

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            LoadNews();
        }

        private async void OnCreateNewsClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new CreateNewsPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                    ex.Message,
                    _localizationService.GetText("OK") ?? "OK");
            }
        }

        private async void OnEditNewsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int newsId)
            {
                try
                {
                    var news = _allNews.FirstOrDefault(n => n.NewsId == newsId);
                    if (news != null)
                    {
                        await Navigation.PushAsync(new EditNewsPage(news, _currentUser, _dbService, _settingsService));
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        ex.Message,
                        _localizationService.GetText("OK") ?? "OK");
                }
            }
        }

        private async void OnDeleteNewsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int newsId)
            {
                try
                {
                    bool confirm = await DisplayAlert(
                        _localizationService.GetText("Confirm") ?? "Подтверждение",
                        _localizationService.GetText("ConfirmDeleteNews") ?? "Вы уверены, что хотите удалить эту новость?",
                        _localizationService.GetText("Yes") ?? "Да",
                        _localizationService.GetText("No") ?? "Нет");

                    if (confirm)
                    {
                        if (LoadingOverlay != null)
                            LoadingOverlay.IsVisible = true;

                        bool success = await _dbService.DeleteNewsAsync(newsId);

                        if (success)
                        {
                            await DisplayAlert(_localizationService.GetText("Success") ?? "Успех",
                                _localizationService.GetText("NewsDeleted") ?? "Новость удалена",
                                _localizationService.GetText("OK") ?? "OK");
                            LoadNews();
                        }
                        else
                        {
                            await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                                _localizationService.GetText("FailedToDeleteNews") ?? "Не удалось удалить новость",
                                _localizationService.GetText("OK") ?? "OK");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        ex.Message,
                        _localizationService.GetText("OK") ?? "OK");
                }
                finally
                {
                    if (LoadingOverlay != null)
                        LoadingOverlay.IsVisible = false;
                }
            }
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                ResetFilterButtons();

                button.BackgroundColor = (Color)Application.Current.Resources["PrimaryColor"];
                button.TextColor = Colors.White;
                button.BorderColor = (Color)Application.Current.Resources["PrimaryColor"];
                button.BorderWidth = 0;

                _currentFilter = button.Text switch
                {
                    string s when s.Contains("Курсы") || s.Contains("Courses") => "courses",
                    string s when s.Contains("Конкурсы") || s.Contains("Contests") => "contests",
                    string s when s.Contains("Система") || s.Contains("System") => "system",
                    _ => "all"
                };

                ApplyFilters();
            }
        }

        private void ResetFilterButtons()
        {
            AllButton.BackgroundColor = Colors.Transparent;
            AllButton.TextColor = (Color)Application.Current.Resources["PrimaryColor"];
            AllButton.BorderColor = (Color)Application.Current.Resources["PrimaryColor"];
            AllButton.BorderWidth = 1;

            CoursesButton.BackgroundColor = Colors.Transparent;
            CoursesButton.TextColor = (Color)Application.Current.Resources["PrimaryColor"];
            CoursesButton.BorderColor = (Color)Application.Current.Resources["PrimaryColor"];
            CoursesButton.BorderWidth = 1;

            ContestsButton.BackgroundColor = Colors.Transparent;
            ContestsButton.TextColor = (Color)Application.Current.Resources["PrimaryColor"];
            ContestsButton.BorderColor = (Color)Application.Current.Resources["PrimaryColor"];
            ContestsButton.BorderWidth = 1;

            SystemButton.BackgroundColor = Colors.Transparent;
            SystemButton.TextColor = (Color)Application.Current.Resources["PrimaryColor"];
            SystemButton.BorderColor = (Color)Application.Current.Resources["PrimaryColor"];
            SystemButton.BorderWidth = 1;
        }

        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            _searchText = SearchBar.Text ?? "";
            ApplyFilters();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadNews();
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}