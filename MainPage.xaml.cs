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
        private LocalizationService _localizationService;
        private CaptchaService _captchaService;
        private IEmailService _emailService;
        private ConsentSyncService _consentSyncService;
        private string _currentCaptcha = "";

        public MainPage()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            _settingsService = App.AppSettings;
            _localizationService = App.AppLocalization;
            _captchaService = new CaptchaService();
            _emailService = new EmailService();
            _consentSyncService = new ConsentSyncService(_dbService);

            // Подписываемся на смену языка
            _localizationService.LanguageChanged += OnLanguageChanged;

            RefreshCaptchaButton.Clicked += OnRefreshCaptchaClicked;
            ShowPasswordBtn.Clicked += OnShowPasswordClicked;
            LoginBtn.Clicked += OnLoginClicked;
            RegisterBtn.Clicked += OnRegisterClicked;
            ForgotPasswordBtn.Clicked += OnForgotPasswordClicked;

            UsernameEntry.Completed += OnEntryCompleted;
            PasswordEntry.Completed += OnEntryCompleted;
            CaptchaEntry.Completed += OnEntryCompleted;

            RefreshCaptcha();
            UpdateTexts();
        }

        private void OnLanguageChanged(object? sender, string language)
        {
            MainThread.BeginInvokeOnMainThread(UpdateTexts);
        }

        private void UpdateTexts()
        {
            Title = _localizationService.GetText("Login");
            LoginBtn.Text = _localizationService.GetText("Login");
            RegisterBtn.Text = _localizationService.GetText("Register");
            ForgotPasswordBtn.Text = _localizationService.GetText("ForgotPassword");
            UsernameEntry.Placeholder = _localizationService.GetText("Username");
            PasswordEntry.Placeholder = _localizationService.GetText("Password");
            CaptchaEntry.Placeholder = _localizationService.GetText("CaptchaCode");

            var mainLabel = this.FindByName<Label>("MainLabel");
            if (mainLabel != null)
                mainLabel.Text = _localizationService.GetText("AppName");

            var captchaLabel = this.FindByName<Label>("CaptchaInstructionLabel");
            if (captchaLabel != null)
                captchaLabel.Text = _localizationService.GetText("CaptchaInstruction");
        }

        private async void OnForgotPasswordClicked(object? sender, EventArgs e)
        {
            try
            {
                var login = await DisplayPromptAsync(
                    _localizationService.GetText("PasswordRecovery"),
                    _localizationService.GetText("EnterLoginOrEmail"),
                    _localizationService.GetText("Continue"),
                    _localizationService.GetText("Cancel"));

                if (string.IsNullOrWhiteSpace(login)) return;

                ForgotPasswordBtn.IsEnabled = false;
                ForgotPasswordBtn.Text = _localizationService.GetText("SendingRequest");

                var user = await _dbService.GetUserByUsernameAsync(login) ??
                           await _dbService.GetUserByEmailAsync(login);

                if (user == null)
                {
                    await DisplayAlert(
                        _localizationService.GetText("Error"),
                        _localizationService.GetText("UserNotFound"),
                        _localizationService.GetText("OK"));
                    ForgotPasswordBtn.IsEnabled = true;
                    ForgotPasswordBtn.Text = _localizationService.GetText("ForgotPassword");
                    return;
                }

                var code = new Random().Next(100000, 999999).ToString();

                bool codeSaved = await _dbService.SavePasswordResetCodeAsync(user.UserId, code);

                if (!codeSaved)
                {
                    await DisplayAlert(
                        _localizationService.GetText("Error"),
                        _localizationService.GetText("CodeCreationFailed"),
                        _localizationService.GetText("OK"));
                    ForgotPasswordBtn.IsEnabled = true;
                    ForgotPasswordBtn.Text = _localizationService.GetText("ForgotPassword");
                    return;
                }

                bool emailSent = await _emailService.SendPasswordResetCodeAsync(
                    user.Email, user.Username, code);

                if (emailSent)
                {
                    await DisplayAlert(
                        _localizationService.GetText("Success"),
                        string.Format(_localizationService.GetText("CodeSentToEmail"), MaskEmail(user.Email)),
                        _localizationService.GetText("OK"));

                    var enteredCode = await DisplayPromptAsync(
                        _localizationService.GetText("Confirmation"),
                        _localizationService.GetText("EnterSixDigitCode"),
                        _localizationService.GetText("Confirm"),
                        _localizationService.GetText("Cancel"),
                        maxLength: 6, keyboard: Keyboard.Numeric);

                    if (string.IsNullOrWhiteSpace(enteredCode))
                    {
                        ForgotPasswordBtn.IsEnabled = true;
                        ForgotPasswordBtn.Text = _localizationService.GetText("ForgotPassword");
                        return;
                    }

                    var validUser = await _dbService.GetUserByResetCodeAsync(enteredCode);

                    if (validUser?.UserId == user.UserId)
                    {
                        await Navigation.PushAsync(new ResetPasswordPage(validUser, _dbService));
                    }
                    else
                    {
                        await DisplayAlert(
                            _localizationService.GetText("Error"),
                            _localizationService.GetText("InvalidOrExpiredCode"),
                            _localizationService.GetText("OK"));
                    }
                }
                else
                {
                    await DisplayAlert(
                        _localizationService.GetText("Error"),
                        _localizationService.GetText("FailedToSendEmail"),
                        _localizationService.GetText("OK"));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService.GetText("Error"),
                    ex.Message,
                    _localizationService.GetText("OK"));
            }
            finally
            {
                ForgotPasswordBtn.IsEnabled = true;
                ForgotPasswordBtn.Text = _localizationService.GetText("ForgotPassword");
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

        private void OnEntryCompleted(object? sender, EventArgs e) => OnLoginClicked(sender, e);

        private void OnShowPasswordClicked(object? sender, EventArgs e)
        {
            try
            {
                PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
                ShowPasswordBtn.Text = PasswordEntry.IsPassword ? "👁" : "🙈";
                UpdatePasswordField();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void UpdatePasswordField()
        {
            var currentText = PasswordEntry.Text;
            var cursorPosition = PasswordEntry.CursorPosition;
            PasswordEntry.Text = "";
            PasswordEntry.Text = currentText;
            PasswordEntry.CursorPosition = Math.Min(cursorPosition, currentText?.Length ?? 0);
        }

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameEntry?.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry?.Text))
            {
                await DisplayAlert(
                    _localizationService.GetText("Error"),
                    _localizationService.GetText("EnterLoginAndPassword"),
                    _localizationService.GetText("OK"));
                return;
            }

            if (_captchaService.IsCaptchaRequired)
            {
                if (string.IsNullOrWhiteSpace(CaptchaEntry?.Text) ||
                    !_captchaService.ValidateCaptcha(CaptchaEntry.Text, _currentCaptcha))
                {
                    await DisplayAlert(
                        _localizationService.GetText("Error"),
                        _localizationService.GetText("InvalidCaptcha"),
                        _localizationService.GetText("OK"));
                    RefreshCaptcha();
                    return;
                }
            }

            SetControlsEnabled(false);
            LoginActivity.IsVisible = true;
            LoginActivity.IsRunning = true;
            FullScreenLoading.IsVisible = true;

            try
            {
                var user = await _dbService.LoginAsync(UsernameEntry.Text, PasswordEntry.Text);
                if (user == null)
                {
                    _captchaService.RecordFailedAttempt();

                    if (_captchaService.IsCaptchaRequired)
                    {
                        ShowCaptcha();
                        await DisplayAlert(
                            _localizationService.GetText("Error"),
                            _localizationService.GetText("InvalidCredentialsWithCaptcha"),
                            _localizationService.GetText("OK"));
                    }
                    else
                    {
                        await DisplayAlert(
                            _localizationService.GetText("Error"),
                            _localizationService.GetText("InvalidCredentials"),
                            _localizationService.GetText("OK"));
                    }
                    return;
                }

                _captchaService.ResetAttempts();
                await _dbService.UpdateLoginStreakAsync(user.UserId);

                // ЗАГРУЖАЕМ ТЕМУ ИЗ БАЗЫ ДАННЫХ
                string userTheme = await _dbService.GetUserThemeAsync(user.UserId);

                // Применяем тему через SettingsService
                if (_settingsService.CurrentTheme != userTheme)
                {
                    _settingsService.CurrentTheme = userTheme;
                }

                // Инициализируем SettingsService для пользователя (для будущих сохранений)
                _settingsService.InitializeForUser(user.UserId, _dbService);

                // Устанавливаем язык
                if (!string.IsNullOrEmpty(user.LanguagePref) && _localizationService.CurrentLanguage != user.LanguagePref)
                {
                    _localizationService.CurrentLanguage = user.LanguagePref;
                }

                bool hasConsent = await _dbService.CheckUserPrivacyConsentAsync(user.UserId);

                ContentPage nextPage = hasConsent
                    ? new MainDashboardPage(user, _dbService, _settingsService)
                    : new PrivacyConsentPage(user, _dbService, _settingsService);

                Application.Current.MainPage = new NavigationPage(nextPage);
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService.GetText("Error"),
                    ex.Message,
                    _localizationService.GetText("OK"));
            }
            finally
            {
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

        private void HideCaptcha() => CaptchaContainer.IsVisible = false;

        private void RefreshCaptcha()
        {
            _currentCaptcha = _captchaService.GenerateCaptchaText();
            CaptchaLabel.Text = _currentCaptcha;
            CaptchaEntry.Text = string.Empty;
        }

        private void OnRefreshCaptchaClicked(object? sender, EventArgs e) => RefreshCaptcha();

        private async void OnRegisterClicked(object? sender, EventArgs e)
        {
            try
            {
                RegisterBtn.IsEnabled = false;
                await Navigation.PushAsync(new RegisterPage(_dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService.GetText("Error"),
                    ex.Message,
                    _localizationService.GetText("OK"));
            }
            finally
            {
                RegisterBtn.IsEnabled = true;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            UsernameEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            CaptchaEntry.Text = string.Empty;

            PasswordEntry.IsPassword = true;
            ShowPasswordBtn.Text = "👁";

            if (_captchaService.IsCaptchaRequired)
                ShowCaptcha();
            else
                HideCaptcha();

            SetControlsEnabled(true);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _localizationService.LanguageChanged -= OnLanguageChanged;
        }

        protected override bool OnBackButtonPressed()
        {
            ShowExitConfirmation();
            return true;
        }

        private async void ShowExitConfirmation()
        {
            bool result = await DisplayAlert(
                _localizationService.GetText("Confirmation"),
                _localizationService.GetText("ConfirmExit"),
                _localizationService.GetText("Yes"),
                _localizationService.GetText("No"));

            if (result)
                Application.Current?.Quit();
        }
    }
}