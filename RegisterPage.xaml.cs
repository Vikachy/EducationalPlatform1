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
            // ���������� ��������� ��������
            RolePicker.IsEnabled = false;

            var roles = await _dbService.GetRolesAsync();

            if (roles != null && roles.Any())
            {
                RolePicker.ItemsSource = roles;
                RolePicker.ItemDisplayBinding = new Binding("RoleName");
                RolePicker.SelectedIndex = 1; // �������� Student �� ���������
            }
            else
            {
                await DisplayAlert("��������",
                    "�� ������� ��������� ������ �����. ����������, ���������� �����.", "OK");
                await Navigation.PopAsync();
                return;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("������", $"�� ������� ��������� ����: {ex.Message}", "OK");


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
        LanguagePicker.SelectedIndex = 0; // �������
        InterfacePicker.SelectedIndex = 0; // �����������
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // �������� ���������� �����
        if (!ValidateInput())
            return;

        // ���������� ��������� ��������
        LoadingIndicator.IsVisible = true;
        RegisterButton.IsEnabled = false;

        try
        {
            // �������������� ������
            var username = UsernameEntry.Text.Trim();
            var email = EmailEntry.Text.Trim().ToLower();
            var password = PasswordEntry.Text;
            var firstName = FirstNameEntry.Text.Trim();
            var lastName = LastNameEntry.Text.Trim();
            var selectedRole = (Role)RolePicker.SelectedItem;
            var languagePref = LanguagePicker.SelectedIndex == 0 ? "ru" : "en";
            var interfaceStyle = InterfacePicker.SelectedIndex == 0 ? "standard" : "teen";

            // ������������ ������������
            var success = await _dbService.RegisterUserAsync(
                username, email, password, firstName, lastName,
                selectedRole.RoleId, languagePref, interfaceStyle);

            if (success)
            {
                await DisplayAlert("�����",
                    $"������� ������� ������!\n����� ����������, {firstName}!", "OK");

                // ������������ �� �������� �����
                await Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("������", $"������ ��� �����������: {ex.Message}", "OK");
        }
        finally
        {
            // �������� ���������
            LoadingIndicator.IsVisible = false;
            RegisterButton.IsEnabled = true;
        }
    }

    private bool ValidateInput()
    {
        // �������� ������������ �����
        if (string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
            string.IsNullOrWhiteSpace(LastNameEntry.Text) ||
            string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
            string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
            string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
        {
            DisplayAlert("������", "��������� ��� ������������ ����", "OK");
            return false;
        }

        // �������� ������ ����
        if (RolePicker.SelectedItem == null)
        {
            DisplayAlert("������", "�������� ����", "OK");
            return false;
        }

        // �������� EMAIL
        if (!IsValidEmail(EmailEntry.Text))
        {
            DisplayAlert("������", "������� ���������� email �����", "OK");
            return false;
        }

        // �������� ���������� �������
        if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
        {
            DisplayAlert("������", "������ �� ���������", "OK");
            return false;
        }

        // �������� ����� ������
        if (PasswordEntry.Text.Length < 6)
        {
            DisplayAlert("������", "������ ������ ��������� ������� 6 ��������", "OK");
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