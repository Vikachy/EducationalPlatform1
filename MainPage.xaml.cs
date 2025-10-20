using EducationalPlatform.Services;
using EducationalPlatform.Models;

namespace EducationalPlatform
{
    public partial class MainPage : ContentPage
    {
        private DatabaseService _dbService;
        private SettingsService _settingsService;
        private CaptchaService _captchaService;
        private string _currentCaptcha = "";

        // УБИРАЕМ ВСЕ СВОЙСТВА - используем прямые обращения к элементам
        // MAUI автоматически создает поля для элементов с x:Name

        public MainPage()
        {
            InitializeComponent();

            _dbService = new DatabaseService();
            _settingsService = new SettingsService();
            _captchaService = new CaptchaService();

            // Настраиваем обработчики событий напрямую
            RefreshCaptchaButton.Clicked += OnRefreshCaptchaClicked;
            LoginBtn.Clicked += OnLoginClicked; // Изменяем имя чтобы избежать конфликта
            RegisterBtn.Clicked += OnRegisterClicked; // Изменяем имя

            RefreshCaptcha();
        }

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            // Проверяем введенные данные
            if (string.IsNullOrWhiteSpace(UsernameEntry?.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry?.Text))
            {
                await DisplayAlert("Ошибка", "Введите логин и пароль", "OK");
                return;
            }

            // Проверка капчи если требуется
            if (_captchaService.IsCaptchaRequired)
            {
                if (string.IsNullOrWhiteSpace(CaptchaEntry?.Text) ||
                    !_captchaService.ValidateCaptcha(CaptchaEntry.Text, _currentCaptcha))
                {
                    await DisplayAlert("Ошибка", "Неверный код капчи", "OK");
                    RefreshCaptcha();
                    return;
                }
            }

            try
            {
                var user = await _dbService.LoginAsync(UsernameEntry.Text, PasswordEntry.Text);

                if (user != null)
                {
                    _captchaService.ResetAttempts();
                    await _dbService.UpdateLoginStreakAsync(user.UserId);

                    // Переходим на страницу курсов
                    await Navigation.PushAsync(new CoursesPage(user, _dbService, _settingsService));
                }
                else
                {
                    _captchaService.RecordFailedAttempt();

                    if (_captchaService.IsCaptchaRequired)
                    {
                        ShowCaptcha();
                        await DisplayAlert("Ошибка", "Неверный логин или пароль. Требуется подтверждение капчи.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Неверный логин или пароль", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка при входе: {ex.Message}", "OK");
            }
        }

        private void ShowCaptcha()
        {
            if (CaptchaContainer != null)
            {
                CaptchaContainer.IsVisible = true;
                RefreshCaptcha();
            }
        }

        private void RefreshCaptcha()
        {
            _currentCaptcha = _captchaService.GenerateCaptchaText();

            if (CaptchaLabel != null)
                CaptchaLabel.Text = _currentCaptcha;

            if (CaptchaEntry != null)
                CaptchaEntry.Text = string.Empty;
        }

        private void OnRefreshCaptchaClicked(object? sender, EventArgs e)
        {
            RefreshCaptcha();
        }

        private async void OnRegisterClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage(_dbService, _settingsService));
        }

        protected override bool OnBackButtonPressed()
        {
            ShowExitConfirmation();
            return true;
        }

        private async void ShowExitConfirmation()
        {
            bool result = await DisplayAlert("Подтверждение",
                "Вы точно хотите выйти из приложения?", "Да", "Нет");

            if (result && Application.Current != null)
            {
                Application.Current.Quit();
            }
        }
    }
}