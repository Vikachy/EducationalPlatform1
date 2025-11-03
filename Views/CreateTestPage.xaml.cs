namespace EducationalPlatform.Views;

using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class CreateTestPage : ContentPage
    {
        public CreateTestPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            Content = new Label { Text = "Страница создания теста" };
        }
    }
}