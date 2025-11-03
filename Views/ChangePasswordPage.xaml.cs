using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class ChangePasswordPage : ContentPage
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;

        public ChangePasswordPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
        }

        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            try
            {
                // Валидация введённых полей
                if (string.IsNullOrWhiteSpace(CurrentPasswordEntry.Text))
                {
                    await DisplayAlert("Ошибка", "Введите текущий пароль", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewPassword1Entry.Text))
                {
                    await DisplayAlert("Ошибка", "Введите новый пароль", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewPassword2Entry.Text))
                {
                    await DisplayAlert("Ошибка", "Подтвердите новый пароль", "OK");
                    return;
                }

                // Проверка текущего пароля
                var reauth = await _dbService.LoginAsync(_currentUser.Username, CurrentPasswordEntry.Text);
                if (reauth == null || reauth.UserId != _currentUser.UserId)
                {
                    await DisplayAlert("Ошибка", "Неверный текущий пароль", "OK");
                    CurrentPasswordEntry.Text = "";
                    return;
                }

                string new1 = NewPassword1Entry.Text;
                string new2 = NewPassword2Entry.Text;

                if (new1.Length < 6)
                {
                    await DisplayAlert("Ошибка", "Пароль должен содержать не менее 6 символов", "OK");
                    NewPassword1Entry.Text = "";
                    NewPassword2Entry.Text = "";
                    return;
                }

                if (new1 != new2)
                {
                    await DisplayAlert("Ошибка", "Пароли не совпадают", "OK");
                    NewPassword1Entry.Text = "";
                    NewPassword2Entry.Text = "";
                    return;
                }

                // Кнопка в состоянии загрузки
                ChangePasswordButton.IsEnabled = false;
                ChangePasswordButton.Text = "Меняем пароль...";

                // Смена пароля
                bool success = await _dbService.ChangePasswordAsync(_currentUser.UserId, new1);

                if (success)
                {
                    await DisplayAlert("Готово", "Пароль успешно изменён", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось изменить пароль", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка при смене пароля: {ex.Message}", "OK");
            }
            finally
            {
                // Восстанавливаем кнопку
                ChangePasswordButton.IsEnabled = true;
                ChangePasswordButton.Text = "Сменить пароль";
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // ��������� ���������� ������ "�����" �� Android
        protected override bool OnBackButtonPressed()
        {
            _ = Navigation.PopAsync();
            return true;
        }
    }

    // Partials for toggle handlers
    public partial class ChangePasswordPage
    {
        private void OnToggleCurrentClicked(object sender, EventArgs e)
        {
            CurrentPasswordEntry.IsPassword = !CurrentPasswordEntry.IsPassword;
            if (sender is Button b) b.Text = CurrentPasswordEntry.IsPassword ? "👁" : "🙈";
        }

        private void OnToggleNew1Clicked(object sender, EventArgs e)
        {
            NewPassword1Entry.IsPassword = !NewPassword1Entry.IsPassword;
            if (sender is Button b) b.Text = NewPassword1Entry.IsPassword ? "👁" : "🙈";
        }

        private void OnToggleNew2Clicked(object sender, EventArgs e)
        {
            NewPassword2Entry.IsPassword = !NewPassword2Entry.IsPassword;
            if (sender is Button b) b.Text = NewPassword2Entry.IsPassword ? "👁" : "🙈";
        }
    }
}