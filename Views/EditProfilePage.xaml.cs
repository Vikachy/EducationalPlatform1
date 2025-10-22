using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace EducationalPlatform.Views
{
    public partial class EditProfilePage : ContentPage
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;
        private FileResult? _selectedImage;
        private string? _avatarUrl;

        public EditProfilePage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _avatarUrl = user.AvatarUrl;
            LoadUserData();
        }

        private async void LoadUserData()
        {
            try
            {
                FirstNameEntry.Text = _currentUser.FirstName ?? "";
                LastNameEntry.Text = _currentUser.LastName ?? "";
                UsernameEntry.Text = _currentUser.Username ?? "";
                EmailEntry.Text = _currentUser.Email ?? "";

                // ��������� ���������� ������ �� ����
                var currentAvatar = await _dbService.GetUserAvatarAsync(_currentUser.UserId);

                if (!string.IsNullOrEmpty(currentAvatar))
                {
                    AvatarPreview.Source = ImageSource.FromFile(currentAvatar);
                    _avatarUrl = currentAvatar;
                    _currentUser.AvatarUrl = currentAvatar;
                }
                else
                {
                    AvatarPreview.Source = "default_avatar.png";
                    _avatarUrl = null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"�� ������� ��������� ������: {ex.Message}", "OK");
            }
        }

        private async void OnSelectImageClicked(object sender, EventArgs e)
        {
            try
            {
                // ��������� ����������
                var status = await Permissions.RequestAsync<Permissions.Photos>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("������", "���������� ���������� �� ������ � �����������", "OK");
                    return;
                }

                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "�������� ������",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    _selectedImage = result;

                    // ���������� ������ �� ����� ������
                    AvatarPreview.Source = ImageSource.FromFile(result.FullPath);

                    Console.WriteLine($"������ ����: {result.FileName}, ����: {result.FullPath}");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"�� ������� ������� �����������: {ex.Message}", "OK");
            }
        }

        private void OnRemoveImageClicked(object sender, EventArgs e)
        {
            try
            {
                _selectedImage = null;
                _avatarUrl = null;
                AvatarPreview.Source = "default_avatar.png";

                // ������� ������ �� ���� ������
                _ = Task.Run(async () =>
                {
                    await _dbService.UpdateUserAsync(
                        _currentUser.UserId,
                        _currentUser.FirstName ?? "",
                        _currentUser.LastName ?? "",
                        _currentUser.Username ?? "",
                        _currentUser.Email ?? "",
                        null);
                });

                DisplayAlert("�����", "������ ������!", "OK");
            }
            catch (Exception ex)
            {
                DisplayAlert("������", $"�� ������� ������� ������: {ex.Message}", "OK");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
                string.IsNullOrWhiteSpace(LastNameEntry.Text) ||
                string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                await DisplayAlert("������", "��������� ��� ����", "OK");
                return;
            }

            // ��������� ������������ username � email
            bool userExists = await _dbService.CheckUserExistsAsync(
                UsernameEntry.Text,
                EmailEntry.Text,
                _currentUser.UserId);

            if (userExists)
            {
                await DisplayAlert("������", "������������ � ����� ������� ��� email ��� ����������", "OK");
                return;
            }

            // ���������� ��������� ��������
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            try
            {
                string finalAvatarUrl = _avatarUrl;

                // ��������� ����� ����������� ���� �������
                if (_selectedImage != null)
                {
                    Console.WriteLine($"�������� �������� �������: {_selectedImage.FileName}");

                    using var stream = await _selectedImage.OpenReadAsync();
                    finalAvatarUrl = await _dbService.UploadAvatarAsync(stream, _selectedImage.FileName, _currentUser.UserId);

                    if (string.IsNullOrEmpty(finalAvatarUrl))
                    {
                        await DisplayAlert("������", "�� ������� ��������� ������", "OK");
                        LoadingIndicator.IsVisible = false;
                        LoadingIndicator.IsRunning = false;
                        return;
                    }

                    Console.WriteLine($"������ ������� ��������: {finalAvatarUrl}");
                    await DisplayAlert("�����", "������ ������� ��������!", "OK");
                }

                // ��������� ������ ������������
                bool success = await _dbService.UpdateUserAsync(
                    _currentUser.UserId,
                    FirstNameEntry.Text,
                    LastNameEntry.Text,
                    UsernameEntry.Text,
                    EmailEntry.Text,
                    finalAvatarUrl);

                if (success)
                {
                    // ��������� ������ � ������� ������������
                    _currentUser.FirstName = FirstNameEntry.Text;
                    _currentUser.LastName = LastNameEntry.Text;
                    _currentUser.Username = UsernameEntry.Text;
                    _currentUser.Email = EmailEntry.Text;
                    _currentUser.AvatarUrl = finalAvatarUrl;

                    await DisplayAlert("�����", "������� ������� ��������!", "OK");

                    // ������������ �� �������� ������� � ������������ �������
                    var profilePage = new ProfilePage(_currentUser, _dbService, _settingsService);
                    await Navigation.PushAsync(profilePage);

                    // ������� ������� �������� �� ����� ���������
                    var existingPages = Navigation.NavigationStack.ToList();
                    foreach (var page in existingPages)
                    {
                        if (page is EditProfilePage)
                        {
                            Navigation.RemovePage(page);
                            break;
                        }
                    }
                }
                else
                {
                    await DisplayAlert("������", "�� ������� �������� �������", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"�� ������� ��������� ���������: {ex.Message}", "OK");
                Console.WriteLine($"������ ����������: {ex}");
            }
            finally
            {
                // �������� ��������� ��������
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            try
            {
                // ������������ �� �������� ������� ��� ����������
                var profilePage = new ProfilePage(_currentUser, _dbService, _settingsService);
                await Navigation.PushAsync(profilePage);

                // ������� ������� �������� �� ����� ���������
                var existingPages = Navigation.NavigationStack.ToList();
                foreach (var page in existingPages)
                {
                    if (page is EditProfilePage)
                    {
                        Navigation.RemovePage(page);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"�� ������� ���������: {ex.Message}", "OK");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // ��������� ����������� ��������� ������ �����
            OnCancelClicked(null, null);
            return true;
        }

        // ���������� ��������� ������ � ����� ��� ��������� � �������� �������
        private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private void ValidateForm()
        {
            bool isValid = !string.IsNullOrWhiteSpace(FirstNameEntry.Text) &&
                          !string.IsNullOrWhiteSpace(LastNameEntry.Text) &&
                          !string.IsNullOrWhiteSpace(UsernameEntry.Text) &&
                          !string.IsNullOrWhiteSpace(EmailEntry.Text) &&
                          IsValidEmail(EmailEntry.Text);

            // ����� �������� ���������� ��������� ���������� �����
            if (isValid)
            {
                // ����� �������
            }
            else
            {
                // ����� ���������
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

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

        // ���������� ��� �������� email ��� ������ ������
        private async void OnEmailUnfocused(object sender, FocusEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(EmailEntry.Text) && !IsValidEmail(EmailEntry.Text))
            {
                await DisplayAlert("������", "������� ���������� email �����", "OK");
                EmailEntry.Focus();
            }
        }

        // ���������� ��� �������� username ��� ������ ������
        private async void OnUsernameUnfocused(object sender, FocusEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UsernameEntry.Text))
            {
                // ��������� ����������� ����� username
                if (UsernameEntry.Text.Length < 3)
                {
                    await DisplayAlert("������", "����� ������ ��������� ������� 3 �������", "OK");
                    UsernameEntry.Focus();
                    return;
                }

                // ��������� ������������ username (����� �������� ������������)
                bool userExists = await _dbService.CheckUserExistsAsync(
                    UsernameEntry.Text,
                    "", // ������ email ��� �������� ������ username
                    _currentUser.UserId);

                if (userExists)
                {
                    await DisplayAlert("������", "������������ � ����� ������� ��� ����������", "OK");
                    UsernameEntry.Focus();
                }
            }
        }
    }
}