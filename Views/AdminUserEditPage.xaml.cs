using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Data.SqlClient;
using Dapper;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class AdminUserEditPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly AdminUserModel? _editingUser;

        private Entry? _usernameEntry;
        private Entry? _emailEntry;
        private Entry? _firstNameEntry;
        private Entry? _lastNameEntry;
        private Entry? _passwordEntry;
        private Picker? _rolePicker;
        private Switch? _isActiveSwitch;
        private Image? _avatarImage;
        private Label? _titleLabel;
        private Label? _passwordLabel;
        private Border? _passwordBorder;
        private Button? _deleteButton;
        private Button? _deleteButton2;
        private Grid? _loadingOverlay;
        private ActivityIndicator? _loadingIndicator;
        private Label? _loadingLabel;

        private byte[]? _selectedAvatarBytes;
        private string? _selectedAvatarFileName;

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminUserEditPage(User user, DatabaseService dbService, SettingsService settingsService, AdminUserModel? existingUser = null)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminUserEditPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _editingUser = existingUser;

            InitializeControls();

            if (existingUser != null)
            {
                if (_titleLabel != null) _titleLabel.Text = "Редактирование пользователя";
                // Показываем кнопки удаления
                if (_deleteButton != null) _deleteButton.IsVisible = true;
                if (_deleteButton2 != null) _deleteButton2.IsVisible = true;
                // Скрываем поля пароля для существующего пользователя
                if (_passwordLabel != null) _passwordLabel.IsVisible = false;
                if (_passwordBorder != null) _passwordBorder.IsVisible = false;
                LoadUserData();
            }
            else
            {
                if (_titleLabel != null) _titleLabel.Text = "Создание пользователя";
                // Скрываем кнопки удаления
                if (_deleteButton != null) _deleteButton.IsVisible = false;
                if (_deleteButton2 != null) _deleteButton2.IsVisible = false;
                // Показываем поля пароля для нового пользователя
                if (_passwordLabel != null) _passwordLabel.IsVisible = true;
                if (_passwordBorder != null) _passwordBorder.IsVisible = true;
            }
        }

        private void InitializeControls()
        {
            _loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
            _loadingIndicator = this.FindByName<ActivityIndicator>("LoadingIndicator");
            _loadingLabel = this.FindByName<Label>("LoadingLabel");
            _titleLabel = this.FindByName<Label>("TitleLabel");
            _usernameEntry = this.FindByName<Entry>("UsernameEntry");
            _emailEntry = this.FindByName<Entry>("EmailEntry");
            _firstNameEntry = this.FindByName<Entry>("FirstNameEntry");
            _lastNameEntry = this.FindByName<Entry>("LastNameEntry");
            _passwordEntry = this.FindByName<Entry>("PasswordEntry");
            _passwordLabel = this.FindByName<Label>("PasswordLabel");
            _passwordBorder = this.FindByName<Border>("PasswordBorder");
            _rolePicker = this.FindByName<Picker>("RolePicker");
            _isActiveSwitch = this.FindByName<Switch>("IsActiveSwitch");
            _avatarImage = this.FindByName<Image>("AvatarImage");
            _deleteButton = this.FindByName<Button>("DeleteButton");
            _deleteButton2 = this.FindByName<Button>("DeleteButton2");

            if (_rolePicker != null)
                _rolePicker.SelectedIndex = 0;
        }

        private void ShowLoading(bool show, string message = "Загрузка...")
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_loadingOverlay != null)
                {
                    _loadingOverlay.IsVisible = show;
                    _loadingOverlay.InputTransparent = !show;
                }
                if (_loadingIndicator != null)
                    _loadingIndicator.IsRunning = show;
                if (_loadingLabel != null && !string.IsNullOrEmpty(message))
                    _loadingLabel.Text = message;
            });
        }

        private void LoadUserData()
        {
            if (_editingUser == null) return;

            if (_usernameEntry != null) _usernameEntry.Text = _editingUser.Username;
            if (_emailEntry != null) _emailEntry.Text = _editingUser.Email;
            if (_firstNameEntry != null) _firstNameEntry.Text = _editingUser.FirstName;
            if (_lastNameEntry != null) _lastNameEntry.Text = _editingUser.LastName;
            if (_isActiveSwitch != null) _isActiveSwitch.IsToggled = _editingUser.IsActive;

            // Правильная установка роли (RoleId от 1 до 4)
            if (_rolePicker != null)
            {
                // RoleId: 1-Студент, 2-Преподаватель, 3-Админ, 4-Контент-менеджер
                _rolePicker.SelectedIndex = _editingUser.RoleId - 1;
                Console.WriteLine($"📌 Загружена роль: {_editingUser.RoleId} -> индекс {_editingUser.RoleId - 1}");
            }

            if (_avatarImage != null)
            {
                if (!string.IsNullOrEmpty(_editingUser.AvatarUrl))
                {
                    try
                    {
                        _avatarImage.Source = ImageSource.FromUri(new Uri(_editingUser.AvatarUrl));
                    }
                    catch
                    {
                        _avatarImage.Source = "default_avatar.png";
                    }
                }
                else
                {
                    _avatarImage.Source = "default_avatar.png";
                }
            }
        }

        private async void OnSelectAvatarClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите аватар",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" } },
                        { DevicePlatform.Android, new[] { "image/jpeg", "image/png", "image/gif", "image/bmp" } },
                        { DevicePlatform.iOS, new[] { "public.image" } },
                    })
                });

                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    _selectedAvatarBytes = memoryStream.ToArray();
                    _selectedAvatarFileName = result.FileName;

                    // Отображаем выбранный аватар
                    if (_avatarImage != null)
                    {
                        _avatarImage.Source = ImageSource.FromStream(() => new MemoryStream(_selectedAvatarBytes));
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выбрать аватар: {ex.Message}", "OK");
            }
        }

        private void OnRemoveAvatarClicked(object sender, EventArgs e)
        {
            _selectedAvatarBytes = null;
            _selectedAvatarFileName = null;
            if (_avatarImage != null)
            {
                _avatarImage.Source = "default_avatar.png";
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                ShowLoading(true, "Сохранение...");

                // Валидация
                if (string.IsNullOrWhiteSpace(_usernameEntry?.Text))
                {
                    await DisplayAlert("Ошибка", "Введите логин", "OK");
                    ShowLoading(false);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_emailEntry?.Text) || !_emailEntry.Text.Contains("@"))
                {
                    await DisplayAlert("Ошибка", "Введите корректный email", "OK");
                    ShowLoading(false);
                    return;
                }

                if (_rolePicker?.SelectedIndex == -1)
                {
                    await DisplayAlert("Ошибка", "Выберите роль", "OK");
                    ShowLoading(false);
                    return;
                }

                // Для нового пользователя проверяем пароль
                if (_editingUser == null && string.IsNullOrWhiteSpace(_passwordEntry?.Text))
                {
                    await DisplayAlert("Ошибка", "Введите пароль", "OK");
                    ShowLoading(false);
                    return;
                }

                // SelectedIndex: 0-Студент, 1-Преподаватель, 2-Админ, 3-Контент-менеджер
                // RoleId: 1-Студент, 2-Преподаватель, 3-Админ, 4-Контент-менеджер
                int roleId = _rolePicker.SelectedIndex + 1;

                Console.WriteLine($"📌 Выбрана роль: индекс {_rolePicker.SelectedIndex} -> RoleId {roleId}");

                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                if (_editingUser == null)
                {
                    // Создание нового пользователя
                    string password = _passwordEntry?.Text ?? GenerateRandomPassword();
                    string hashedPassword = HashPassword(password);

                    // Вставляем пользователя
                    var userId = await connection.ExecuteScalarAsync<int>(@"
                        INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, RoleId, IsActive, RegistrationDate)
                        VALUES (@Username, @Email, @PasswordHash, @FirstName, @LastName, @RoleId, @IsActive, GETDATE());
                        SELECT SCOPE_IDENTITY();
                    ", new
                    {
                        Username = _usernameEntry.Text.Trim(),
                        Email = _emailEntry.Text.Trim(),
                        PasswordHash = hashedPassword,
                        FirstName = _firstNameEntry?.Text?.Trim() ?? "",
                        LastName = _lastNameEntry?.Text?.Trim() ?? "",
                        RoleId = roleId,
                        IsActive = _isActiveSwitch?.IsToggled ?? true
                    });

                    // Если есть аватар, сохраняем его
                    if (_selectedAvatarBytes != null && userId > 0)
                    {
                        await SaveUserAvatarAsync(userId, _selectedAvatarBytes, _selectedAvatarFileName ?? "avatar.jpg");
                    }

                    await DisplayAlert("Успех", $"Пользователь создан.\nПароль: {password}", "OK");
                }
                else
                {
                    // Обновление существующего пользователя
                    await connection.ExecuteAsync(@"
                        UPDATE Users SET 
                            Username = @Username,
                            Email = @Email,
                            FirstName = @FirstName,
                            LastName = @LastName,
                            RoleId = @RoleId,
                            IsActive = @IsActive
                        WHERE UserId = @UserId
                    ", new
                    {
                        UserId = _editingUser.UserId,
                        Username = _usernameEntry?.Text?.Trim(),
                        Email = _emailEntry?.Text?.Trim(),
                        FirstName = _firstNameEntry?.Text?.Trim() ?? "",
                        LastName = _lastNameEntry?.Text?.Trim() ?? "",
                        RoleId = roleId,
                        IsActive = _isActiveSwitch?.IsToggled ?? true
                    });

                    // Если есть новый аватар, сохраняем его
                    if (_selectedAvatarBytes != null)
                    {
                        await SaveUserAvatarAsync(_editingUser.UserId, _selectedAvatarBytes, _selectedAvatarFileName ?? "avatar.jpg");
                    }

                    await DisplayAlert("Успех", "Пользователь обновлен", "OK");
                }

                ShowLoading(false);
                await Navigation.PopAsync();
            }
            catch (SqlException ex)
            {
                ShowLoading(false);
                await DisplayAlert("Ошибка базы данных", $"Код ошибки: {ex.Number}\n{ex.Message}", "OK");
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnDeleteUserClicked(object sender, EventArgs e)
        {
            if (_editingUser == null) return;

            bool confirm = await DisplayAlert("Подтверждение",
                $"Вы уверены, что хотите УДАЛИТЬ пользователя {_editingUser.Email}?\n\n" +
                $"Имя: {_editingUser.FullName}\n" +
                $"Логин: {_editingUser.Username}\n\n" +
                $"Это действие необратимо! Все связанные данные будут удалены.",
                "Да, удалить", "Отмена");

            if (confirm)
            {
                try
                {
                    ShowLoading(true, "Удаление...");

                    using var connection = new SqlConnection(_dbService.ConnectionString);
                    await connection.OpenAsync();

                    // Удаляем пользователя
                    int deleted = await connection.ExecuteAsync("DELETE FROM Users WHERE UserId = @UserId",
                        new { UserId = _editingUser.UserId });

                    if (deleted > 0)
                    {
                        ShowLoading(false);
                        await DisplayAlert("Успех", "Пользователь удален", "OK");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        ShowLoading(false);
                        await DisplayAlert("Ошибка", "Пользователь не найден", "OK");
                    }
                }
                catch (SqlException ex)
                {
                    ShowLoading(false);
                    if (ex.Number == 547) // Foreign key violation
                    {
                        await DisplayAlert("Ошибка",
                            "Невозможно удалить пользователя, так как он связан с другими записями.\n" +
                            "Сначала удалите все связанные данные (курсы, группы, ответы).", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка базы данных", ex.Message, "OK");
                    }
                }
                catch (Exception ex)
                {
                    ShowLoading(false);
                    await DisplayAlert("Ошибка", ex.Message, "OK");
                }
            }
        }

        private async Task SaveUserAvatarAsync(int userId, byte[] imageBytes, string fileName)
        {
            try
            {
                // Сохраняем аватар в локальную папку
                string avatarsFolder = Path.Combine(FileSystem.AppDataDirectory, "Avatars");
                if (!Directory.Exists(avatarsFolder))
                    Directory.CreateDirectory(avatarsFolder);

                string fileExtension = Path.GetExtension(fileName);
                string newFileName = $"avatar_{userId}{fileExtension}";
                string filePath = Path.Combine(avatarsFolder, newFileName);

                await File.WriteAllBytesAsync(filePath, imageBytes);

                // Обновляем путь в БД
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "UPDATE Users SET AvatarUrl = @AvatarUrl WHERE UserId = @UserId",
                    new { UserId = userId, AvatarUrl = filePath });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения аватара: {ex.Message}");
            }
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}