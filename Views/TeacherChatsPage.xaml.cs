using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class TeacherChatsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;
        private Dictionary<int, int> _unreadMessagesCount = new();

        public ObservableCollection<TeacherGroupChatItem> Groups { get; } = new();

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

        public TeacherChatsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            BindingContext = this;

            UpdateTexts();
        }

        private void UpdateTexts()
        {
            Title = _localizationService.GetText("TeacherChats") ?? "Чаты групп";

            var headerLabel = this.FindByName<Label>("HeaderLabel");
            if (headerLabel != null)
                headerLabel.Text = _localizationService.GetText("TeacherChats") ?? "👨‍🏫 Чаты групп";

            var manageButton = this.FindByName<Button>("ManageGroupsButton");
            if (manageButton != null)
                manageButton.Text = _localizationService.GetText("ManageGroups") ?? "⚙️ Управление";

            var loadingLabel = this.FindByName<Label>("LoadingLabel");
            if (loadingLabel != null)
                loadingLabel.Text = _localizationService.GetText("LoadingChats") ?? "Загрузка чатов...";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadGroups();
        }

        private async void LoadGroups()
        {
            try
            {
                ShowLoading(true);

                var groups = await _dbService.GetTeacherStudyGroupsAsync(_currentUser.UserId);
                var unreadCounts = await _dbService.GetTeacherUnreadMessagesCountAsync(_currentUser.UserId);

                var uniqueGroups = groups
                    .GroupBy(g => g.GroupId)
                    .Select(g => g.First())
                    .ToList();

                var tempList = new List<TeacherGroupChatItem>();

                foreach (var group in uniqueGroups)
                {
                    var groupItem = new TeacherGroupChatItem
                    {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName,
                        CourseName = group.CourseName ?? "Без названия",
                        StudentCount = group.StudentCount,
                        IsActive = group.IsActive,
                        GroupAvatarUrl = await GetGroupAvatarUrl(group.GroupId)
                    };

                    if (unreadCounts.ContainsKey(group.GroupId))
                        groupItem.UnreadMessages = unreadCounts[group.GroupId];

                    try
                    {
                        var messages = await _dbService.GetGroupChatMessagesAsync(group.GroupId, 50);

                        if (messages != null && messages.Any())
                        {
                            var lastMsg = messages
                                .OrderByDescending(m => m.SentAt)
                                .First();

                            groupItem.LastMessage = lastMsg.DisplayText;
                            groupItem.LastMessageTime = lastMsg.SentAt;
                        }
                        else
                        {
                            groupItem.LastMessage = "Нет сообщений";
                            groupItem.LastMessageTime = DateTime.MinValue;
                        }
                    }
                    catch
                    {
                        groupItem.LastMessage = "Ошибка загрузки";
                        groupItem.LastMessageTime = DateTime.MinValue;
                    }

                    tempList.Add(groupItem);
                }

                var sorted = tempList
                    .OrderByDescending(g => g.LastMessageTime)
                    .ToList();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Groups.Clear();
                    foreach (var g in sorted)
                        Groups.Add(g);
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                ShowLoading(false);
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

        private async Task<string> GetGroupAvatarUrl(int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = "SELECT AvatarUrl FROM StudyGroups WHERE GroupId = @GroupId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);

                var result = await command.ExecuteScalarAsync();

                if (result != null && !string.IsNullOrEmpty(result.ToString()))
                {
                    return result.ToString();
                }

                // Проверяем наличие файла в локальной папке
                string localPath = Path.Combine(FileSystem.AppDataDirectory, "GroupAvatars", $"group_{groupId}.png");
                if (File.Exists(localPath))
                {
                    return localPath;
                }

                return "default_group.png"; // Аватар по умолчанию для групп
            }
            catch
            {
                return "default_group.png";
            }
        }

        private async void OnGroupSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is TeacherGroupChatItem selectedGroup)
            {
                try
                {
                    var group = new StudyGroup
                    {
                        GroupId = selectedGroup.GroupId,
                        GroupName = selectedGroup.GroupName,
                        StudentCount = selectedGroup.StudentCount,
                        IsActive = selectedGroup.IsActive
                    };

                    await Navigation.PushAsync(new GroupChatPage(group, _currentUser, _dbService, _settingsService));
                }
                catch (Exception ex)
                {
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        ex.Message, _localizationService.GetText("OK") ?? "OK");
                }
            }

            ((CollectionView)sender).SelectedItem = null;
        }

        private async void OnManageGroupsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TeacherGroupsManagementPage(_currentUser, _dbService, _settingsService));
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TeacherGroupChatItem : INotifyPropertyChanged
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public bool IsActive { get; set; }
        public string GroupAvatarUrl { get; set; } = "default_group.png";

        private int _unreadMessages;
        public int UnreadMessages
        {
            get => _unreadMessages;
            set
            {
                _unreadMessages = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasUnreadMessages));
            }
        }

        private string _lastMessage = string.Empty;
        public string LastMessage
        {
            get => _lastMessage;
            set
            {
                _lastMessage = value;
                OnPropertyChanged();
            }
        }

        private DateTime _lastMessageTime = DateTime.Now;
        public DateTime LastMessageTime
        {
            get => _lastMessageTime;
            set
            {
                _lastMessageTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastMessageTimeDisplay));
            }
        }

        public bool HasUnreadMessages => UnreadMessages > 0;

        public string LastMessageTimeDisplay
        {
            get
            {
                var diff = DateTime.Now - _lastMessageTime;
                if (diff.TotalMinutes < 1) return "только что";
                if (diff.TotalMinutes < 60) return $"{diff.Minutes} мин";
                if (diff.TotalHours < 24) return $"{diff.Hours} ч";
                if (diff.TotalDays < 7) return $"{diff.Days} дн";
                return _lastMessageTime.ToString("dd.MM.yy");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}