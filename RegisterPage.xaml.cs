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
        await DisplayAlert("�������� ��������� ������",
            @"�������� ��������� ������������ ������
                1. ���� ������: �� �������� ������ ����������� ������ ��� ������ ���������.
                2. �������������: ������ ������������ ��� ���������������� ��������.
                3. ������: ���� ������ �������� � �� ���������� ������� �����.
                4. ��������: ������ �������� � ���������� ���� ������.
                ������� '��������', �� ������������� �������� � ���������.", "�������");
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // ��������� ��� ����
        if (FirstNameEntry == null || string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
            LastNameEntry == null || string.IsNullOrWhiteSpace(LastNameEntry.Text) ||
            UsernameEntry == null || string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
            EmailEntry == null || string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            PasswordEntry == null || string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
            ConfirmPasswordEntry == null || string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
        {
            await DisplayAlert("������", "��������� ��� ����", "OK");
            return;
        }

        if (AgreementCheckBox == null || !AgreementCheckBox.IsChecked)
        {
            await DisplayAlert("������", "���������� �������� �� ��������� ������", "OK");
            return;
        }

        if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
        {
            await DisplayAlert("������", "������ �� ���������", "OK");
            return;
        }

        if (PasswordEntry.Text.Length < 6)
        {
            await DisplayAlert("������", "������ ������ ��������� ������� 6 ��������", "OK");
            return;
        }

        // ���������� ��������� ��������
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
                await DisplayAlert("�����", "������� ������� ������!", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("������", "������������ � ����� ������� ��� email ��� ����������", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("������", $"������ ��� �����������: {ex.Message}", "OK");
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
        bool result = await DisplayAlert("�������������",
            "�� ����� ������ �����? ��� ������������� ������ ����� ��������.", "��", "���");

        if (result)
        {
            await Navigation.PopAsync();
        }
    }
}
