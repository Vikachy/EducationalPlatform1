using System.Collections.ObjectModel;
using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Globalization;
using EducationalPlatform.Converters;
using Microsoft.Data.SqlClient;

namespace EducationalPlatform.Views
{
    public partial class TeacherChatsPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;

        public ObservableCollection<TeacherGroupChat> Groups { get; set; }

        public ObservableCollection<GroupChatMessage> Messages { get; set; }

        // Правая панель чата (встроенная)
        private StudyGroup? _activeGroup;
        private readonly ObservableCollection<GroupChatMessage> _messages = new();
        private Timer? _refreshTimer;

        // Поддерживаемые форматы файлов
        private readonly FilePickerFileType _supportedFileTypes = new(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".zip", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".txt", ".xls", ".xlsx", ".jpg", ".png", ".mp4" } },
                { DevicePlatform.macOS, new[] { ".zip", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".txt", ".xls", ".xlsx", ".jpg", ".png", ".mp4" } }
            });

        public TeacherChatsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _fileService = new FileService();

            Groups = new ObservableCollection<TeacherGroupChat>();
            BindingContext = this;

            LoadTeacherGroups();

            // Привязываем сообщения к CollectionView
            var messagesCollection = this.FindByName<CollectionView>("MessagesCollectionView");
            if (messagesCollection != null)
            {
                messagesCollection.ItemsSource = _messages;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadTeacherGroups();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _refreshTimer?.Dispose();
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
                    _activeGroup = new StudyGroup
                    {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName,
                        StudentCount = group.StudentCount,
                        IsActive = group.IsActive
                    };

                    var header = this.FindByName<Label>("ChatGroupNameLabel");
                    var online = this.FindByName<Label>("ChatOnlineLabel");
                    if (header != null) header.Text = group.GroupName;
                    if (online != null) online.Text = $"Онлайн: {group.StudentCount}";

                    _messages.Clear();
                    await LoadMessages();

                    _refreshTimer?.Dispose();
                    _refreshTimer = new Timer(async _ => await RefreshMessages(), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
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

                    // Загружаем аватар
                    if (m.IsMyMessage)
                    {
                        m.SenderAvatar = _currentUser.AvatarUrl ?? "default_avatar.png";
                    }
                    else if (string.IsNullOrEmpty(m.SenderAvatar))
                    {
                        m.SenderAvatar = "default_avatar.png";
                    }

                    // Парсим файловые сообщения
                    if (m.MessageText?.StartsWith("[FILE]") == true)
                    {
                        m.IsFileMessage = true;
                        var fileData = ParseFileMessage(m.MessageText);
                        m.FileName = fileData.FileName;
                        m.FileType = fileData.FileType;
                        m.FileSize = fileData.FileSize;
                    }

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

                    // Загружаем аватар
                    if (m.IsMyMessage)
                    {
                        m.SenderAvatar = _currentUser.AvatarUrl ?? "default_avatar.png";
                    }
                    else if (string.IsNullOrEmpty(m.SenderAvatar))
                    {
                        m.SenderAvatar = "default_avatar.png";
                    }

                    // Парсим файловые сообщения
                    if (m.MessageText?.StartsWith("[FILE]") == true)
                    {
                        m.IsFileMessage = true;
                        var fileData = ParseFileMessage(m.MessageText);
                        m.FileName = fileData.FileName;
                        m.FileType = fileData.FileType;
                        m.FileSize = fileData.FileSize;
                    }

                    _messages.Add(m);
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var messagesCollection = this.FindByName<CollectionView>("MessagesCollectionView");
                    if (messagesCollection != null && _messages.Count > 0)
                        messagesCollection.ScrollTo(_messages[^1], position: ScrollToPosition.End, animate: true);
                });

                await MarkMessagesAsRead();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления сообщений: {ex.Message}");
            }
        }

        private async Task MarkMessagesAsRead()
        {
            if (_activeGroup == null) return;

            try
            {
                await _dbService.MarkMessagesAsReadAsync(_activeGroup.GroupId, _currentUser.UserId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отметки сообщений: {ex.Message}");
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
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите файл для отправки",
                    FileTypes = _supportedFileTypes
                });

                if (result != null)
                {
                    await SendFileAsync(result);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось прикрепить файл: {ex.Message}", "OK");
            }
        }

        private async Task SendFileAsync(FileResult fileResult)
        {
            try
            {
                using var stream = await fileResult.OpenReadAsync();
                var uniqueFileName = _fileService.GenerateUniqueFileName(fileResult.FileName);
                var savedPath = await _fileService.SaveDocumentAsync(stream, uniqueFileName);

                if (!string.IsNullOrEmpty(savedPath))
                {
                    var fileInfo = new FileInfo(savedPath);
                    var fileSize = FormatFileSize(fileInfo.Length);

                    var message = $"[FILE]|{savedPath}|{fileResult.FileName}|{fileSize}|{Path.GetExtension(fileResult.FileName)}";

                    var ok = await _dbService.SendGroupChatMessageAsync(_activeGroup.GroupId, _currentUser.UserId, message);
                    if (ok)
                    {
                        await DisplayAlert("Успех", "Файл успешно отправлен", "OK");
                        await LoadMessages();
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось отправить файл", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка отправки файла: {ex.Message}", "OK");
            }
        }

        private (string FilePath, string FileName, string FileType, string FileSize) ParseFileMessage(string messageText)
        {
            try
            {
                var parts = messageText.Split('|');
                if (parts.Length >= 5 && parts[0] == "[FILE]")
                {
                    return (parts[1], parts[2], parts[4], parts[3]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга файлового сообщения: {ex.Message}");
            }

            return (string.Empty, "Неизвестный файл", "", "");
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes >= 1 << 30) return $"{(bytes / (1 << 30)):F1} GB";
            if (bytes >= 1 << 20) return $"{(bytes / (1 << 20)):F1} MB";
            if (bytes >= 1 << 10) return $"{(bytes / (1 << 10)):F1} KB";
            return $"{bytes} B";
        }

        private async void OnFileMessageTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is GroupChatMessage message && message.IsFileMessage)
            {
                try
                {
                    var fileData = ParseFileMessage(message.MessageText);

                    var action = await DisplayActionSheet(
                        $"Файл: {fileData.FileName}",
                        "Отмена",
                        null,
                        "📥 Скачать файл",
                        "📁 Открыть файл");

                    if (action == "📥 Скачать файл")
                    {
                        await DownloadFile(fileData.FilePath, fileData.FileName);
                    }
                    else if (action == "📁 Открыть файл")
                    {
                        await OpenFile(fileData.FilePath);
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось обработать файл: {ex.Message}", "OK");
                }
            }
        }

        private async Task DownloadFile(string filePath, string fileName)
        {
            try
            {
                var success = await _fileService.DownloadFileAsync(filePath, fileName);
                if (success)
                {
                    await DisplayAlert("Успех", $"Файл {fileName} скачан", "OK");
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось скачать файл", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка при скачивании: {ex.Message}", "OK");
            }
        }

        private async Task OpenFile(string filePath)
        {
            try
            {
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть файл: {ex.Message}", "OK");
            }
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

    

}