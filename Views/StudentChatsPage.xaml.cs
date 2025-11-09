using System.Collections.ObjectModel;
using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EducationalPlatform.Views
{
    public partial class StudentChatsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        // Добавьте это поле в класс
        private Dictionary<(int ChatId, string ChatType), int> _unreadMessagesCount = new();

        public ObservableCollection<StudentChatItem> AllChats { get; set; } = new();
        public ObservableCollection<ChatMessage> Messages { get; set; } = new();

        private StudentChatItem? _activeChat;
        private Timer? _refreshTimer;

        public StudentChatItem? ActiveChat
        {
            get => _activeChat;
            set
            {
                if (_activeChat != value)
                {
                    _activeChat = value;
                    OnPropertyChanged(nameof(ActiveChat));
                    OnPropertyChanged(nameof(HasActiveChat));
                }
            }
        }

        public bool HasActiveChat => ActiveChat != null;

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public StudentChatsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            BindingContext = this;

            // Проверяем и создаем таблицы при создании страницы
            _ = InitializeChatTables();

            AllChatsCollectionView.ItemsSource = AllChats;
            MessagesCollectionView.ItemsSource = Messages;

            LoadAllChats();
        }

        private async Task InitializeChatTables()
        {
            try
            {
                await _dbService.CheckAndCreateMissingTables();
                await _dbService.CreateMissingChatTables();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка инициализации таблиц: {ex.Message}");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadAllChats();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _refreshTimer?.Dispose();
        }

        // Обновите метод LoadAllChats
        private async void LoadAllChats()
        {
            try
            {
                AllChats.Clear();
                IsBusy = true;

                Console.WriteLine($"🔍 Загружаем чаты для студента {_currentUser.UserId}");

                // Загружаем чаты и непрочитанные сообщения
                var chats = await _dbService.GetStudentAllChatsAsync(_currentUser.UserId);
                _unreadMessagesCount = await _dbService.GetStudentUnreadMessagesCountAsync(_currentUser.UserId);

                Console.WriteLine($"📊 Получено чатов: {chats?.Count ?? 0}");

                if (chats == null || !chats.Any())
                {
                    Console.WriteLine("ℹ️ Чатов не найдено, проверяем группы студента...");
                    await CheckStudentGroups();
                    return;
                }

                // Очищаем коллекцию перед добавлением
                AllChats.Clear();

                foreach (var chat in chats)
                {
                    Console.WriteLine($"💬 Добавляем чат: {chat.ChatName}, ID: {chat.ChatId}, тип: {chat.ChatType}");

                    // Устанавливаем количество непрочитанных сообщений
                    var key = (chat.ChatId, chat.ChatType);
                    if (_unreadMessagesCount.ContainsKey(key))
                    {
                        chat.UnreadMessages = _unreadMessagesCount[key];
                    }

                    // Проверяем на дубликаты перед добавлением
                    if (!AllChats.Any(c => c.ChatId == chat.ChatId && c.ChatType == chat.ChatType))
                    {
                        AllChats.Add(chat);
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Пропущен дубликат чата: {chat.ChatName}, ID: {chat.ChatId}");
                    }
                }

                OnPropertyChanged(nameof(AllChats));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки чатов: {ex.Message}");
                await DisplayAlert("Error", $"Failed to load chats: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CheckStudentGroups()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT sg.GroupId, sg.GroupName, c.CourseName
            FROM GroupEnrollments ge
            INNER JOIN StudyGroups sg ON ge.GroupId = sg.GroupId
            INNER JOIN Courses c ON sg.CourseId = c.CourseId
            WHERE ge.StudentId = @StudentId 
                AND ge.Status = 'active'
                AND sg.IsActive = 1";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentId", _currentUser.UserId);

                using var reader = await command.ExecuteReaderAsync();

                int groupCount = 0;
                while (await reader.ReadAsync())
                {
                    groupCount++;
                    Console.WriteLine($"👥 Студент состоит в группе: {reader.GetString("GroupName")} (Курс: {reader.GetString("CourseName")})");
                }

                if (groupCount == 0)
                {
                    await DisplayAlert("Info", "You are not in any active group", "OK");
                }
                else
                {
                    await DisplayAlert("Diagnostics",
                        $"You are in {groupCount} groups, but chats were not found. " +
                        "Please contact your teacher to set up chats.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка проверки групп: {ex.Message}");
            }
        }

        // Обновите метод OnChatSelected
        private async void OnChatSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is StudentChatItem selectedChat)
            {
                try
                {
                    Console.WriteLine($"🎯 Выбран чат: {selectedChat.ChatName}, тип: {selectedChat.ChatType}");

                    ActiveChat = selectedChat;
                    UpdateChatHeader(selectedChat);
                    await LoadChatMessages(selectedChat);

                    // Отмечаем сообщения как прочитанные ПЕРЕД обновлением счетчика
                    await MarkMessagesAsRead(selectedChat);

                    // Обновляем счетчик непрочитанных в UI
                    await UpdateUnreadCount(selectedChat);

                    // Запускаем автообновление
                    StartAutoRefresh(selectedChat);

                    // Обновляем список чатов для актуальных счетчиков
                    await RefreshChatList();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to open chat: {ex.Message}", "OK");
                }
            }

            // Сбрасываем выделение
            if (sender is CollectionView collectionView)
            {
                collectionView.SelectedItem = null;
            }
        }

        // Добавьте метод для обновления счетчика непрочитанных
        private async Task UpdateUnreadCount(StudentChatItem chat)
        {
            try
            {
                // Обновляем счетчик в локальном словаре
                var key = (chat.ChatId, chat.ChatType);
                if (_unreadMessagesCount.ContainsKey(key))
                {
                    _unreadMessagesCount[key] = 0;
                }

                // Обновляем отображение в списке чатов
                chat.UnreadMessages = 0;

                // Принудительно обновляем отображение
                AllChatsCollectionView.ItemsSource = null;
                AllChatsCollectionView.ItemsSource = AllChats;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления счетчика: {ex.Message}");
            }
        }

        private void UpdateChatHeader(StudentChatItem chat)
        {
            switch (chat.ChatType)
            {
                case "group":
                    ChatTitleLabel.Text = chat.ChatName;
                    ChatSubtitleLabel.Text = $"{chat.CourseName} • Participants: {chat.ParticipantCount}";
                    break;
                case "teacher":
                    ChatTitleLabel.Text = chat.ChatName;
                    ChatSubtitleLabel.Text = chat.TeacherSubject ?? "Teacher";
                    break;
                case "support":
                    ChatTitleLabel.Text = chat.ChatName;
                    ChatSubtitleLabel.Text = chat.Description;
                    break;
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

        private async Task LoadChatMessages(StudentChatItem chat)
        {
            if (chat == null) return;

            try
            {
                Messages.Clear();
                Console.WriteLine($"📨 Загружаем сообщения для чата {chat.ChatName}");

                List<ChatMessage> messages = new();

                switch (chat.ChatType)
                {
                    case "group":
                        if (chat.GroupId.HasValue)
                        {
                            var groupMessages = await _dbService.GetGroupChatMessagesAsync(chat.GroupId.Value);
                            foreach (var m in groupMessages)
                            {
                                var message = new ChatMessage
                                {
                                    MessageId = m.MessageId,
                                    SenderId = m.SenderId,
                                    MessageText = m.MessageText,
                                    SentAt = m.SentAt,
                                    IsRead = m.IsRead,
                                    SenderName = m.SenderName,
                                    IsMyMessage = m.SenderId == _currentUser.UserId,
                                    IsFileMessage = m.IsFileMessage,
                                    FileName = m.FileName,
                                    FileType = m.FileType,
                                    FileSize = m.FileSize
                                };

                                // Загружаем аватар
                                message.SenderAvatar = await GetUserAvatarAsync(m.SenderId);

                                messages.Add(message);
                            }
                        }
                        break;
                    case "teacher":
                        if (chat.TeacherId.HasValue)
                        {
                            var privateMessages = await _dbService.GetPrivateChatMessagesAsync(_currentUser.UserId, chat.TeacherId.Value);
                            foreach (var m in privateMessages)
                            {
                                var message = new ChatMessage
                                {
                                    MessageId = m.MessageId,
                                    SenderId = m.SenderId,
                                    MessageText = m.MessageText,
                                    SentAt = m.SentAt,
                                    IsRead = m.IsRead,
                                    SenderName = m.SenderName,
                                    IsMyMessage = m.SenderId == _currentUser.UserId
                                };

                                // Загружаем аватар
                                message.SenderAvatar = await GetUserAvatarAsync(m.SenderId);

                                messages.Add(message);
                            }
                        }
                        break;
                    case "support":
                        var supportMessages = await _dbService.GetSupportChatMessagesAsync(_currentUser.UserId);
                        foreach (var m in supportMessages)
                        {
                            var message = new ChatMessage
                            {
                                MessageId = m.MessageId,
                                SenderId = m.SenderId,
                                MessageText = m.MessageText,
                                SentAt = m.SentAt,
                                IsRead = m.IsRead,
                                SenderName = m.SenderName,
                                IsMyMessage = m.SenderId == _currentUser.UserId
                            };

                            // Загружаем аватар
                            message.SenderAvatar = await GetUserAvatarAsync(m.SenderId);

                            messages.Add(message);
                        }
                        break;
                }

                foreach (var message in messages)
                {
                    Messages.Add(message);
                }

                Console.WriteLine($"📨 Загружено {messages.Count} сообщений");

                // Прокручиваем к последнему сообщению
                if (Messages.Count > 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        MessagesCollectionView.ScrollTo(Messages[^1], position: ScrollToPosition.End, animate: true);
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки сообщений: {ex.Message}");
                await DisplayAlert("Error", $"Failed to load messages: {ex.Message}", "OK");
            }
        }

        private async Task MarkMessagesAsRead(StudentChatItem chat)
        {
            if (chat == null) return;

            try
            {
                switch (chat.ChatType)
                {
                    case "group":
                        if (chat.GroupId.HasValue)
                            await _dbService.MarkGroupMessagesAsReadAsync(chat.GroupId.Value, _currentUser.UserId);
                        break;
                    case "teacher":
                        if (chat.TeacherId.HasValue)
                            await _dbService.MarkPrivateMessagesAsReadAsync(_currentUser.UserId, chat.TeacherId.Value);
                        break;
                    case "support":
                        await _dbService.MarkSupportMessagesAsReadAsync(_currentUser.UserId);
                        break;
                }

                // Обновляем счетчик непрочитанных в списке чатов
                chat.UnreadMessages = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка отметки сообщений как прочитанных: {ex.Message}");
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
            if (_activeChat == null) return;

            var text = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                bool success = false;

                switch (_activeChat.ChatType)
                {
                    case "group":
                        if (_activeChat.GroupId.HasValue)
                            success = await _dbService.SendGroupChatMessageAsync(_activeChat.GroupId.Value, _currentUser.UserId, text);
                        break;
                    case "teacher":
                        if (_activeChat.TeacherId.HasValue)
                            success = await _dbService.SendPrivateMessageAsync(_currentUser.UserId, _activeChat.TeacherId.Value, text);
                        break;
                    case "support":
                        success = await _dbService.SendSupportMessageAsync(_currentUser.UserId, text);
                        break;
                }

                if (success)
                {
                    MessageEntry.Text = string.Empty;
                    await LoadChatMessages(_activeChat);
                    await RefreshChatList();
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

        private void StartAutoRefresh(StudentChatItem chat)
        {
            _refreshTimer?.Dispose();
            _refreshTimer = new Timer(async _ => await RefreshChatMessages(chat), null,
                TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        private async Task RefreshChatMessages(StudentChatItem chat)
        {
            if (chat == null) return;

            try
            {
                // Получаем только новые сообщения, чтобы избежать дублирования
                List<ChatMessage> newMessages = new();

                switch (chat.ChatType)
                {
                    case "group":
                        if (chat.GroupId.HasValue)
                        {
                            var allGroupMessages = await _dbService.GetGroupChatMessagesAsync(chat.GroupId.Value);
                            var existingGroupIds = Messages.Select(m => m.MessageId).ToHashSet();
                            foreach (var m in allGroupMessages.Where(m => !existingGroupIds.Contains(m.MessageId)))
                            {
                                var message = new ChatMessage
                                {
                                    MessageId = m.MessageId,
                                    SenderId = m.SenderId,
                                    MessageText = m.MessageText,
                                    SentAt = m.SentAt,
                                    IsRead = m.IsRead,
                                    SenderName = m.SenderName,
                                    IsMyMessage = m.SenderId == _currentUser.UserId,
                                    IsFileMessage = m.IsFileMessage,
                                    FileName = m.FileName,
                                    FileType = m.FileType,
                                    FileSize = m.FileSize
                                };

                                // Загружаем аватар
                                message.SenderAvatar = await GetUserAvatarAsync(m.SenderId);
                                newMessages.Add(message);
                            }
                        }
                        break;
                    case "teacher":
                        if (chat.TeacherId.HasValue)
                        {
                            var allPrivateMessages = await _dbService.GetPrivateChatMessagesAsync(_currentUser.UserId, chat.TeacherId.Value);
                            var existingPrivateIds = Messages.Select(m => m.MessageId).ToHashSet();
                            foreach (var m in allPrivateMessages.Where(m => !existingPrivateIds.Contains(m.MessageId)))
                            {
                                var message = new ChatMessage
                                {
                                    MessageId = m.MessageId,
                                    SenderId = m.SenderId,
                                    MessageText = m.MessageText,
                                    SentAt = m.SentAt,
                                    IsRead = m.IsRead,
                                    SenderName = m.SenderName,
                                    IsMyMessage = m.SenderId == _currentUser.UserId
                                };

                                // Загружаем аватар
                                message.SenderAvatar = await GetUserAvatarAsync(m.SenderId);
                                newMessages.Add(message);
                            }
                        }
                        break;
                    case "support":
                        var allSupportMessages = await _dbService.GetSupportChatMessagesAsync(_currentUser.UserId);
                        var existingSupportIds = Messages.Select(m => m.MessageId).ToHashSet();
                        foreach (var m in allSupportMessages.Where(m => !existingSupportIds.Contains(m.MessageId)))
                        {
                            var message = new ChatMessage
                            {
                                MessageId = m.MessageId,
                                SenderId = m.SenderId,
                                MessageText = m.MessageText,
                                SentAt = m.SentAt,
                                IsRead = m.IsRead,
                                SenderName = m.SenderName,
                                IsMyMessage = m.SenderId == _currentUser.UserId
                            };

                            // Загружаем аватар
                            message.SenderAvatar = await GetUserAvatarAsync(m.SenderId);
                            newMessages.Add(message);
                        }
                        break;
                }

                if (newMessages.Any())
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        foreach (var message in newMessages)
                        {
                            Messages.Add(message);
                        }

                        if (Messages.Count > 0)
                        {
                            MessagesCollectionView.ScrollTo(Messages[^1], position: ScrollToPosition.End, animate: true);
                        }
                    });

                    await MarkMessagesAsRead(chat);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка обновления сообщений: {ex.Message}");
            }
        }



        // Обновите метод RefreshChatList
        private async Task RefreshChatList()
        {
            try
            {
                var chats = await _dbService.GetStudentAllChatsAsync(_currentUser.UserId);
                if (chats == null) return;

                // Обновляем счетчики непрочитанных
                _unreadMessagesCount = await _dbService.GetStudentUnreadMessagesCountAsync(_currentUser.UserId);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Создаем хэш-сет для быстрой проверки существующих чатов
                    var existingChatKeys = AllChats.Select(c => (c.ChatId, c.ChatType)).ToHashSet();
                    var updatedChatKeys = chats.Select(c => (c.ChatId, c.ChatType)).ToHashSet();

                    // Обновляем существующие чаты или добавляем новые
                    foreach (var updatedChat in chats)
                    {
                        var existingChat = AllChats.FirstOrDefault(c =>
                            c.ChatId == updatedChat.ChatId &&
                            c.ChatType == updatedChat.ChatType);

                        if (existingChat != null)
                        {
                            // Обновляем существующий чат
                            existingChat.LastMessage = updatedChat.LastMessage;
                            existingChat.LastMessageTime = updatedChat.LastMessageTime;

                            // Обновляем счетчик непрочитанных
                            var key = (updatedChat.ChatId, updatedChat.ChatType);
                            if (_unreadMessagesCount.ContainsKey(key))
                            {
                                existingChat.UnreadMessages = _unreadMessagesCount[key];
                            }
                            else
                            {
                                existingChat.UnreadMessages = 0;
                            }
                        }
                        else
                        {
                            // Добавляем новый чат только если его нет в списке
                            var key = (updatedChat.ChatId, updatedChat.ChatType);
                            if (_unreadMessagesCount.ContainsKey(key))
                            {
                                updatedChat.UnreadMessages = _unreadMessagesCount[key];
                            }

                            // Проверяем, что чата действительно нет в списке
                            if (!AllChats.Any(c => c.ChatId == updatedChat.ChatId && c.ChatType == updatedChat.ChatType))
                            {
                                AllChats.Add(updatedChat);
                            }
                        }
                    }

                    // Удаляем чаты, которых больше нет
                    var toRemove = AllChats.Where(c => !updatedChatKeys.Contains((c.ChatId, c.ChatType))).ToList();
                    foreach (var chat in toRemove)
                    {
                        AllChats.Remove(chat);
                    }

                    Console.WriteLine($"🔄 Обновлено чатов: {AllChats.Count}");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка обновления списка чатов: {ex.Message}");
            }
        }

        private async void OnAttachFileClicked(object sender, EventArgs e)
        {
            if (_activeChat == null) return;

            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select file to send",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    // TODO: Реализовать отправку файлов
                    await DisplayAlert("Info", $"File {result.FileName} selected for sending", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to attach file: {ex.Message}", "OK");
            }
        }

        private async void OnFileMessageTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is ChatMessage message && message.IsFileMessage)
            {
                try
                {
                    var fileData = ParseFileMessage(message.MessageText);

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

                var fileService = new FileService();
                var success = await fileService.DownloadFileAsync(filePath, fileName);
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

        private async void OnMyCoursesClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new MyCoursesPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open my courses: {ex.Message}", "OK");
            }
        }
    }
}