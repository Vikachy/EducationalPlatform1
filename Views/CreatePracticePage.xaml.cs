namespace EducationalPlatform.Views;

public partial class CreatePracticePage : ContentPage
{
    public CreatePracticePage(User user, DatabaseService dbService, SettingsService settingsService)
    {
        InitializeComponent();
        Content = new Label { Text = "Страница создания практики" };
    }
}