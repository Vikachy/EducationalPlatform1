using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class CreateTheoryPage : ContentPage
    {
        public CreateTheoryPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            Content = new Label { Text = "Страница создания теории" };
        }
    }
}