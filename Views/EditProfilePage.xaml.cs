using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class EditProfilePage : ContentPage
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;
        private FileResult _selectedImage;
        private string _avatarUrl;

        public EditProfilePage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _avatarUrl = user.AvatarUrl;
            LoadUserData();
        }

        private void LoadUserData()
        {
            FirstNameEntry.Text = _currentUser.FirstName ?? "";
            LastNameEntry.Text = _currentUser.LastName ?? "";
            UsernameEntry.Text = _currentUser.Username ?? "";
            EmailEntry.Text = _currentUser.Email ?? "";

            // Загружаем аватар если есть
            if (!string.IsNullOrEmpty(_currentUser.AvatarUrl))
            {
                AvatarPreview.Source = ImageSource.FromUri(new Uri(_currentUser.AvatarUrl));
            }
            else
            {
                AvatarPreview.Source = "default_avatar.png";
            }
        }

        private async void OnSelectImageClicked(object sender, EventArgs e)
        {
            try
            {
                // Проверяем разрешения
                var status = await Permissions.RequestAsync<Permissions.Photos>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Ошибка", "Необходимо разрешение на доступ к фотографиям", "OK");
                    return;
                }

                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите аватар",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    _selectedImage = result;

                    // Показываем превью выбранного изображения
                    AvatarPreview.Source = ImageSource.FromFile(result.FullPath);

                    await DisplayAlert("Успех", "Изображение выбрано!", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выбрать изображение: {ex.Message}", "OK");
            }
        }

        private void OnRemoveImageClicked(object sender, EventArgs e)
        {
            _selectedImage = null;
            _avatarUrl = null;
            AvatarPreview.Source = "default_avatar.png";
            DisplayAlert("Успех", "Аватар удален!", "OK");
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
                string.IsNullOrWhiteSpace(LastNameEntry.Text) ||
                string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                await DisplayAlert("Ошибка", "Заполните все поля", "OK");
                return;
            }

            // Проверяем уникальность username и email
            bool userExists = await _dbService.CheckUserExistsAsync(
                UsernameEntry.Text,
                EmailEntry.Text,
                _currentUser.UserId);

            if (userExists)
            {
                await DisplayAlert("Ошибка", "Пользователь с таким логином или email уже существует", "OK");
                return;
            }

            // Показываем индикатор загрузки
            var loadingIndicator = this.FindByName<ActivityIndicator>("LoadingIndicator");
            if (loadingIndicator != null)
            {
                loadingIndicator.IsVisible = true;
                loadingIndicator.IsRunning = true;
            }

            try
            {
                string finalAvatarUrl = _avatarUrl;

                // Загружаем новое изображение если выбрано
                if (_selectedImage != null)
                {
                    using var stream = await _selectedImage.OpenReadAsync();
                    finalAvatarUrl = await _dbService.UploadAvatarAsync(stream, _selectedImage.FileName, _currentUser.UserId);

                    if (string.IsNullOrEmpty(finalAvatarUrl))
                    {
                        await DisplayAlert("Ошибка", "Не удалось загрузить аватар", "OK");
                        return;
                    }
                }

                // Обновляем данные пользователя
                bool success = await _dbService.UpdateUserAsync(
                    _currentUser.UserId,
                    FirstNameEntry.Text,
                    LastNameEntry.Text,
                    UsernameEntry.Text,
                    EmailEntry.Text,
                    finalAvatarUrl);

                if (success)
                {
                    // Обновляем данные в текущем пользователе
                    _currentUser.FirstName = FirstNameEntry.Text;
                    _currentUser.LastName = LastNameEntry.Text;
                    _currentUser.Username = UsernameEntry.Text;
                    _currentUser.Email = EmailEntry.Text;
                    _currentUser.AvatarUrl = finalAvatarUrl;

                    await DisplayAlert("Успех", "Профиль успешно обновлен!", "OK");

                    // Возвращаемся на страницу профиля с обновленными данными
                    await Navigation.PushAsync(new ProfilePage(_currentUser, _dbService, _settingsService));

                    // Удаляем текущую страницу из стека навигации
                    Navigation.RemovePage(this);
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось обновить профиль", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось сохранить изменения: {ex.Message}", "OK");
            }
            finally
            {
                // Скрываем индикатор загрузки
                if (loadingIndicator != null)
                {
                    loadingIndicator.IsVisible = false;
                    loadingIndicator.IsRunning = false;
                }
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage(_currentUser, _dbService, _settingsService));
            Navigation.RemovePage(this);
        }

        protected override bool OnBackButtonPressed()
        {
            OnCancelClicked(null, null);
            return true;
        }
    }
}