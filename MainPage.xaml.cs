using EducationalPlatform.Services;
using EducationalPlatform.Models;
using EducationalPlatform.Views;
using Microsoft.Maui.Controls;

namespace EducationalPlatform
{
    public partial class MainPage : ContentPage
    {
        private DatabaseService _dbService;
        private SettingsService _settingsService;
        private CaptchaService _captchaService;
        private IEmailService _emailService;
        private string _currentCaptcha = "";

        public MainPage()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            _settingsService = new SettingsService();
            _captchaService = new CaptchaService();

            _emailService = new EmailService();

            // Устанавливаем обработчики событий
            RefreshCaptchaButton.Clicked += OnRefreshCaptchaClicked;
            ShowPasswordBtn.Clicked += OnShowPasswordClicked;
            LoginBtn.Clicked += OnLoginClicked;
            RegisterBtn.Clicked += OnRegisterClicked;
            ForgotPasswordBtn.Clicked += OnForgotPasswordClicked;

            // Устанавливаем обработчик завершения ввода для полей
            UsernameEntry.Completed += OnEntryCompleted;
            PasswordEntry.Completed += OnEntryCompleted;
            CaptchaEntry.Completed += OnEntryCompleted;

            RefreshCaptcha();
        }

        private async void OnForgotPasswordClicked(object? sender, EventArgs e)
        {
            try
            {
                var login = await DisplayPromptAsync("Восстановление пароля",
                    "Введите ваш логин или email:", "Продолжить", "Отмена");

                if (string.IsNullOrWhiteSpace(login)) return;

                // Показываем индикатор загрузки
                ForgotPasswordBtn.IsEnabled = false;
                ForgotPasswordBtn.Text = "Отправка запроса...";

                // Ищем пользователя
                var user = await _dbService.GetUserByUsernameAsync(login) ??
                           await _dbService.GetUserByEmailAsync(login);

                if (user == null)
                {
                    await DisplayAlert("Ошибка", "Пользователь с такими данными не найден.", "OK");
                    ForgotPasswordBtn.IsEnabled = true;
                    ForgotPasswordBtn.Text = "Забыли пароль?";
                    return;
                }

                // Генерируем код
                var code = new Random().Next(100000, 999999).ToString();
                Console.WriteLine($"🔑 Сгенерирован код: {code} для пользователя: {user.UserId}");

                // Пробуем сохранить код (основной метод)
                bool codeSaved = await _dbService.SavePasswordResetCodeAsync(user.UserId, code);

                if (!codeSaved)
                {
                    await DisplayAlert("Ошибка", "Не удалось создать код восстановления.", "OK");
                    ForgotPasswordBtn.IsEnabled = true;
                    ForgotPasswordBtn.Text = "Забыли пароль?";
                    return;
                }

                // Отправляем email
                bool emailSent = await _emailService.SendPasswordResetCodeAsync(
                    user.Email, user.Username, code);

                if (emailSent)
                {
                    await DisplayAlert("Успех",
                        $"Код восстановления отправлен на вашу почту {MaskEmail(user.Email)}", "OK");

                    // Запрашиваем код
                    var enteredCode = await DisplayPromptAsync("Подтверждение",
                        "Введите 6-значный код из письма:", "Подтвердить", "Отмена",
                        maxLength: 6, keyboard: Keyboard.Numeric);

                    if (string.IsNullOrWhiteSpace(enteredCode))
                    {
                        ForgotPasswordBtn.IsEnabled = true;
                        ForgotPasswordBtn.Text = "Забыли пароль?";
                        return;
                    }

                    // Проверяем код
                    var validUser = await _dbService.GetUserByResetCodeAsync(enteredCode);

                    if (validUser?.UserId == user.UserId)
                    {
                        await Navigation.PushAsync(new ResetPasswordPage(validUser, _dbService));
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Неверный или устаревший код.", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Ошибка отправки",
                        "Не удалось отправить код на вашу почту.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
                Console.WriteLine($"❌ Ошибка восстановления: {ex}");
            }
            finally
            {
                ForgotPasswordBtn.IsEnabled = true;
                ForgotPasswordBtn.Text = "Забыли пароль?";
            }
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return "***@***";

            var parts = email.Split('@');
            if (parts.Length != 2) return "***@***";

            var username = parts[0];
            var domain = parts[1];

            if (username.Length <= 2)
                return $"***@{domain}";

            return $"{username.Substring(0, 2)}***@{domain}";
        }

        private void OnEntryCompleted(object? sender, EventArgs e)
        {
            OnLoginClicked(sender, e);
        }

        private void OnShowPasswordClicked(object? sender, EventArgs e)
        {
            try
            {
                // Меняем видимость пароля
                PasswordEntry.IsPassword = !PasswordEntry.IsPassword;

                // Меняем иконку на кнопке
                ShowPasswordBtn.Text = PasswordEntry.IsPassword ? "👁" : "🙈";

                // Обновляем фокус для корректного отображения
                UpdatePasswordField();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка OnShowPasswordClicked: {ex.Message}");
            }
        }

        private void UpdatePasswordField()
        {
            // Сохраняем текущий текст и курсор
            var currentText = PasswordEntry.Text;
            var cursorPosition = PasswordEntry.CursorPosition;

            // Временно меняем текст для обновления отображения
            PasswordEntry.Text = "";
            PasswordEntry.Text = currentText;

            // Восстанавливаем позицию курсора
            PasswordEntry.CursorPosition = Math.Min(cursorPosition, currentText?.Length ?? 0);
        }

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameEntry?.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry?.Text))
            {
                await DisplayAlert("Ошибка", "Введите логин и пароль", "OK");
                return;
            }

            // Проверяем капчу если требуется
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

            // Показываем индикатор загрузки
            SetControlsEnabled(false);
            LoginActivity.IsVisible = true;
            LoginActivity.IsRunning = true;
            FullScreenLoading.IsVisible = true;

            try
            {
                var user = await _dbService.LoginAsync(UsernameEntry.Text, PasswordEntry.Text);

                if (user != null)
                {
                    _captchaService.ResetAttempts();
                    await _dbService.UpdateLoginStreakAsync(user.UserId);

                    Console.WriteLine($"✅ Пользователь {user.Username} найден");
                    Console.WriteLine($"💰 Текущий баланс: {user.GameCurrency} монет");
                    Console.WriteLine($"🆔 UserId: {user.UserId}");

                    // Применяем тему пользователя из БД (работает на всех устройствах)
                    if (!string.IsNullOrEmpty(user.InterfaceStyle))
                    {
                        _settingsService.ApplyTheme(user.InterfaceStyle);
                        Console.WriteLine($"🎨 Применена тема: {user.InterfaceStyle}");
                    }
                    else
                    {
                        // Если тема не установлена, применяем стандартную
                        _settingsService.ApplyTheme("standard");
                        await _dbService.SaveUserThemeAsync(user.UserId, "standard");
                    }

                    // ПРОВЕРЯЕМ СОГЛАСИЕ НА ОБРАБОТКУ ДАННЫХ
                    bool hasConsent = await _dbService.CheckUserPrivacyConsentAsync(user.UserId);
                    Console.WriteLine($"📝 Согласие принято: {hasConsent}");

                    if (hasConsent)
                    {
                        // Переходим на главную страницу
                        await Navigation.PushAsync(new MainDashboardPage(user, _dbService, _settingsService));

                        // Удаляем страницу логина из стека навигации
                        if (Navigation.NavigationStack.Count > 1)
                        {
                            Navigation.RemovePage(this);
                        }
                    }
                    else
                    {
                        // ПЕРЕНАПРАВЛЯЕМ НА СТРАНИЦУ СОГЛАСИЯ
                        await Navigation.PushAsync(new PrivacyConsentPage(user, _dbService, _settingsService));

                        // Удаляем страницу логина из стека навигации
                        if (Navigation.NavigationStack.Count > 1)
                        {
                            Navigation.RemovePage(this);
                        }
                    }
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
                Console.WriteLine($"❌ Ошибка входа: {ex}");
            }
            finally
            {
                // Восстанавливаем кнопки
                SetControlsEnabled(true);
                LoginActivity.IsVisible = false;
                LoginActivity.IsRunning = false;
                FullScreenLoading.IsVisible = false;
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            LoginBtn.IsEnabled = enabled;
            RegisterBtn.IsEnabled = enabled;
            ShowPasswordBtn.IsEnabled = enabled;
            UsernameEntry.IsEnabled = enabled;
            PasswordEntry.IsEnabled = enabled;
            CaptchaEntry.IsEnabled = enabled;
            RefreshCaptchaButton.IsEnabled = enabled;
        }

        private void ShowCaptcha()
        {
            CaptchaContainer.IsVisible = true;
            RefreshCaptcha();
        }

        private void HideCaptcha()
        {
            CaptchaContainer.IsVisible = false;
        }

        private void RefreshCaptcha()
        {
            _currentCaptcha = _captchaService.GenerateCaptchaText();
            CaptchaLabel.Text = _currentCaptcha;
            CaptchaEntry.Text = string.Empty;
        }

        private void OnRefreshCaptchaClicked(object? sender, EventArgs e)
        {
            RefreshCaptcha();
        }

        private async void OnRegisterClicked(object? sender, EventArgs e)
        {
            try
            {
                RegisterBtn.IsEnabled = false;
                await Navigation.PushAsync(new RegisterPage(_dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка перехода: {ex.Message}", "OK");
            }
            finally
            {
                RegisterBtn.IsEnabled = true;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Сбрасываем поля при появлении страницы
            UsernameEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            CaptchaEntry.Text = string.Empty;

            // Сбрасываем состояние пароля
            PasswordEntry.IsPassword = true;
            ShowPasswordBtn.Text = "👁";

            // Сбрасываем капчу если была показана
            if (_captchaService.IsCaptchaRequired)
            {
                ShowCaptcha();
            }
            else
            {
                HideCaptcha();
            }

            // Сбрасываем состояние кнопок
            SetControlsEnabled(true);
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
            if (result)
            {
                if (Application.Current != null)
                {
                    Application.Current.Quit();
                }
            }
        }
    }
}