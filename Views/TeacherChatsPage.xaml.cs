using EducationalPlatform.Converters;
using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using Microsoft.Maui.Devices;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        private bool _isLoadingGroups;
        private bool _isLoadingMessages;
        private bool _isRefreshingMessages;
        private bool _isSendingMessage;
        private bool _isSendingFile;

        // Словарь для хранения непрочитанных сообщений
        private Dictionary<int, int> _unreadMessagesCount = new();
        private readonly Dictionary<int, string> _avatarCache = new();

        // Поддерживаемые форматы файлов
        private readonly FilePickerFileType _supportedFileTypes = new(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".zip", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".txt", ".xls", ".xlsx", ".jpg", ".png", ".mp4" } },
                { DevicePlatform.macOS, new[] { ".zip", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".txt", ".xls", ".xlsx", ".jpg", ".png", ".mp4" } },
                { DevicePlatform.Android, new[] { ".zip", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".txt", ".xls", ".xlsx", ".jpg", ".png", ".mp4" } },
                { DevicePlatform.iOS, new[] { ".zip", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".txt", ".xls", ".xlsx", ".jpg", ".png", ".mp4" } }
            });

        public StudyGroup? ActiveGroup
        {
            get => _activeGroup;
            private set
            {
                if (_activeGroup != value)
                {
                    _activeGroup = value;
                    OnPropertyChanged(nameof(ActiveGroup));
                    OnPropertyChanged(nameof(HasActiveChat));
                }
            }
        }

        private async Task RefreshTeacherGroups()
        {
            try
            {
                var groups = await _dbService.GetTeacherStudyGroupsAsync(_currentUser.UserId) ?? new List<StudyGroup>();
                _unreadMessagesCount = await _dbService.GetTeacherUnreadMessagesCountAsync(_currentUser.UserId);

                var infoCache = new Dictionary<int, (string CourseName, int StudentCount, string LastMessage, DateTime LastMessageTime)>();
                foreach (var group in groups)
                {
                    infoCache[group.GroupId] = await GetGroupChatInfoAsync(group.GroupId);
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var updatedIds = groups.Select(g => g.GroupId).ToHashSet();

                    foreach (var group in groups)
                    {
                        var info = infoCache[group.GroupId];
                        var unreadCount = _unreadMessagesCount.TryGetValue(group.GroupId, out var count) ? count : 0;

                        var existing = Groups.FirstOrDefault(g => g.GroupId == group.GroupId);
                        if (existing != null)
                        {
                            existing.UpdateMeta(info.CourseName ?? existing.CourseName, info.StudentCount, info.LastMessage, info.LastMessageTime, unreadCount, group.IsActive);
                        }
                        else
                        {
                            Groups.Add(new TeacherGroupChat
                            {
                                GroupId = group.GroupId,
                                GroupName = group.GroupName,
                                CourseName = info.CourseName ?? "No Course",
                                StudentCount = info.StudentCount,
                                IsActive = group.IsActive,
                                UnreadMessages = unreadCount,
                                LastMessage = info.LastMessage,
                                LastMessageTime = info.LastMessageTime
                            });
                        }
                    }

                    var toRemove = Groups.Where(g => !updatedIds.Contains(g.GroupId)).ToList();
                    foreach (var remove in toRemove)
                    {
                        Groups.Remove(remove);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления списка групп: {ex.Message}");
            }
        }

        public bool HasActiveChat => ActiveGroup != null;

        public TeacherChatsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user ?? throw new ArgumentNullException(nameof(user));
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            // Используем Dependency Injection для получения FileService
            _fileService = ServiceHelper.GetService<FileService>();

            // ИНИЦИАЛИЗАЦИЯ КОЛЛЕКЦИИ ПЕРЕД ИСПОЛЬЗОВАНИЕМ
            Groups = new ObservableCollection<TeacherGroupChat>();

            BindingContext = this;

            // Подписываемся на глобальное событие изменения аватара,
            // чтобы сбрасывать кэш и обновлять аватарки в чате
            UserSessionService.AvatarChanged += OnGlobalAvatarChanged;

            // Устанавливаем ItemsSource для CollectionView
            GroupsCollectionView.ItemsSource = Groups;

            LoadTeacherGroups();

            // Привязываем сообщения к CollectionView
            MessagesCollectionView.ItemsSource = _messages;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Всегда обновляем список групп при появлении страницы
            LoadTeacherGroups();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _refreshTimer?.Dispose();
            UserSessionService.AvatarChanged -= OnGlobalAvatarChanged;
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
            if (_isLoadingGroups)
            {
                return;
            }

            _isLoadingGroups = true;

            try
            {
                // Показываем индикатор загрузки на весь экран
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                    var loadingIndicator = this.FindByName<ActivityIndicator>("LoadingIndicator");
                    if (loadingOverlay != null)
                    {
                        loadingOverlay.IsVisible = true;
                    }
                    if (loadingIndicator != null)
                    {
                        loadingIndicator.IsRunning = true;
                    }
                });
                if (_dbService == null)
                {
                    Console.WriteLine("❌ DatabaseService не инициализирован");
                    return;
                }

                Console.WriteLine($"🔄 Загружаем группы для учителя {_currentUser.UserId}");

                var groups = await _dbService.GetTeacherStudyGroupsAsync(_currentUser.UserId) ?? new List<StudyGroup>();
                _unreadMessagesCount = await _dbService.GetTeacherUnreadMessagesCountAsync(_currentUser.UserId);

                Console.WriteLine($"📊 Получено {groups.Count} групп из БД");

                var preparedItems = new List<TeacherGroupChat>();
                var addedGroupIds = new HashSet<int>();

                foreach (var group in groups)
                {
                    if (!addedGroupIds.Add(group.GroupId))
                    {
                        Console.WriteLine($"⚠️ Пропущен дубликат группы: {group.GroupName}, ID: {group.GroupId}");
                        continue;
                    }

                    var groupInfo = await GetGroupChatInfoAsync(group.GroupId);
                    var unreadCount = _unreadMessagesCount.TryGetValue(group.GroupId, out var count) ? count : 0;

                    preparedItems.Add(new TeacherGroupChat
                    {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName,
                        CourseName = groupInfo.CourseName ?? "No Course",
                        StudentCount = groupInfo.StudentCount,
                        IsActive = group.IsActive,
                        UnreadMessages = unreadCount,
                        LastMessage = groupInfo.LastMessage,
                        LastMessageTime = groupInfo.LastMessageTime
                    });
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Groups.Clear();
                    foreach (var item in preparedItems)
                    {
                        Groups.Add(item);
                    }

                    OnPropertyChanged(nameof(HasActiveChat));
                });

                Console.WriteLine($"✅ Загружено {Groups.Count} уникальных групп");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки групп: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Error", $"Failed to load groups: {ex.Message}", "OK");
            }
            finally
            {
                _isLoadingGroups = false;
                
                // Скрываем индикатор загрузки
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                    var loadingIndicator = this.FindByName<ActivityIndicator>("LoadingIndicator");
                    if (loadingOverlay != null)
                    {
                        loadingOverlay.IsVisible = false;
                    }
                    if (loadingIndicator != null)
                    {
                        loadingIndicator.IsRunning = false;
                    }
                });
            }
        }

        private async Task<(string CourseName, int StudentCount, string LastMessage, DateTime LastMessageTime)> GetGroupChatInfoAsync(int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        c.CourseName,
                        COUNT(DISTINCT ge.StudentId) as StudentCount,
                        (SELECT TOP 1 MessageText FROM GroupChatMessages WHERE GroupId = @GroupId ORDER BY SentAt DESC) as LastMessage,
                        (SELECT TOP 1 SentAt FROM GroupChatMessages WHERE GroupId = @GroupId ORDER BY SentAt DESC) as LastMessageTime
                    FROM StudyGroups sg
                    JOIN Courses c ON sg.CourseId = c.CourseId
                    LEFT JOIN GroupEnrollments ge ON sg.GroupId = ge.GroupId AND ge.Status = 'active'
                    WHERE sg.GroupId = @GroupId
                    GROUP BY c.CourseName";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return (
                        reader.GetString("CourseName"),
                        reader.GetInt32("StudentCount"),
                        reader.IsDBNull("LastMessage") ? "Чат создан" : reader.GetString("LastMessage"),
                        reader.IsDBNull("LastMessageTime") ? DateTime.Now : reader.GetDateTime("LastMessageTime")
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения информации о группе: {ex.Message}");
            }

            return ("No Course", 0, "Чат создан", DateTime.Now);
        }

        private async void OnGroupSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is TeacherGroupChat selectedGroup)
            {
                try
                {
                    Console.WriteLine($"🎯 Выбрана группа: {selectedGroup.GroupName}, ID: {selectedGroup.GroupId}");

                    // На мобильных устройствах переходим на отдельную страницу чата
                    if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
                    {
                        // Оптимизация: используем данные из selectedGroup вместо запроса к БД
                        // Переход должен быть быстрым, без блокирующих операций
                        var group = new StudyGroup
                        {
                            GroupId = selectedGroup.GroupId,
                            GroupName = selectedGroup.GroupName,
                            StudentCount = selectedGroup.StudentCount,
                            IsActive = selectedGroup.IsActive
                        };
                        
                        Console.WriteLine($"✅ Группа подготовлена, переходим в чат");
                        // Навигация выполняется напрямую на главном потоке для быстрого отклика
                        await Navigation.PushAsync(new GroupChatPage(group, _currentUser, _dbService, _settingsService));
                    }
                    else
                    {
                        // На десктопе показываем встроенный чат
                        ActiveGroup = new StudyGroup
                        {
                            GroupId = selectedGroup.GroupId,
                            GroupName = selectedGroup.GroupName,
                            StudentCount = selectedGroup.StudentCount,
                            IsActive = selectedGroup.IsActive
                        };

                        ChatGroupNameLabel.Text = selectedGroup.GroupName;
                        ChatOnlineLabel.Text = $"Студентов: {selectedGroup.StudentCount}";

                        _messages.Clear();
                        await LoadMessages();

                        // Отмечаем сообщения как прочитанные при открытии чата
                        await MarkMessagesAsRead();

                        // Обновляем счетчик непрочитанных в UI
                        await UpdateUnreadCount(selectedGroup.GroupId);

                        StartAutoRefresh();

                        // Уведомляем об изменении свойства для привязок
                        OnPropertyChanged(nameof(HasActiveChat));

                        // Обновляем список групп в фоне (не блокируем UI)
                        _ = Task.Run(async () => await RefreshTeacherGroups());
                    }
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
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var group = Groups.FirstOrDefault(g => g.GroupId == groupId);
                    if (group != null)
                    {
                        group.UnreadMessages = 0;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления счетчика: {ex.Message}");
            }
        }

        private async Task<string> GetUserAvatarAsync(int userId)
        {
            if (_avatarCache.TryGetValue(userId, out var cached))
            {
                return cached;
            }

            try
            {
                var avatarData = await _dbService.GetUserAvatarAsync(userId);
                if (string.IsNullOrEmpty(avatarData))
                {
                    avatarData = "default_avatar.png";
                }

                _avatarCache[userId] = avatarData;
                return avatarData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки аватара пользователя {userId}: {ex.Message}");
                return "default_avatar.png";
            }
        }

        private void OnGlobalAvatarChanged(object? sender, AvatarChangedEventArgs e)
        {
            try
            {
                // Сбрасываем кэш для пользователя с обновлённым аватаром
                if (_avatarCache.ContainsKey(e.UserId))
                    _avatarCache.Remove(e.UserId);

                var newAvatar = e.AvatarData ?? "default_avatar.png";

                // Обновляем аватар в уже загруженных сообщениях
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var msg in _messages.Where(m => m.SenderId == e.UserId))
                    {
                        msg.SenderAvatar = newAvatar;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки глобального изменения аватара в TeacherChatsPage: {ex.Message}");
            }
        }

        private async Task LoadMessages()
        {
            if (_isLoadingMessages || ActiveGroup == null) return;
            _isLoadingMessages = true;
            try
            {
                var list = await _dbService.GetGroupChatMessagesAsync(ActiveGroup.GroupId);

                // Очищаем только если это первая загрузка или группа изменилась
                if (_messages.Count == 0 || !_messages.Any(m => list.Any(l => l.MessageId == m.MessageId)))
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _messages.Clear();
                    });
                }

                // Добавляем только новые сообщения
                var existingIds = _messages.Select(m => m.MessageId).ToHashSet();
                var newMessages = list.Where(m => !existingIds.Contains(m.MessageId)).ToList();

                foreach (var m in newMessages)
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
                        m.FilePath = await ResolveFilePath(fileData.StorageDescriptor, fileData.FileName); // РЕШАЕМ ПУТЬ К ФАЙЛУ

                        Console.WriteLine($"📄 Файл: {m.FileName}, тип: {m.FileType}, путь: {m.FilePath}");
                    }
                    else
                    {
                        m.IsFileMessage = false;
                    }

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _messages.Add(m);
                    });
                }

                // Сортируем по дате отправки
                var sortedMessages = _messages.OrderBy(m => m.SentAt).ToList();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _messages.Clear();
                    foreach (var msg in sortedMessages)
                    {
                        _messages.Add(msg);
                    }
                });

                if (_messages.Count > 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        MessagesCollectionView.ScrollTo(_messages[^1], position: ScrollToPosition.End, animate: true);
                    });
                }

                Console.WriteLine($"✅ Загружено {_messages.Count} сообщений, файловых: {_messages.Count(m => m.IsFileMessage)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки сообщений: {ex.Message}");
            }
            finally
            {
                _isLoadingMessages = false;
            }
        }

        // УНИВЕРСАЛЬНЫЙ МЕТОД ДЛЯ РЕШЕНИЯ ПУТЕЙ К ФАЙЛАМ НА ВСЕХ ПЛАТФОРМАХ
        private async Task<string> ResolveFilePath(string filePath, string? preferredFileName = null)
        {
            return await _fileService.ResolveFilePath(filePath, preferredFileName, "ChatFiles");
        }

        private async Task RefreshMessages()
        {
            if (_isRefreshingMessages || ActiveGroup == null) return;
            _isRefreshingMessages = true;
            try
            {
                var list = await _dbService.GetGroupChatMessagesAsync(ActiveGroup.GroupId);
                var existingIds = _messages.Select(m => m.MessageId).ToHashSet();
                var newOnes = list.Where(m => !existingIds.Contains(m.MessageId)).ToList();

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
                        m.FilePath = await ResolveFilePath(fileData.StorageDescriptor, fileData.FileName);
                    }
                    else
                    {
                        m.IsFileMessage = false;
                    }

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _messages.Add(m);
                    });
                }

                // Сортируем по дате
                var sortedMessages = _messages.OrderBy(m => m.SentAt).ToList();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _messages.Clear();
                    foreach (var msg in sortedMessages)
                    {
                        _messages.Add(msg);
                    }
                });

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
            finally
            {
                _isRefreshingMessages = false;
            }
        }

        private void StartAutoRefresh()
        {
            _refreshTimer?.Dispose();
            _refreshTimer = new Timer(async _ => await RefreshMessages(), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
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

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления счетчиков: {ex.Message}");
            }
        }

        private async Task MarkMessagesAsRead()
        {
            if (ActiveGroup == null) return;

            try
            {
                var success = await _dbService.MarkGroupMessagesAsReadAsync(ActiveGroup.GroupId, _currentUser.UserId);
                if (success)
                {
                    Console.WriteLine($"✅ Все сообщения в группе {ActiveGroup.GroupId} отмечены как прочитанные");
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
            if (ActiveGroup == null || _isSendingMessage) return;
            var text = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                _isSendingMessage = true;
                var ok = await _dbService.SendGroupChatMessageAsync(ActiveGroup.GroupId, _currentUser.UserId, text);
                if (ok)
                {
                    MessageEntry.Text = string.Empty;
                    await LoadMessages();

                    // Обновляем список групп для отображения последнего сообщения
                    await RefreshTeacherGroups();
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
            finally
            {
                _isSendingMessage = false;
            }
        }

        private async void OnAttachFileClicked(object sender, EventArgs e)
        {
            if (ActiveGroup == null || _isSendingFile) return;
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
            if (ActiveGroup == null) return;
            try
            {
                _isSendingFile = true;
                using var stream = await fileResult.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var fileSize = _fileService.FormatFileSize(fileBytes.Length);
                var mimeType = _fileService.GetMimeType(Path.GetExtension(fileResult.FileName));
                var base64Payload = Convert.ToBase64String(fileBytes);

                var message = $"[FILE]|BASE64|{mimeType}|{base64Payload}|{fileResult.FileName}|{fileSize}|{Path.GetExtension(fileResult.FileName)}";

                var ok = await _dbService.SendGroupChatMessageAsync(ActiveGroup.GroupId, _currentUser.UserId, message);
                if (ok)
                {
                    await DisplayAlert("Success", "File sent successfully", "OK");
                    await LoadMessages();

                    // Обновляем список групп
                    await RefreshTeacherGroups();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to send file", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error sending file: {ex.Message}", "OK");
            }
            finally
            {
                _isSendingFile = false;
            }
        }

        private FileMessagePayload ParseFileMessage(string messageText)
        {
            try
            {
                Console.WriteLine($"🔍 Парсим файловое сообщение: {messageText}");

                var parts = messageText.Split('|');
                if (parts.Length >= 7 && parts[0] == "[FILE]" && parts[1].Equals("BASE64", StringComparison.OrdinalIgnoreCase))
                {
                    var mime = parts[2];
                    var base64 = parts[3];
                    var fileName = parts[4];
                    var fileSize = parts[5];
                    var fileType = parts[6];

                    Console.WriteLine($"📁 Распарсено (base64): {fileName}, тип: {fileType}, размер: {fileSize}");

                    return new FileMessagePayload
                    {
                        StorageDescriptor = $"data:{mime};base64,{base64}",
                        FileName = fileName,
                        FileType = fileType,
                        FileSize = fileSize,
                        MimeType = mime
                    };
                }

                if (parts.Length >= 5 && parts[0] == "[FILE]")
                {
                    var filePath = parts[1];
                    var fileName = parts[2];
                    var fileSize = parts[3];
                    var fileType = parts[4];

                    Console.WriteLine($"📁 Распарсено (legacy): {fileName}, тип: {fileType}, размер: {fileSize}");

                    return new FileMessagePayload
                    {
                        StorageDescriptor = filePath,
                        FileName = fileName,
                        FileType = fileType,
                        FileSize = fileSize,
                        MimeType = _fileService.GetMimeType(fileType)
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка парсинга файлового сообщения: {ex.Message}");
            }

            return new FileMessagePayload();
        }

        private async void OnFileMessageTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is GroupChatMessage message && message.IsFileMessage)
            {
                try
                {
                    var fileData = ParseFileMessage(message.MessageText);
                    Console.WriteLine($"🎯 Тап по файловому сообщению: {message.FileName}");

                    // РЕШАЕМ ПУТЬ К ФАЙЛУ ПЕРЕД ОТКРЫТИЕМ
                    var resolvedPath = await ResolveFilePath(fileData.StorageDescriptor, fileData.FileName);

                    if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
                    {
                        await DisplayAlert("Ошибка", "Файл не найден", "OK");
                        return;
                    }

                    var action = await DisplayActionSheet(
                        $"File: {fileData.FileName} ({fileData.FileSize})",
                        "Cancel",
                        null,
                        "📥 Download file",
                        "📁 Open file");

                    if (action == "📥 Download file")
                    {
                        await DownloadFile(resolvedPath, fileData.FileName);
                    }
                    else if (action == "📁 Open file")
                    {
                        await OpenFile(resolvedPath);
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

        // Вспомогательные методы для работы с UI
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private class FileMessagePayload
        {
            public string StorageDescriptor { get; set; } = string.Empty;
            public string FileName { get; set; } = "Unknown file";
            public string FileType { get; set; } = string.Empty;
            public string FileSize { get; set; } = string.Empty;
            public string MimeType { get; set; } = "application/octet-stream";
        }
    }

    public class TeacherGroupChat : INotifyPropertyChanged
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;

        private string _courseName = string.Empty;
        public string CourseName
        {
            get => _courseName;
            set => SetProperty(ref _courseName, value);
        }

        private int _studentCount;
        public int StudentCount
        {
            get => _studentCount;
            set => SetProperty(ref _studentCount, value);
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        private int _unreadMessages;
        public int UnreadMessages
        {
            get => _unreadMessages;
            set
            {
                if (SetProperty(ref _unreadMessages, value))
                {
                    OnPropertyChanged(nameof(UnreadBadgeText));
                    OnPropertyChanged(nameof(HasUnreadMessages));
                }
            }
        }

        private string _lastMessage = string.Empty;
        public string LastMessage
        {
            get => _lastMessage;
            set
            {
                if (SetProperty(ref _lastMessage, value))
                {
                    OnPropertyChanged(nameof(LastMessagePreview));
                }
            }
        }

        private DateTime _lastMessageTime = DateTime.Now;
        public DateTime LastMessageTime
        {
            get => _lastMessageTime;
            set
            {
                if (SetProperty(ref _lastMessageTime, value))
                {
                    OnPropertyChanged(nameof(LastMessageTimeDisplay));
                }
            }
        }

        public string UnreadBadgeText => UnreadMessages > 0 ? UnreadMessages.ToString() : "";
        public bool HasUnreadMessages => UnreadMessages > 0;
        public string LastMessagePreview => LastMessage.Length > 50 ? LastMessage.Substring(0, 50) + "..." : LastMessage;
        public string LastMessageTimeDisplay => LastMessageTime.ToString("dd.MM.yyyy HH:mm");

        public void UpdateMeta(string courseName, int studentCount, string lastMessage, DateTime lastMessageTime, int unreadMessages, bool isActive)
        {
            CourseName = courseName;
            StudentCount = studentCount;
            LastMessage = lastMessage;
            LastMessageTime = lastMessageTime;
            UnreadMessages = unreadMessages;
            IsActive = isActive;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}