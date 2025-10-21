using EducationalPlatform.Models;
using EducationalPlatform.Services;
using EducationalPlatform.Views;

namespace EducationalPlatform;

public partial class RegisterPage : ContentPage
{
    private DatabaseService _dbService;
    private SettingsService _settingsService;

    public RegisterPage(DatabaseService dbService, SettingsService settingsService)
    {
        InitializeComponent();
        _dbService = dbService;
        _settingsService = settingsService;
    }

    private async void OnPolicyTapped(object sender, TappedEventArgs e)
    {
        await DisplayAlert("Политика обработки данных",
            @"ПОЛИТИКА ОБРАБОТКИ ПЕРСОНАЛЬНЫХ ДАННЫХ
                1. Сбор данных: Мы собираем только необходимые данные для работы платформы.
                2. Использование: Данные используются для образовательного процесса.
                3. Защита: Ваши данные защищены и не передаются третьим лицам.
                4. Хранение: Данные хранятся в защищенной базе данных.
                Нажимая 'Согласен', вы подтверждаете согласие с политикой.", "Понятно");
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // Проверяем все поля
        if (FirstNameEntry == null || string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
            LastNameEntry == null || string.IsNullOrWhiteSpace(LastNameEntry.Text) ||
            UsernameEntry == null || string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
            EmailEntry == null || string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            PasswordEntry == null || string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
            ConfirmPasswordEntry == null || string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
        {
            await DisplayAlert("Ошибка", "Заполните все поля", "OK");
            return;
        }

        if (AgreementCheckBox == null || !AgreementCheckBox.IsChecked)
        {
            await DisplayAlert("Ошибка", "Необходимо согласие на обработку данных", "OK");
            return;
        }

        if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
        {
            await DisplayAlert("Ошибка", "Пароли не совпадают", "OK");
            return;
        }

        if (PasswordEntry.Text.Length < 6)
        {
            await DisplayAlert("Ошибка", "Пароль должен содержать минимум 6 символов", "OK");
            return;
        }

        // Показываем индикатор загрузки
        if (LoadingIndicator != null)
            LoadingIndicator.IsVisible = true;
        if (RegisterButton != null)
            RegisterButton.IsEnabled = false;

        try
        {
            bool success = await _dbService.RegisterUserAsync(
                UsernameEntry.Text,
                EmailEntry.Text,
                PasswordEntry.Text,
                FirstNameEntry.Text,
                LastNameEntry.Text);

            if (success)
            {
                await DisplayAlert("Успех", "Аккаунт успешно создан!", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Ошибка", "Пользователь с таким логином или email уже существует", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Ошибка при регистрации: {ex.Message}", "OK");
        }
        finally
        {
            if (LoadingIndicator != null)
                LoadingIndicator.IsVisible = false;
            if (RegisterButton != null)
                RegisterButton.IsEnabled = true;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        ShowExitConfirmation();
        return true;
    }

    private async void ShowExitConfirmation()
    {
        bool result = await DisplayAlert("Подтверждение",
            "Вы точно хотите выйти? Все несохраненные данные будут потеряны.", "Да", "Нет");

        if (result)
        {
            await Navigation.PopAsync();
        }
    }
}
