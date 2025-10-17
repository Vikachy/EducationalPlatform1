using EducationalPlatform.Services;

namespace EducationalPlatform
{
    public partial class MainPage : ContentPage
    {
        private DatabaseService _dbService;

        public MainPage()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            CheckConnection();
        }

        private async void CheckConnection()
        {
            var isConnected = await _dbService.TestConnection();
            if (!isConnected)
            {
                await DisplayAlert("Внимание", "Проверьте подключение к SQL Server", "OK");
            }
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var user = await _dbService.LoginAsync(UsernameEntry.Text, PasswordEntry.Text);

            if (user != null)
            {
                await _dbService.UpdateLoginStreakAsync(user.UserId);

                await DisplayAlert("Успех", $"Добро пожаловать, {user.FirstName}!", "OK");

                await Navigation.PushAsync(new CoursesPage(user, _dbService));
            }
            else
            {
                await DisplayAlert("Ошибка", "Неверный логин или пароль", "OK");
            }
        }

        //private async void OnRegisterClicked(object sender, EventArgs e)
        //{
        //    var success = await _dbService.RegisterUserAsync(
        //        "newuser", "new@email.com", "password123", "Имя", "Фамилия");

        //    if (success)
        //    {
        //        await DisplayAlert("Успех", "Пользователь создан!", "OK");
        //    }
        //}


        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            
            await Navigation.PushAsync(new RegisterPage());
        }


    }
}


