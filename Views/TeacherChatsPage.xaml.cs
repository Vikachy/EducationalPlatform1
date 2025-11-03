using System.Collections.ObjectModel;
using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace EducationalPlatform.Views
{
    public partial class TeacherChatsPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        public ObservableCollection<TeacherGroupChat> Groups { get; set; }

        // Правая панель чата (встроенная)
        private StudyGroup? _activeGroup;
        private readonly ObservableCollection<GroupChatMessage> _messages = new();
        private Timer? _refreshTimer;

        public TeacherChatsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            Groups = new ObservableCollection<TeacherGroupChat>();
            BindingContext = this;

            // Добавляем конвертеры
            Resources.Add("StatusConverter", new StatusTextConverter());
            Resources.Add("StatusColorConverter", new StatusColorConverter());

            LoadTeacherGroups();

            // Привязываем сообщения к CollectionView
            var messagesCollection = this.FindByName<CollectionView>("MessagesCollectionView");
            if (messagesCollection != null)
            {
                messagesCollection.ItemsSource = _messages;
            }
        }

        private async void OnManageGroupsClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new TeacherGroupsManagementPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть управление группами: {ex.Message}", "OK");
            }
        }

        private async void LoadTeacherGroups()
        {
            try
            {
                var groups = await _dbService.GetTeacherStudyGroupsAsync(_currentUser.UserId);
                Groups.Clear();
                foreach (var group in groups)
                {
                    // Получаем название курса для группы
                    var courseName = await GetCourseNameForGroup(group.GroupId);

                    Groups.Add(new TeacherGroupChat
                    {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName,
                        CourseName = courseName ?? "Без курса",
                        StudentCount = group.StudentCount,
                        IsActive = group.IsActive
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить группы: {ex.Message}", "OK");
            }
        }

        private async Task<string?> GetCourseNameForGroup(int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT c.CourseName 
                    FROM StudyGroups sg
                    JOIN Courses c ON sg.CourseId = c.CourseId
                    WHERE sg.GroupId = @GroupId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);

                var result = await command.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private async void OnGroupSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is TeacherGroupChat group)
            {
                try
                {
                    // Устанавливаем активную группу и подгружаем сообщения в правой панели
                    _activeGroup = new StudyGroup { GroupId = group.GroupId, GroupName = group.GroupName, StudentCount = group.StudentCount, IsActive = group.IsActive };

                    var header = this.FindByName<Label>("ChatGroupNameLabel");
                    var online = this.FindByName<Label>("ChatOnlineLabel");
                    if (header != null) header.Text = group.GroupName;
                    if (online != null) online.Text = $"Онлайн: {group.StudentCount}";

                    _messages.Clear();
                    await LoadMessages();

                    _refreshTimer?.Dispose();
                    _refreshTimer = new Timer(async _ => await RefreshMessages(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть чат: {ex.Message}", "OK");
                }
            }
            ((CollectionView)sender).SelectedItem = null;
        }

        private async Task LoadMessages()
        {
            if (_activeGroup == null) return;
            try
            {
                var list = await _dbService.GetGroupChatMessagesAsync(_activeGroup.GroupId);
                _messages.Clear();
                foreach (var m in list)
                {
                    m.IsMyMessage = m.SenderId == _currentUser.UserId;
                    _messages.Add(m);
                }
                var messagesCollection = this.FindByName<CollectionView>("MessagesCollectionView");
                if (messagesCollection != null && _messages.Count > 0)
                {
                    messagesCollection.ScrollTo(_messages[^1], position: ScrollToPosition.End, animate: true);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка загрузки сообщений: {ex.Message}", "OK");
            }
        }

        private async Task RefreshMessages()
        {
            if (_activeGroup == null) return;
            try
            {
                var list = await _dbService.GetGroupChatMessagesAsync(_activeGroup.GroupId);
                var newOnes = list.Where(m => !_messages.Any(x => x.MessageId == m.MessageId)).ToList();
                if (newOnes.Count == 0) return;
                foreach (var m in newOnes)
                {
                    m.IsMyMessage = m.SenderId == _currentUser.UserId;
                    _messages.Add(m);
                }
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var messagesCollection = this.FindByName<CollectionView>("MessagesCollectionView");
                    if (messagesCollection != null)
                        messagesCollection.ScrollTo(_messages[^1], position: ScrollToPosition.End, animate: true);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления сообщений: {ex.Message}");
            }
        }

        private async void OnSendMessageClicked(object sender, EventArgs e)
        {
            await SendMessage();
        }

        private async void OnMessageSent(object sender, EventArgs e)
        {
            await SendMessage();
        }

        private async Task SendMessage()
        {
            if (_activeGroup == null) return;
            var entry = this.FindByName<Entry>("MessageEntry");
            var text = entry?.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                var ok = await _dbService.SendGroupChatMessageAsync(_activeGroup.GroupId, _currentUser.UserId, text);
                if (ok)
                {
                    if (entry != null) entry.Text = string.Empty;
                    await LoadMessages();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось отправить сообщение", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка отправки: {ex.Message}", "OK");
            }
        }

        private async void OnAttachFileClicked(object sender, EventArgs e)
        {
            if (_activeGroup == null) return;
            try
            {
                var result = await FilePicker.Default.PickAsync();
                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    var fileService = ServiceHelper.GetService<FileService>();
                    var unique = fileService.GenerateUniqueFileName(result.FileName);
                    var saved = await fileService.SaveDocumentAsync(stream, unique);
                    var ok = await _dbService.SendGroupChatMessageAsync(_activeGroup.GroupId, _currentUser.UserId, $"[file] {saved}");
                    if (ok)
                    {
                        await DisplayAlert("Файл", "Файл отправлен", "OK");
                        await LoadMessages();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка отправки файла: {ex.Message}", "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _refreshTimer?.Dispose();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class TeacherGroupChat
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class StatusTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool active && active ? "Активна" : "Неактивна";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool active && active ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}