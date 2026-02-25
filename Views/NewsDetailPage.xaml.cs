using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class NewsDetailPage : ContentPage
    {
        private readonly News _news;
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        public NewsDetailPage(News news, User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _news = news;
            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;

            BindingContext = news;
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}