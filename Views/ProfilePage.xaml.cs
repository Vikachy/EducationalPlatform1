namespace EducationalPlatform.Views
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage()
        {
            InitializeComponent();
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Настройки", "Переход к настройкам профиля", "OK");
        }
    }
}