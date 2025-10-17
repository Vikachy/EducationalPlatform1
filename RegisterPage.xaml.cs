using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform;

public partial class RegisterPage : ContentPage
{
    private DatabaseService _dbService;

    public RegisterPage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
        LoadRoles();
        SetDefaultValues();
    }

    private async void LoadRoles()
    {
        try
        {
            // Показываем индикатор загрузки
            RolePicker.IsEnabled = false;

            var roles = await _dbService.GetRolesAsync();

            if (roles != null && roles.Any())
            {
                RolePicker.ItemsSource = roles;
                RolePicker.ItemDisplayBinding = new Binding("RoleName");
                RolePicker.SelectedIndex = 1; // Выбираем Student по умолчанию
            }
            else
            {
                await DisplayAlert("Внимание",
                    "Не удалось загрузить список ролей. Пожалуйста, попробуйте позже.", "OK");
                await Navigation.PopAsync();
                return;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить роли: {ex.Message}", "OK");


            var tempRoles = new List<Role>
        {
            new Role { RoleId = 2, RoleName = "Teacher" },
            new Role { RoleId = 3, RoleName = "Student" },
            new Role { RoleId = 4, RoleName = "ContentManager" }
        };

            RolePicker.ItemsSource = tempRoles;
            RolePicker.ItemDisplayBinding = new Binding("RoleName");
            RolePicker.SelectedIndex = 1;
        }
        finally
        {
            RolePicker.IsEnabled = true;
        }
    }




    private void SetDefaultValues()
    {
        LanguagePicker.SelectedIndex = 0; // Русский
        InterfacePicker.SelectedIndex = 0; // Стандартный
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // ПРОВЕРКА ЗАПОЛНЕНИЯ ПОЛЕЙ
        if (!ValidateInput())
            return;

        // ПОКАЗЫВАЕМ ИНДИКАТОР ЗАГРУЗКИ
        LoadingIndicator.IsVisible = true;
        RegisterButton.IsEnabled = false;

        try
        {
            // ПОДГОТАВЛИВАЕМ ДАННЫЕ
            var username = UsernameEntry.Text.Trim();
            var email = EmailEntry.Text.Trim().ToLower();
            var password = PasswordEntry.Text;
            var firstName = FirstNameEntry.Text.Trim();
            var lastName = LastNameEntry.Text.Trim();
            var selectedRole = (Role)RolePicker.SelectedItem;
            var languagePref = LanguagePicker.SelectedIndex == 0 ? "ru" : "en";
            var interfaceStyle = InterfacePicker.SelectedIndex == 0 ? "standard" : "teen";

            // РЕГИСТРИРУЕМ ПОЛЬЗОВАТЕЛЯ
            var success = await _dbService.RegisterUserAsync(
                username, email, password, firstName, lastName,
                selectedRole.RoleId, languagePref, interfaceStyle);

            if (success)
            {
                await DisplayAlert("Успех",
                    $"Аккаунт успешно создан!\nДобро пожаловать, {firstName}!", "OK");

                // ВОЗВРАЩАЕМСЯ НА СТРАНИЦУ ВХОДА
                await Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Ошибка при регистрации: {ex.Message}", "OK");
        }
        finally
        {
            // СКРЫВАЕМ ИНДИКАТОР
            LoadingIndicator.IsVisible = false;
            RegisterButton.IsEnabled = true;
        }
    }

    private bool ValidateInput()
    {
        // ПРОВЕРКА ОБЯЗАТЕЛЬНЫХ ПОЛЕЙ
        if (string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
            string.IsNullOrWhiteSpace(LastNameEntry.Text) ||
            string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
            string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
            string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
        {
            DisplayAlert("Ошибка", "Заполните все обязательные поля", "OK");
            return false;
        }

        // ПРОВЕРКА ВЫБОРА РОЛИ
        if (RolePicker.SelectedItem == null)
        {
            DisplayAlert("Ошибка", "Выберите роль", "OK");
            return false;
        }

        // ПРОВЕРКА EMAIL
        if (!IsValidEmail(EmailEntry.Text))
        {
            DisplayAlert("Ошибка", "Введите корректный email адрес", "OK");
            return false;
        }

        // ПРОВЕРКА СОВПАДЕНИЯ ПАРОЛЕЙ
        if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
        {
            DisplayAlert("Ошибка", "Пароли не совпадают", "OK");
            return false;
        }

        // ПРОВЕРКА ДЛИНЫ ПАРОЛЯ
        if (PasswordEntry.Text.Length < 6)
        {
            DisplayAlert("Ошибка", "Пароль должен содержать минимум 6 символов", "OK");
            return false;
        }

        return true;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}