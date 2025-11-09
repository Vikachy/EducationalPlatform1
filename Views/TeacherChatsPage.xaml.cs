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

        // Правая панель чата (встроенная)
        private StudyGroup? _activeGroup;
        private readonly ObservableCollection<GroupChatMessage> _messages = new();
        private Timer? _refreshTimer;

        // Словарь для хранения непрочитанных сообщений
        private Dictionary<int, int> _unreadMessagesCount = new();

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
            MessagesCollectionView.ItemsSource = _messages;
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
                await DisplayAlert("Error", $"Failed to open group management: {ex.Message}", "OK");
            }
        }

        private async void LoadTeacherGroups()
        {
            try
            {
                // Загружаем группы и непрочитанные сообщения
                var groups = await _dbService.GetTeacherStudyGroupsAsync(_currentUser.UserId);
                _unreadMessagesCount = await _dbService.GetTeacherUnreadMessagesCountAsync(_currentUser.UserId);

                Groups.Clear();
                foreach (var group in groups)
                {
                    var courseName = await GetCourseNameForGroup(group.GroupId);
                    var unreadCount = _unreadMessagesCount.ContainsKey(group.GroupId) ? _unreadMessagesCount[group.GroupId] : 0;

                    Groups.Add(new TeacherGroupChat
                    {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName,
                        CourseName = courseName ?? "No Course",
                        StudentCount = group.StudentCount,
                        IsActive = group.IsActive,
                        UnreadMessages = unreadCount
                    });
                }

                Console.WriteLine($"📊 Загружено {Groups.Count} групп с непрочитанными сообщениями");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load groups: {ex.Message}", "OK");
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

                    ChatGroupNameLabel.Text = group.GroupName;
                    ChatOnlineLabel.Text = $"Online: {group.StudentCount}";

                    _messages.Clear();
                    await LoadMessages();

                    // Отмечаем сообщения как прочитанные при открытии чата
                    await MarkMessagesAsRead();

                    // Обновляем счетчик непрочитанных в UI
                    await UpdateUnreadCount(group.GroupId);

                    _refreshTimer?.Dispose();
                    _refreshTimer = new Timer(async _ => await RefreshMessages(), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to open chat: {ex.Message}", "OK");
                }
            }
            ((CollectionView)sender).SelectedItem = null;
        }

        private async Task UpdateUnreadCount(int groupId)
        {
            try
            {
                // Обновляем счетчик в локальном словаре
                if (_unreadMessagesCount.ContainsKey(groupId))
                {
                    _unreadMessagesCount[groupId] = 0;
                }

                // Обновляем отображение в списке групп
                var group = Groups.FirstOrDefault(g => g.GroupId == groupId);
                if (group != null)
                {
                    group.UnreadMessages = 0;

                    // Принудительно обновляем отображение
                    GroupsCollectionView.ItemsSource = null;
                    GroupsCollectionView.ItemsSource = Groups;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления счетчика: {ex.Message}");
            }
        }

        private async Task<string?> GetUserAvatarAsync(int userId)
        {
            try
            {
                if (_dbService != null)
                {
                    var avatarPath = await _dbService.GetUserAvatarAsync(userId);

                    if (!string.IsNullOrEmpty(avatarPath))
                    {
                        if (File.Exists(avatarPath))
                        {
                            Console.WriteLine($"✅ Аватар найден для пользователя {userId}: {avatarPath}");
                            return avatarPath;
                        }
                        else
                        {
                            var localPath = Path.Combine(FileSystem.AppDataDirectory, avatarPath);
                            if (File.Exists(localPath))
                            {
                                Console.WriteLine($"✅ Аватар найден по локальному пути: {localPath}");
                                return localPath;
                            }
                            else
                            {
                                Console.WriteLine($"❌ Аватар не найден по пути: {avatarPath}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️ Аватар не установлен для пользователя {userId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки аватара пользователя {userId}: {ex.Message}");
            }

            return "default_avatar.png";
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

                    // Загружаем аватар для ВСЕХ сообщений
                    if (string.IsNullOrEmpty(m.SenderAvatar) || m.SenderAvatar == "default_avatar.png")
                    {
                        m.SenderAvatar = await GetUserAvatarAsync(m.SenderId);
                    }

                    // Парсим файловые сообщения
                    if (m.MessageText?.StartsWith("[FILE]") == true)
                    {
                        Console.WriteLine($"📨 Найдено файловое сообщение: {m.MessageText}");
                        m.IsFileMessage = true;
                        var fileData = ParseFileMessage(m.MessageText);
                        m.FileName = fileData.FileName;
                        m.FileType = fileData.FileType?.ToLower();
                        m.FileSize = fileData.FileSize;
                        m.FilePath = fileData.FilePath;

                        Console.WriteLine($"📄 Файл: {m.FileName}, тип: {m.FileType}, путь: {m.FilePath}");
                    }
                    else
                    {
                        m.IsFileMessage = false;
                    }

                    _messages.Add(m);
                }

                if (_messages.Count > 0)
                {
                    MessagesCollectionView.ScrollTo(_messages[^1], position: ScrollToPosition.End, animate: true);
                }

                Console.WriteLine($"✅ Загружено {_messages.Count} сообщений, файловых: {_messages.Count(m => m.IsFileMessage)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки сообщений: {ex.Message}");
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

                    // Загружаем аватар для новых сообщений
                    if (string.IsNullOrEmpty(m.SenderAvatar) || m.SenderAvatar == "default_avatar.png")
                    {
                        m.SenderAvatar = await GetUserAvatarAsync(m.SenderId);
                    }

                    // Парсим файловые сообщения
                    if (m.MessageText?.StartsWith("[FILE]") == true)
                    {
                        m.IsFileMessage = true;
                        var fileData = ParseFileMessage(m.MessageText);
                        m.FileName = fileData.FileName;
                        m.FileType = fileData.FileType?.ToLower();
                        m.FileSize = fileData.FileSize;
                        m.FilePath = fileData.FilePath;
                    }

                    _messages.Add(m);
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_messages.Count > 0)
                        MessagesCollectionView.ScrollTo(_messages[^1], position: ScrollToPosition.End, animate: true);
                });

                // Обновляем счетчик непрочитанных при получении новых сообщений
                await RefreshUnreadCounts();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления сообщений: {ex.Message}");
            }
        }

        private async Task RefreshUnreadCounts()
        {
            try
            {
                _unreadMessagesCount = await _dbService.GetTeacherUnreadMessagesCountAsync(_currentUser.UserId);

                // Обновляем все группы
                foreach (var group in Groups)
                {
                    if (_unreadMessagesCount.ContainsKey(group.GroupId))
                    {
                        group.UnreadMessages = _unreadMessagesCount[group.GroupId];
                    }
                    else
                    {
                        group.UnreadMessages = 0;
                    }
                }

                // Принудительно обновляем отображение
                GroupsCollectionView.ItemsSource = null;
                GroupsCollectionView.ItemsSource = Groups;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления счетчиков: {ex.Message}");
            }
        }

        private async Task MarkMessagesAsRead()
        {
            if (_activeGroup == null) return;

            try
            {
                var success = await _dbService.MarkGroupMessagesAsReadAsync(_activeGroup.GroupId, _currentUser.UserId);
                if (success)
                {
                    Console.WriteLine($"✅ Все сообщения в группе {_activeGroup.GroupId} отмечены как прочитанные");
                }
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
            var text = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                var ok = await _dbService.SendGroupChatMessageAsync(_activeGroup.GroupId, _currentUser.UserId, text);
                if (ok)
                {
                    MessageEntry.Text = string.Empty;
                    await LoadMessages();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to send message", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error sending: {ex.Message}", "OK");
            }
        }

        private async void OnAttachFileClicked(object sender, EventArgs e)
        {
            if (_activeGroup == null) return;
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select file to send",
                    FileTypes = _supportedFileTypes
                });

                if (result != null)
                {
                    await SendFileAsync(result);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to attach file: {ex.Message}", "OK");
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
                        await DisplayAlert("Success", "File sent successfully", "OK");
                        await LoadMessages();
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to send file", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error sending file: {ex.Message}", "OK");
            }
        }

        private (string FilePath, string FileName, string FileType, string FileSize) ParseFileMessage(string messageText)
        {
            try
            {
                Console.WriteLine($"🔍 Парсим файловое сообщение: {messageText}");

                var parts = messageText.Split('|');
                if (parts.Length >= 5 && parts[0] == "[FILE]")
                {
                    var filePath = parts[1];
                    var fileName = parts[2];
                    var fileSize = parts[3];
                    var fileType = parts[4];

                    Console.WriteLine($"📁 Распарсено: {fileName}, тип: {fileType}, размер: {fileSize}, путь: {filePath}");

                    return (filePath, fileName, fileType, fileSize);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка парсинга файлового сообщения: {ex.Message}");
            }

            return (string.Empty, "Unknown file", "", "");
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
                    Console.WriteLine($"🎯 Тап по файловому сообщению: {message.FileName}");

                    var action = await DisplayActionSheet(
                        $"File: {fileData.FileName} ({fileData.FileSize})",
                        "Cancel",
                        null,
                        "📥 Download file",
                        "📁 Open file");

                    if (action == "📥 Download file")
                    {
                        await DownloadFile(fileData.FilePath, fileData.FileName);
                    }
                    else if (action == "📁 Open file")
                    {
                        await OpenFile(fileData.FilePath);
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to process file: {ex.Message}", "OK");
                }
            }
        }

        private async Task DownloadFile(string filePath, string fileName)
        {
            try
            {
                Console.WriteLine($"📥 Начинаем скачивание: {fileName}");
                Console.WriteLine($"📁 Исходный путь: {filePath}");

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    await DisplayAlert("Error", "File not found or path is empty", "OK");
                    return;
                }

                var success = await _fileService.DownloadFileAsync(filePath, fileName);
                if (success)
                {
                    await DisplayAlert("Success", $"File {fileName} downloaded", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Failed to download file", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка скачивания: {ex.Message}");
                await DisplayAlert("Error", $"Download error: {ex.Message}", "OK");
            }
        }

        private async Task OpenFile(string filePath)
        {
            try
            {
                Console.WriteLine($"📂 Открываем файл: {filePath}");

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    await DisplayAlert("Error", "File not found", "OK");
                    return;
                }

                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(filePath)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка открытия файла: {ex.Message}");
                await DisplayAlert("Error", $"Failed to open file: {ex.Message}", "OK");
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
        public int UnreadMessages { get; set; }
    }

    public class StatusTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool active && active ? "Active" : "Inactive";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}