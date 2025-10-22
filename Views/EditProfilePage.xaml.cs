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

        private async void LoadUserData()
        {
            try
            {
                FirstNameEntry.Text = _currentUser.FirstName ?? "";
                LastNameEntry.Text = _currentUser.LastName ?? "";
                UsernameEntry.Text = _currentUser.Username ?? "";
                EmailEntry.Text = _currentUser.Email ?? "";

                // Загружаем актуальный аватар из базы
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
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные: {ex.Message}", "OK");
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

                    // Показываем превью ВО ВРЕМЯ ВЫБОРА
                    AvatarPreview.Source = ImageSource.FromFile(result.FullPath);

                    Console.WriteLine($"Выбран файл: {result.FileName}, путь: {result.FullPath}");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выбрать изображение: {ex.Message}", "OK");
            }
        }

        private void OnRemoveImageClicked(object sender, EventArgs e)
        {
            try
            {
                _selectedImage = null;
                _avatarUrl = null;
                AvatarPreview.Source = "default_avatar.png";

                // Удаляем аватар из базы данных
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

                DisplayAlert("Успех", "Аватар удален!", "OK");
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", $"Не удалось удалить аватар: {ex.Message}", "OK");
            }
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
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            try
            {
                string finalAvatarUrl = _avatarUrl;

                // Загружаем новое изображение если выбрано
                if (_selectedImage != null)
                {
                    Console.WriteLine($"Начинаем загрузку аватара: {_selectedImage.FileName}");

                    using var stream = await _selectedImage.OpenReadAsync();
                    finalAvatarUrl = await _dbService.UploadAvatarAsync(stream, _selectedImage.FileName, _currentUser.UserId);

                    if (string.IsNullOrEmpty(finalAvatarUrl))
                    {
                        await DisplayAlert("Ошибка", "Не удалось загрузить аватар", "OK");
                        LoadingIndicator.IsVisible = false;
                        LoadingIndicator.IsRunning = false;
                        return;
                    }

                    Console.WriteLine($"Аватар успешно загружен: {finalAvatarUrl}");
                    await DisplayAlert("Успех", "Аватар успешно загружен!", "OK");
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
                    var profilePage = new ProfilePage(_currentUser, _dbService, _settingsService);
                    await Navigation.PushAsync(profilePage);

                    // Удаляем текущую страницу из стека навигации
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
                    await DisplayAlert("Ошибка", "Не удалось обновить профиль", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось сохранить изменения: {ex.Message}", "OK");
                Console.WriteLine($"Ошибка сохранения: {ex}");
            }
            finally
            {
                // Скрываем индикатор загрузки
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            try
            {
                // Возвращаемся на страницу профиля без сохранения
                var profilePage = new ProfilePage(_currentUser, _dbService, _settingsService);
                await Navigation.PushAsync(profilePage);

                // Удаляем текущую страницу из стека навигации
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
                await DisplayAlert("Ошибка", $"Не удалось вернуться: {ex.Message}", "OK");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // Блокируем стандартное поведение кнопки назад
            OnCancelClicked(null, null);
            return true;
        }

        // Обработчик изменения текста в полях для валидации в реальном времени
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

            // Можно добавить визуальную индикацию валидности формы
            if (isValid)
            {
                // Форма валидна
            }
            else
            {
                // Форма невалидна
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

        // Обработчик для проверки email при потере фокуса
        private async void OnEmailUnfocused(object sender, FocusEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(EmailEntry.Text) && !IsValidEmail(EmailEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите корректный email адрес", "OK");
                EmailEntry.Focus();
            }
        }

        // Обработчик для проверки username при потере фокуса
        private async void OnUsernameUnfocused(object sender, FocusEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UsernameEntry.Text))
            {
                // Проверяем минимальную длину username
                if (UsernameEntry.Text.Length < 3)
                {
                    await DisplayAlert("Ошибка", "Логин должен содержать минимум 3 символа", "OK");
                    UsernameEntry.Focus();
                    return;
                }

                // Проверяем уникальность username (кроме текущего пользователя)
                bool userExists = await _dbService.CheckUserExistsAsync(
                    UsernameEntry.Text,
                    "", // Пустой email для проверки только username
                    _currentUser.UserId);

                if (userExists)
                {
                    await DisplayAlert("Ошибка", "Пользователь с таким логином уже существует", "OK");
                    UsernameEntry.Focus();
                }
            }
        }
    }
}