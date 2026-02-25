using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class StudentChatsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;

        private bool _isLoading;

        public ObservableCollection<StudentChatItem> Chats { get; } = new();

        private string _title;
        public new string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public StudentChatsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            BindingContext = this;

            UpdateTexts();
            // ❌ УБРАН LoadChats() из конструктора
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (!_isLoading)
                LoadChats();
        }

        private void UpdateTexts()
        {
            Title = _localizationService.GetText("MyChats") ?? "Мои чаты";

            var headerLabel = this.FindByName<Label>("HeaderLabel");
            if (headerLabel != null)
                headerLabel.Text = _localizationService.GetText("MyChats") ?? "💬 Мои чаты";

            var myCoursesButton = this.FindByName<Button>("MyCoursesButton");
            if (myCoursesButton != null)
                myCoursesButton.Text = _localizationService.GetText("MyCourses") ?? "📚 Мои курсы";

            var loadingLabel = this.FindByName<Label>("LoadingLabel");
            if (loadingLabel != null)
                loadingLabel.Text = _localizationService.GetText("LoadingChats") ?? "Загрузка чатов...";
        }

        private async void LoadChats()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                ShowLoading(true);

                var chats = await _dbService.GetStudentAllChatsAsync(_currentUser.UserId);
                var unreadCounts = await _dbService.GetStudentUnreadMessagesCountAsync(_currentUser.UserId);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Chats.Clear();

                    var uniqueChats = chats
                        .GroupBy(c => new { c.ChatId, c.ChatType })
                        .Select(g => g.First())
                        .OrderByDescending(c => c.LastMessageTime)
                        .ToList();

                    foreach (var chat in uniqueChats)
                    {
                        var chatItem = new StudentChatItem
                        {
                            ChatId = chat.ChatId,
                            ChatName = chat.ChatName,
                            ChatType = chat.ChatType,
                            Description = chat.Description,
                            GroupId = chat.GroupId,
                            TeacherId = chat.TeacherId,
                            CourseName = chat.CourseName,
                            ParticipantCount = chat.ParticipantCount,
                            LastMessage = chat.LastMessage,
                            LastMessageTime = chat.LastMessageTime,
                            Avatar = chat.Avatar ?? "default_avatar.png"
                        };

                        var key = (chat.ChatId, chat.ChatType);
                        if (unreadCounts.ContainsKey(key))
                            chatItem.UnreadMessages = unreadCounts[key];

                        Chats.Add(chatItem);
                    }
                });

                await LoadEmojisAndFrames();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService.GetText("Error") ?? "Ошибка",
                    $"{_localizationService.GetText("FailedToLoadChats") ?? "Не удалось загрузить чаты"}: {ex.Message}",
                    _localizationService.GetText("OK") ?? "OK");
            }
            finally
            {
                ShowLoading(false);
                _isLoading = false;
            }
        }

        private void ShowLoading(bool show)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                var loadingIndicator = this.FindByName<ActivityIndicator>("LoadingIndicator");

                if (loadingOverlay != null)
                    loadingOverlay.IsVisible = show;

                if (loadingIndicator != null)
                {
                    loadingIndicator.IsVisible = show;
                    loadingIndicator.IsRunning = show;
                }
            });
        }

        private async Task LoadEmojisAndFrames()
        {
            foreach (var chat in Chats.ToList())
            {
                try
                {
                    if (!chat.TeacherId.HasValue)
                        continue;

                    var equipped = await _dbService.GetEquippedItemsAsync(chat.TeacherId.Value);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var original = Chats.FirstOrDefault(c =>
                            c.ChatId == chat.ChatId &&
                            c.ChatType == chat.ChatType);

                        if (original != null)
                        {
                            original.UserEmoji = equipped.EmojiIcon;
                            original.FrameColor = equipped.FrameColor ?? "#457b9d";
                            original.IsOnline = new Random().Next(2) == 1;
                        }
                    });

                    await Task.Delay(30);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки экипировки: {ex.Message}");
                }
            }
        }

        private async void OnChatSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not StudentChatItem selectedChat)
                return;

            try
            {
                if (selectedChat.ChatType == "group" && selectedChat.GroupId.HasValue)
                {
                    var group = await _dbService.GetStudyGroupByIdAsync(selectedChat.GroupId.Value);
                    if (group != null)
                    {
                        await Navigation.PushAsync(
                            new GroupChatPage(group, _currentUser, _dbService, _settingsService));
                    }
                }
                else if (selectedChat.ChatType == "teacher" && selectedChat.TeacherId.HasValue)
                {
                    var teacher = await _dbService.GetUserByIdAsync(selectedChat.TeacherId.Value);
                    if (teacher != null)
                    {
                        var equipped = await _dbService.GetEquippedItemsAsync(teacher.UserId);
                        teacher.UserEmoji = equipped.EmojiIcon;
                        teacher.FrameColor = equipped.FrameColor;

                        await Navigation.PushAsync(
                            new ChatPage(_currentUser, teacher, _dbService, _settingsService));
                    }
                }
                else if (selectedChat.ChatType == "support")
                {
                    var supportUser = new User
                    {
                        UserId = 0,
                        FirstName = _localizationService.GetText("Support") ?? "Поддержка",
                        AvatarUrl = "support_icon.png"
                    };

                    await Navigation.PushAsync(
                        new ChatPage(_currentUser, supportUser, _dbService, _settingsService));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService.GetText("Error") ?? "Ошибка",
                    ex.Message,
                    _localizationService.GetText("OK") ?? "OK");
            }

            ((CollectionView)sender).SelectedItem = null;
        }

        private async void OnMyCoursesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new MyCoursesPage(_currentUser, _dbService, _settingsService));
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}