using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class MainDashboardPage : ContentPage
    {
        private User? _currentUser;
        private DatabaseService? _dbService;
        private SettingsService? _settingsService;

        // Конструктор для Shell навигации
        public MainDashboardPage()
        {
            InitializeComponent();
        }

        public MainDashboardPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            InitializeDashboard();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_currentUser != null)
            {
                InitializeDashboard();
            }
        }

        private void InitializeDashboard()
        {
            if (_currentUser == null || _settingsService == null) return;

            WelcomeLabel.Text = _settingsService.GetRandomGreeting(_currentUser.FirstName ?? "Пользователь");
            StatsLabel.Text = $"Серия: {_currentUser.StreakDays} дней 🔥 | Валюта: {_currentUser.GameCurrency} 🪙";

            // Обновляем текст серии в анимации огня
            StreakFireLabel.Text = $"Серия: {_currentUser.StreakDays} дней 🔥";

            TeacherPanel.IsVisible = _currentUser.RoleId == 2 || _currentUser.RoleId == 1;
            LoadNews();
        }

        private void LoadNews()
        {
            try
            {
                var news = new List<NewsItem>
                {
                    new() {
                        Title = "🎉 Новый курс по C#",
                        Content = "Добавлен продвинутый курс по C# с практическими заданиями и реальными проектами",
                        PublishedDate = DateTime.Now.AddDays(-1)
                    },
                    new() {
                        Title = "🏆 Конкурс программирования",
                        Content = "Примите участие в конкурсе и выиграйте игровую валюту! Тема: Веб-разработка",
                        PublishedDate = DateTime.Now.AddDays(-3)
                    }
                };
                NewsCollectionView.ItemsSource = news;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось загрузить новости: {ex.Message}");
            }
        }

        // МЕТОДЫ НАВИГАЦИИ
        private async void OnCoursesClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//Courses");
        }

        private async void OnProgressClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//Progress");
        }

        private async void OnAchievementsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Достижения", "🏆 Система достижений скоро будет доступна!", "OK");
        }

        private async void OnShopClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Магазин", "🛒 Магазин внутриигровых предметов в разработке", "OK");
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            if (_currentUser != null && _dbService != null && _settingsService != null)
            {
                await Navigation.PushAsync(new SettingsPage(_currentUser, _dbService, _settingsService));
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось перейти к настройкам", "OK");
            }
        }

        private async void OnTeacherPanelClicked(object sender, EventArgs e)
        {
            if (_currentUser?.RoleId == 2 || _currentUser?.RoleId == 3)
            {
                // Убедитесь, что передаём параметры, если они нужны
                await Shell.Current.GoToAsync($"//{nameof(TeacherDashboardPage)}");
            }
        }

        private async void OnTeacherManagementClicked(object sender, EventArgs e)
        {
            if (_currentUser?.RoleId == 2 || _currentUser?.RoleId == 3)
            {
                await DisplayAlert("Управление", "👨‍🏫 Панель управления курсами и студентами", "OK");
            }
        }

        private async void OnAllNewsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Все новости", "📢 Полная лента новостей будет доступна в следующем обновлении", "OK");
        }
    }

    public class NewsItem
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
    }
}