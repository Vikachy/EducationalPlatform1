using System.Collections.ObjectModel;
using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class NewsPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;
        private string _currentFilter = "all";

        public ObservableCollection<News> NewsItems { get; set; } = new();

        public NewsPage(User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = ServiceHelper.GetService<LocalizationService>();

            BindingContext = this;

            // Показываем кнопку создания новости только для контент-менеджеров
            if (_currentUser.RoleName == "ContentManager")
            {
                CreateNewsButton.IsVisible = true;
            }

            LoadNews();
        }

        private async void LoadNews()
        {
            try
            {
                var languageCode = _currentUser.LanguagePref ?? "ru";
                var news = await _dbService.GetNewsAsync(languageCode, _currentUser.InterfaceStyle == "teen");

                // Фильтруем новости по категории
                var filteredNews = _currentFilter switch
                {
                    "courses" => news.Where(n => n.Title.Contains("курс") || n.Title.Contains("course")).ToList(),
                    "contests" => news.Where(n => n.Title.Contains("конкурс") || n.Title.Contains("contest")).ToList(),
                    "system" => news.Where(n => n.Title.Contains("система") || n.Title.Contains("system")).ToList(),
                    _ => news.ToList()
                };

                NewsItems.Clear();
                foreach (var item in filteredNews)
                {
                    NewsItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка загрузки новостей: {ex.Message}", "OK");
            }
        }

        private async void OnReadMoreClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int newsId)
            {
                // Временная заглушка
                await DisplayAlert("Новость", "Детальная страница новости будет добавлена позже", "OK");
            }
        }

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            LoadNews();
        }

        private async void OnCreateNewsClicked(object sender, EventArgs e)
        {
            // Временная заглушка
            await DisplayAlert("Создание новости", "Функция создания новостей будет добавлена позже", "OK");
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                // Сбрасываем все кнопки
                AllButton.BackgroundColor = Color.FromArgb("#F5F5F5");
                AllButton.TextColor = Color.FromArgb("#2E86AB");
                AllButton.BorderColor = Color.FromArgb("#2E86AB");
                AllButton.BorderWidth = 1;

                CoursesButton.BackgroundColor = Color.FromArgb("#F5F5F5");
                CoursesButton.TextColor = Color.FromArgb("#2E86AB");
                CoursesButton.BorderColor = Color.FromArgb("#2E86AB");
                CoursesButton.BorderWidth = 1;

                ContestsButton.BackgroundColor = Color.FromArgb("#F5F5F5");
                ContestsButton.TextColor = Color.FromArgb("#2E86AB");
                ContestsButton.BorderColor = Color.FromArgb("#2E86AB");
                ContestsButton.BorderWidth = 1;

                SystemButton.BackgroundColor = Color.FromArgb("#F5F5F5");
                SystemButton.TextColor = Color.FromArgb("#2E86AB");
                SystemButton.BorderColor = Color.FromArgb("#2E86AB");
                SystemButton.BorderWidth = 1;

                // Выделяем выбранную кнопку
                button.BackgroundColor = Color.FromArgb("#2E86AB");
                button.TextColor = Colors.White;
                button.BorderColor = Color.FromArgb("#2E86AB");
                button.BorderWidth = 0;

                // Устанавливаем фильтр
                _currentFilter = button.Text.ToLower() switch
                {
                    "все" => "all",
                    "курсы" => "courses",
                    "конкурсы" => "contests",
                    "система" => "system",
                    _ => "all"
                };

                LoadNews();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadNews();
        }
    }
}