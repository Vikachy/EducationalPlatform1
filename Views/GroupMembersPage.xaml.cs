using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class GroupMembersPage : ContentPage, INotifyPropertyChanged
    {
        private readonly int _groupId;
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;

        public ObservableCollection<GroupMember> Members { get; } = new();

        public GroupMembersPage(int groupId, User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _groupId = groupId;
            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            BindingContext = this;

            LoadMembers();

            // Добавляем кнопку для добавления участников (только для учителей)
            if (_currentUser.RoleId == 2 || _currentUser.RoleId == 3 || _currentUser.RoleId == 4)
            {
                AddAddMemberButton();
            }
        }

        private void AddAddMemberButton()
        {
            var addButton = new Button
            {
                Text = _localizationService?.GetText("AddMember") ?? "➕ Добавить участника",
                BackgroundColor = Color.FromArgb("#4CAF50"),
                TextColor = Colors.White,
                CornerRadius = 20,
                HeightRequest = 45,
                Margin = new Thickness(20, 10, 20, 10)
            };
            addButton.Clicked += OnAddMemberClicked;

            var layout = this.FindByName<VerticalStackLayout>("MainLayout");
            if (layout != null)
            {
                layout.Children.Add(addButton);
            }
        }

        private async void OnAddMemberClicked(object sender, EventArgs e)
        {
            try
            {
                var username = await DisplayPromptAsync(
                    _localizationService?.GetText("AddMember") ?? "Добавить участника",
                    _localizationService?.GetText("EnterUsername") ?? "Введите логин пользователя:");

                if (!string.IsNullOrWhiteSpace(username))
                {
                    var user = await _dbService.GetUserByUsernameAsync(username.Trim());
                    if (user != null)
                    {
                        // Добавляем в группу
                        await _dbService.EnrollStudentToGroupAsync(_groupId, user.UserId);

                        // Добавляем в чат
                        await _dbService.SimpleAddToGroupChat(_groupId, user.UserId);

                        // Отправляем системное сообщение
                        await _dbService.AddSystemMessageToGroupAsync(_groupId,
                            $"🎓 {user.FirstName} {user.LastName} присоединился к группе");

                        await DisplayAlert(_localizationService?.GetText("Success") ?? "Успех",
                            _localizationService?.GetText("MemberAdded") ?? "Участник добавлен",
                            _localizationService?.GetText("OK") ?? "OK");

                        LoadMembers();
                    }
                    else
                    {
                        await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                            _localizationService?.GetText("UserNotFound") ?? "Пользователь не найден",
                            _localizationService?.GetText("OK") ?? "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                    ex.Message, _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void LoadMembers()
        {
            try
            {
                var members = await _dbService.GetGroupChatMembersAsync(_groupId);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Members.Clear();

                    foreach (var member in members)
                    {
                        var groupMember = new GroupMember
                        {
                            UserId = member.User.UserId,
                            FirstName = member.User.FirstName,
                            LastName = member.User.LastName,
                            Username = member.User.Username,
                            Email = member.User.Email,
                            AvatarUrl = member.User.AvatarUrl ?? "default_avatar.png",
                            RoleId = member.User.RoleId,
                            RoleName = GetRoleName(member.User.RoleId),
                            IsOnline = new Random().Next(2) == 1,
                            JoinedDate = member.JoinedDate
                        };

                        Members.Add(groupMember);
                    }

                    // Загружаем эмодзи и рамки
                    LoadEmojisAndFrames();

                    var countLabel = this.FindByName<Label>("CountLabel");
                    if (countLabel != null)
                        countLabel.Text = Members.Count.ToString();
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                    ex.Message, _localizationService.GetText("OK") ?? "OK");
            }
        }

        private async void LoadEmojisAndFrames()
        {
            foreach (var member in Members.ToList())
            {
                try
                {
                    var equipped = await _dbService.GetEquippedItemsAsync(member.UserId);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var originalMember = Members.FirstOrDefault(m => m.UserId == member.UserId);
                        if (originalMember != null)
                        {
                            originalMember.UserEmoji = equipped.EmojiIcon;
                            originalMember.FrameColor = equipped.FrameColor ?? "#457b9d";
                        }
                    });

                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки экипировки: {ex.Message}");
                }
            }
        }

        private string GetRoleName(int roleId)
        {
            return roleId switch
            {
                1 => _localizationService.GetText("Student") ?? "Студент",
                2 => _localizationService.GetText("Teacher") ?? "Преподаватель",
                3 => _localizationService.GetText("Admin") ?? "Администратор",
                4 => _localizationService.GetText("ContentManager") ?? "Контент-менеджер",
                _ => _localizationService.GetText("User") ?? "Пользователь"
            };
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GroupMember : User, INotifyPropertyChanged
    {
        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set
            {
                _isOnline = value;
                OnPropertyChanged();
            }
        }

        private string? _userEmoji;
        public string? UserEmoji
        {
            get => _userEmoji;
            set
            {
                _userEmoji = value;
                OnPropertyChanged();
            }
        }

        private string? _frameColor;
        public string? FrameColor
        {
            get => _frameColor;
            set
            {
                _frameColor = value;
                OnPropertyChanged();
            }
        }

        public DateTime JoinedDate { get; set; }
        public string DisplayName => $"{FirstName} {LastName}";
        public string RoleName { get; set; } = string.Empty;

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}