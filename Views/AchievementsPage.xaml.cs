using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class AchievementsPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;
        private ObservableCollection<Achievement> _achievements = new();

        public AchievementsPage(User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            BindingContext = _achievements;
            LoadAchievements();
        }

        private async void LoadAchievements()
        {
            try
            {
                var achievements = await _dbService.GetUserAchievementsAsync(_currentUser.UserId);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _achievements.Clear();
                    foreach (var a in achievements)
                    {
                        _achievements.Add(a);
                    }

                    CountLabel.Text = _achievements.Count.ToString();
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить достижения: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}