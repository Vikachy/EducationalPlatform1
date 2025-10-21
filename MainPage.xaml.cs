using EducationalPlatform.Services;
using EducationalPlatform.Models;
using EducationalPlatform.Views;

namespace EducationalPlatform
{
    public partial class MainPage : ContentPage
    {
        private DatabaseService _dbService;
        private SettingsService _settingsService;
        private CaptchaService _captchaService;
        private string _currentCaptcha = "";

        public MainPage()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            _settingsService = new SettingsService();
            _captchaService = new CaptchaService();

            RefreshCaptchaButton.Clicked += OnRefreshCaptchaClicked;
            LoginBtn.Clicked += OnLoginClicked;
            RegisterBtn.Clicked += OnRegisterClicked;

            RefreshCaptcha();
        }

        private void OnEntryCompleted(object sender, EventArgs e)
        {
            OnLoginClicked(sender, e);
        }


        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameEntry?.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry?.Text))
            {
                await DisplayAlert("Ошибка", "Введите логин и пароль", "OK");
                return;
            }

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

                    // ПОКАЗЫВАЕМ GIF-АНИМАЦИЮ ПЕРЕД ПЕРЕХОДОМ
                    await ShowWelcomeAnimation(user);

                    // ПЕРЕХОДИМ НА ГЛАВНУЮ ПАНЕЛЬ
                    await Navigation.PushAsync(new Views.MainDashboardPage(user, _dbService, _settingsService));
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

        private async Task ShowWelcomeAnimation(User user)
        {
            // Создаем страницу с GIF-анимацией
            var animationPage = new ContentPage
            {
                BackgroundColor = Color.FromArgb("#000000"),
                Content = new Grid
                {
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalOptions = LayoutOptions.Fill,
                    Children =
            {
                // GIF анимация огня
                new Image
                {
                    Source = "fire_animation.gif",
                    Aspect = Aspect.AspectFill,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill
                },
                // Наложение с текстом
                new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 20,
                    Children =
                    {
                        new Label
                        {
                            Text = "🔥 CODING FIRE 🔥",
                            TextColor = Colors.White,
                            FontSize = 24,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = $"Добро пожаловать, {user.FirstName}!",
                            TextColor = Colors.White,
                            FontSize = 18,
                            HorizontalOptions = LayoutOptions.Center
                        }
                    }
                }
            }
                }
            };

            // Показываем анимацию
            await Navigation.PushModalAsync(animationPage);

            // Ждем 3 секунды
            await Task.Delay(3000);

            // Закрываем анимацию
            await Navigation.PopModalAsync();
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