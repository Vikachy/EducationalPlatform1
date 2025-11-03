using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Maui.Controls;

namespace EducationalPlatform.Views
{
    public partial class ResetPasswordPage : ContentPage
    {
        private User _currentUser;
        private DatabaseService _dbService;

        public ResetPasswordPage(User user, DatabaseService dbService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;

            // Показываем маску почты для подтверждения
            EmailMaskLabel.Text = $"Код отправлен на: ***{user.Email?.Substring(Math.Max(0, user.Email.Length - 10))}";
        }

        private async void OnResetPasswordClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewPasswordEntry.Text) ||
                    string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
                {
                    await DisplayAlert("Ошибка", "Заполните все поля", "OK");
                    return;
                }

                string newPassword = NewPasswordEntry.Text;
                string confirmPassword = ConfirmPasswordEntry.Text;

                if (newPassword.Length < 6)
                {
                    await DisplayAlert("Ошибка", "Пароль должен быть не менее 6 символов", "OK");
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    await DisplayAlert("Ошибка", "Пароли не совпадают", "OK");
                    NewPasswordEntry.Text = "";
                    ConfirmPasswordEntry.Text = "";
                    return;
                }

                // Меняем пароль
                ResetButton.IsEnabled = false;
                ResetButton.Text = "Сохраняем...";

                bool success = await _dbService.ChangePasswordWithResetAsync(_currentUser.UserId, newPassword);

                if (success)
                {
                    await DisplayAlert("Успех", "Пароль успешно изменён! Теперь вы можете войти с новым паролем.", "OK");

                    // Возвращаем на страницу логина
                    await Navigation.PopToRootAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось изменить пароль", "OK");
                    ResetButton.IsEnabled = true;
                    ResetButton.Text = "Сменить пароль";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
                ResetButton.IsEnabled = true;
                ResetButton.Text = "Сменить пароль";
            }
        }

        private void OnTogglePassword1Clicked(object sender, EventArgs e)
        {
            NewPasswordEntry.IsPassword = !NewPasswordEntry.IsPassword;
            if (sender is Button btn)
                btn.Text = NewPasswordEntry.IsPassword ? "??" : "??";
        }

        private void OnTogglePassword2Clicked(object sender, EventArgs e)
        {
            ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
            if (sender is Button btn)
                btn.Text = ConfirmPasswordEntry.IsPassword ? "??" : "??";
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}