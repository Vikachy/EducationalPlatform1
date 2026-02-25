using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using Microsoft.Data.SqlClient;

namespace EducationalPlatform.Views
{
    public partial class GroupChatPage : ContentPage, INotifyPropertyChanged
    {
        private readonly StudyGroup _group;
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;
        private readonly FileService _fileService;
        private readonly Dictionary<int, string> _avatarCache = new();
        private System.Timers.Timer? _refreshTimer;
        private System.Timers.Timer? _typingTimer;
        private bool _isAtBottom = true;
        private bool _isTyping = false;
        private bool _isLoading = false;
        private bool _isFirstLoad = true;
        private int _lastMessageId = 0;

        public ObservableCollection<GroupedGroupMessages> GroupedMessages { get; } = new();
        public string GroupName => _group.GroupName;

        private string _groupAvatarUrl;
        public string GroupAvatarUrl
        {
            get => _groupAvatarUrl;
            set
            {
                _groupAvatarUrl = value;
                OnPropertyChanged();
            }
        }

        private string _onlineStatus;
        public string OnlineStatus
        {
            get => _onlineStatus;
            set
            {
                _onlineStatus = value;
                OnPropertyChanged();
            }
        }

        public bool IsTeacher => _user.RoleId == 2 || _user.RoleId == 3 || _user.RoleId == 4;

        public GroupChatPage(StudyGroup group, User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
            }

            _group = group;
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;
            _fileService = ServiceHelper.GetService<FileService>();

            // Загружаем аватар группы
            _ = LoadGroupAvatarAsync();

            BindingContext = this;

            LoadMessages();
            StartAutoRefresh();
            UpdateOnlineStatus();

            var messagesScrollView = this.FindByName<ScrollView>("MessagesScrollView");
            if (messagesScrollView != null)
                messagesScrollView.Scrolled += OnMessagesScrolled;
        }

        private async Task LoadGroupAvatarAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = "SELECT AvatarUrl FROM StudyGroups WHERE GroupId = @GroupId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", _group.GroupId);

                var result = await command.ExecuteScalarAsync();

                if (result != null && !string.IsNullOrEmpty(result.ToString()))
                {
                    GroupAvatarUrl = result.ToString();
                }
                else
                {
                    // Проверяем наличие файла в локальной папке
                    string localPath = Path.Combine(FileSystem.AppDataDirectory, "GroupAvatars", $"group_{_group.GroupId}.png");
                    if (File.Exists(localPath))
                    {
                        GroupAvatarUrl = localPath;
                    }
                    else
                    {
                        GroupAvatarUrl = "default_group.png";
                    }
                }
            }
            catch
            {
                GroupAvatarUrl = "default_group.png";
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            MarkMessagesAsRead();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _typingTimer?.Stop();
            _typingTimer?.Dispose();

            var messagesScrollView = this.FindByName<ScrollView>("MessagesScrollView");
            if (messagesScrollView != null)
                messagesScrollView.Scrolled -= OnMessagesScrolled;
        }

        private void OnMessagesScrolled(object? sender, ScrolledEventArgs e)
        {
            var scrollView = sender as ScrollView;
            if (scrollView != null)
            {
                var isAtBottom = scrollView.ScrollY >= scrollView.ContentSize.Height - scrollView.Height - 100;
                if (_isAtBottom != isAtBottom)
                {
                    _isAtBottom = isAtBottom;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var scrollButton = this.FindByName<Border>("ScrollToBottomButton");
                        if (scrollButton != null)
                            scrollButton.IsVisible = !_isAtBottom && !_isFirstLoad;
                    });
                }
            }
        }

        private void StartAutoRefresh()
        {
            _refreshTimer = new System.Timers.Timer(5000);
            _refreshTimer.Elapsed += async (s, e) => await RefreshMessages();
            _refreshTimer.Start();
        }

        private async Task RefreshMessages()
        {
            if (_isLoading) return;

            try
            {
                var messages = await _dbService.GetGroupChatMessagesAsync(_group.GroupId);

                var maxId = messages.Count > 0 ? messages.Max(m => m.MessageId) : 0;
                if (maxId <= _lastMessageId) return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AddNewMessages(messages);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления сообщений: {ex.Message}");
            }
        }

        private void AddNewMessages(List<GroupChatMessage> allMessages)
        {
            var existingIds = new HashSet<int>();
            foreach (var group in GroupedMessages)
            {
                foreach (var msg in group.Messages)
                {
                    existingIds.Add(msg.MessageId);
                }
            }

            var newMessages = allMessages.Where(m => !existingIds.Contains(m.MessageId)).ToList();
            if (newMessages.Count == 0) return;

            foreach (var message in newMessages)
            {
                message.IsMyMessage = message.SenderId == _user.UserId;

                var dateKey = GetDateKey(message.SentAt);
                var dateDisplay = GetDateDisplay(dateKey);
                var group = GroupedMessages.FirstOrDefault(g => g.Date == dateDisplay);

                if (group == null)
                {
                    group = new GroupedGroupMessages
                    {
                        Date = dateDisplay,
                        Messages = new ObservableCollection<GroupChatMessage>()
                    };
                    GroupedMessages.Add(group);
                }

                group.Messages.Add(message);
            }

            // Сортируем группы по дате
            var sortedGroups = GroupedMessages
                .OrderBy(g => DateTime.Parse(GetDateKeyFromDisplay(g.Date)))
                .ToList();

            GroupedMessages.Clear();
            foreach (var g in sortedGroups)
            {
                GroupedMessages.Add(g);
            }

            _lastMessageId = allMessages.Max(m => m.MessageId);

            // Прокручиваем вниз если пользователь был внизу
            if (_isAtBottom)
            {
                ScrollToBottom();
            }

            _isFirstLoad = false;
        }

        // НОВЫЙ МЕТОД: Прокрутка вниз
        private void ScrollToBottom()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var messagesScrollView = this.FindByName<ScrollView>("MessagesScrollView");
                    if (messagesScrollView != null && GroupedMessages.Count > 0)
                    {
                        await messagesScrollView.ScrollToAsync(0, messagesScrollView.ContentSize.Height, true);

                        var scrollButton = this.FindByName<Border>("ScrollToBottomButton");
                        if (scrollButton != null)
                            scrollButton.IsVisible = false;

                        _isAtBottom = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка прокрутки: {ex.Message}");
                }
            });
        }

        // НОВЫЙ МЕТОД: Прокрутка к последнему сообщению в группе
        private void ScrollToLastMessageInGroup()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (GroupedMessages.Count == 0) return;

                    var lastGroup = GroupedMessages.LastOrDefault();
                    if (lastGroup?.Messages.Count > 0)
                    {
                        var messagesScrollView = this.FindByName<ScrollView>("MessagesScrollView");
                        if (messagesScrollView != null)
                        {
                            // Рассчитываем примерную позицию последнего сообщения
                            double targetY = messagesScrollView.ContentSize.Height - 100;
                            await messagesScrollView.ScrollToAsync(0, targetY, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка прокрутки: {ex.Message}");
                }
            });
        }

        private string GetDateKeyFromDisplay(string display)
        {
            if (display == "Сегодня" || display == "Today")
                return DateTime.Today.ToString("yyyy-MM-dd");
            if (display == "Вчера" || display == "Yesterday")
                return DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            return DateTime.Parse(display).ToString("yyyy-MM-dd");
        }

        private async void LoadMessages()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                    if (loadingOverlay != null && _isFirstLoad)
                        loadingOverlay.IsVisible = true;
                });

                var messages = await _dbService.GetGroupChatMessagesAsync(_group.GroupId);

                if (messages.Count > 0)
                    _lastMessageId = messages.Max(m => m.MessageId);

                foreach (var message in messages)
                {
                    message.IsMyMessage = message.SenderId == _user.UserId;

                    // Загружаем аватар и эмодзи для отправителя
                    var senderAvatar = await GetUserAvatarAsync(message.SenderId);
                    message.SenderAvatar = senderAvatar;

                    var equipped = await _dbService.GetEquippedItemsAsync(message.SenderId);
                    message.UserEmoji = equipped.EmojiIcon;
                    message.SenderFrameColor = equipped.FrameColor ?? "#457b9d";

                    if (message.IsFileMessage)
                    {
                        var fileData = ParseFileMessage(message.MessageText);
                        message.FileName = fileData.FileName;
                        message.FileType = fileData.FileType;
                        message.FileSize = fileData.FileSize;
                        message.FilePath = fileData.StorageDescriptor;
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    GroupedMessages.Clear();

                    var grouped = messages
                        .GroupBy(m => GetDateKey(m.SentAt))
                        .Select(g => new GroupedGroupMessages
                        {
                            Date = GetDateDisplay(g.Key),
                            Messages = new ObservableCollection<GroupChatMessage>(g.OrderBy(m => m.SentAt))
                        })
                        .OrderBy(g => DateTime.Parse(GetDateKeyFromDisplay(g.Date)))
                        .ToList();

                    foreach (var group in grouped)
                    {
                        GroupedMessages.Add(group);
                    }

                    var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                    if (loadingOverlay != null)
                        loadingOverlay.IsVisible = false;

                    // Прокручиваем вниз после загрузки
                    if (GroupedMessages.Count > 0)
                    {
                        ScrollToBottom();
                    }

                    _isFirstLoad = false;
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error") ?? "Error",
                    $"{_localizationService.GetText("FailedToLoadMessages") ?? "Failed to load messages"}: {ex.Message}",
                    _localizationService.GetText("OK") ?? "OK");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                    if (loadingOverlay != null)
                        loadingOverlay.IsVisible = false;
                });
            }
            finally
            {
                _isLoading = false;
            }
        }

        private string GetDateKey(DateTime date)
        {
            var today = DateTime.Today;
            if (date.Date == today)
                return "today";
            if (date.Date == today.AddDays(-1))
                return "yesterday";
            return date.ToString("yyyy-MM-dd");
        }

        private string GetDateDisplay(string key)
        {
            return key switch
            {
                "today" => _localizationService?.CurrentLanguage == "ru" ? "Сегодня" : "Today",
                "yesterday" => _localizationService?.CurrentLanguage == "ru" ? "Вчера" : "Yesterday",
                _ => DateTime.Parse(key).ToString("dd MMMM yyyy")
            };
        }

        private async Task MarkMessagesAsRead()
        {
            try
            {
                await _dbService.MarkMessagesAsReadAsync(_group.GroupId, _user.UserId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка отметки сообщений: {ex.Message}");
            }
        }

        private void UpdateOnlineStatus()
        {
            var random = new Random();
            int onlineCount = random.Next(1, Math.Max(2, _group.StudentCount));
            OnlineStatus = $"{onlineCount} онлайн, {_group.StudentCount} участников";
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
            var messageEntry = this.FindByName<Entry>("MessageEntry");
            if (messageEntry == null) return;

            var messageText = messageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(messageText)) return;

            try
            {
                messageEntry.IsEnabled = false;
                var originalText = messageEntry.Text;
                messageEntry.Text = "";

                // Добавляем сообщение в UI сразу для мгновенного отображения
                var tempMessage = new GroupChatMessage
                {
                    MessageId = -DateTime.Now.Millisecond, // временный ID
                    SenderId = _user.UserId,
                    SenderName = $"{_user.FirstName} {_user.LastName}",
                    SenderAvatar = _user.AvatarUrl ?? "default_avatar.png",
                    MessageText = messageText,
                    SentAt = DateTime.UtcNow,
                    IsMyMessage = true,
                    IsDelivered = false,
                    IsRead = false
                };

                // Добавляем в группу
                var dateKey = GetDateKey(DateTime.UtcNow);
                var dateDisplay = GetDateDisplay(dateKey);
                var group = GroupedMessages.FirstOrDefault(g => g.Date == dateDisplay);

                if (group == null)
                {
                    group = new GroupedGroupMessages
                    {
                        Date = dateDisplay,
                        Messages = new ObservableCollection<GroupChatMessage>()
                    };
                    GroupedMessages.Add(group);

                    // Пересортируем группы
                    var sortedGroups = GroupedMessages
                        .OrderBy(g => DateTime.Parse(GetDateKeyFromDisplay(g.Date)))
                        .ToList();

                    GroupedMessages.Clear();
                    foreach (var g in sortedGroups)
                    {
                        GroupedMessages.Add(g);
                    }

                    group = GroupedMessages.First(g => g.Date == dateDisplay);
                }

                group.Messages.Add(tempMessage);

                // Прокручиваем вниз сразу
                ScrollToBottom();

                bool success = await _dbService.SendGroupChatMessageAsync(_group.GroupId, _user.UserId, messageText);

                if (success)
                {
                    // Удаляем временное сообщение и загружаем реальное
                    group.Messages.Remove(tempMessage);
                    await RefreshMessages();
                    await SendTypingStatus(false);

                    // Снова прокручиваем вниз после загрузки
                    ScrollToBottom();
                }
                else
                {
                    // Если ошибка, возвращаем текст и удаляем временное сообщение
                    group.Messages.Remove(tempMessage);
                    messageEntry.Text = originalText;
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Error",
                        _localizationService.GetText("MessageSendFailed") ?? "Failed to send message",
                        _localizationService.GetText("OK") ?? "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error") ?? "Error",
                    ex.Message, _localizationService.GetText("OK") ?? "OK");
            }
            finally
            {
                if (messageEntry != null)
                {
                    messageEntry.IsEnabled = true;
                    messageEntry.Focus();
                }
            }
        }

        private async void OnMessageTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isTyping && !string.IsNullOrEmpty(e.NewTextValue))
            {
                _isTyping = true;
                await SendTypingStatus(true);

                _typingTimer?.Stop();
                _typingTimer = new System.Timers.Timer(3000);
                _typingTimer.Elapsed += async (s, ev) => await OnTypingTimeout();
                _typingTimer.Start();
            }
        }

        private async Task OnTypingTimeout()
        {
            _isTyping = false;
            await SendTypingStatus(false);
            _typingTimer?.Stop();
        }

        private async Task SendTypingStatus(bool isTyping)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var typingIndicator = this.FindByName<Border>("TypingIndicator");
                var typingLabel = this.FindByName<Label>("TypingLabel");

                if (isTyping && typingIndicator != null && typingLabel != null)
                {
                    typingLabel.Text = $"{_localizationService?.GetText("SomeoneTyping") ?? "Кто-то печатает..."}";
                    typingIndicator.IsVisible = true;
                }
                else if (typingIndicator != null)
                {
                    typingIndicator.IsVisible = false;
                }
            });
        }

        private async void OnAttachMenuClicked(object sender, EventArgs e)
        {
            var action = await DisplayActionSheet(
                _localizationService.GetText("AttachFile") ?? "Прикрепить файл",
                _localizationService.GetText("Cancel") ?? "Отмена",
                null,
                "📷 " + (_localizationService.GetText("Photo") ?? "Фото"),
                "📎 " + (_localizationService.GetText("Document") ?? "Документ"),
                "🗜️ " + (_localizationService.GetText("Archive") ?? "Архив"));

            if (action?.Contains("Фото") == true || action?.Contains("Photo") == true)
                await PickImage();
            else if (action?.Contains("Документ") == true || action?.Contains("Document") == true)
                await PickDocument();
            else if (action?.Contains("Архив") == true || action?.Contains("Archive") == true)
                await PickArchive();
        }

        private async Task PickImage()
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = _localizationService.GetText("SelectPhoto") ?? "Выберите фото"
                });

                if (result != null)
                {
                    await SendFileAsync(result);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                    ex.Message, _localizationService.GetText("OK") ?? "OK");
            }
        }

        private async Task PickDocument()
        {
            try
            {
                var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".doc", ".docx", ".pdf", ".txt", ".xls", ".xlsx", ".ppt", ".pptx" } },
                    { DevicePlatform.Android, new[] { "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/pdf", "text/plain" } },
                    { DevicePlatform.iOS, new[] { "public.content" } },
                });

                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = _localizationService.GetText("SelectDocument") ?? "Выберите документ",
                    FileTypes = fileTypes
                });

                if (result != null)
                {
                    await SendFileAsync(result);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                    ex.Message, _localizationService.GetText("OK") ?? "OK");
            }
        }

        private async Task PickArchive()
        {
            try
            {
                var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".zip", ".rar", ".7z", ".tar", ".gz" } },
                    { DevicePlatform.Android, new[] { "application/zip", "application/x-rar-compressed", "application/x-tar", "application/gzip" } },
                    { DevicePlatform.iOS, new[] { "public.archive" } },
                });

                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = _localizationService.GetText("SelectArchive") ?? "Выберите архив",
                    FileTypes = fileTypes
                });

                if (result != null)
                {
                    await SendFileAsync(result);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                    ex.Message, _localizationService.GetText("OK") ?? "OK");
            }
        }

        private async Task SendFileAsync(FileResult fileResult)
        {
            try
            {
                var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                var loadingLabel = this.FindByName<Label>("LoadingLabel");

                if (loadingOverlay != null)
                {
                    loadingOverlay.IsVisible = true;
                    if (loadingLabel != null)
                        loadingLabel.Text = "Отправка файла...";
                }

                byte[] fileBytes;
                using (var stream = await fileResult.OpenReadAsync())
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                if (fileBytes.Length > 50 * 1024 * 1024)
                {
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        _localizationService.GetText("FileTooLarge") ?? "Файл слишком большой (максимум 50 МБ)",
                        _localizationService.GetText("OK") ?? "OK");
                    return;
                }

                var fileSize = FormatFileSize(fileBytes.Length);
                var mimeType = GetMimeType(fileResult.FileName);
                var base64Payload = Convert.ToBase64String(fileBytes);
                var fileType = Path.GetExtension(fileResult.FileName).ToLower();

                var message = $"[FILE]|BASE64|{mimeType}|{base64Payload}|{fileResult.FileName}|{fileSize}|{fileType}";

                // Добавляем временное сообщение о файле
                var tempMessage = new GroupChatMessage
                {
                    MessageId = -DateTime.Now.Millisecond,
                    SenderId = _user.UserId,
                    SenderName = $"{_user.FirstName} {_user.LastName}",
                    SenderAvatar = _user.AvatarUrl ?? "default_avatar.png",
                    MessageText = message,  // Это автоматически установит IsFileMessage = true через вычисляемое свойство
                    SentAt = DateTime.UtcNow,
                    IsMyMessage = true,
                    // НЕ устанавливаем IsFileMessage - оно вычисляется из MessageText
                    FileName = fileResult.FileName,
                    FileType = fileType,
                    FileSize = fileSize,
                    IsDelivered = false
                };

                var dateKey = GetDateKey(DateTime.UtcNow);
                var dateDisplay = GetDateDisplay(dateKey);
                var group = GroupedMessages.FirstOrDefault(g => g.Date == dateDisplay);

                if (group == null)
                {
                    group = new GroupedGroupMessages
                    {
                        Date = dateDisplay,
                        Messages = new ObservableCollection<GroupChatMessage>()
                    };
                    GroupedMessages.Add(group);
                }

                group.Messages.Add(tempMessage);
                ScrollToBottom();

                var success = await _dbService.SendGroupChatMessageAsync(_group.GroupId, _user.UserId, message);

                if (success)
                {
                    group.Messages.Remove(tempMessage);
                    await RefreshMessages();
                    ScrollToBottom();
                }
                else
                {
                    group.Messages.Remove(tempMessage);
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        _localizationService.GetText("FailedToSendFile") ?? "Не удалось отправить файл",
                        _localizationService.GetText("OK") ?? "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                    ex.Message, _localizationService.GetText("OK") ?? "OK");
            }
            finally
            {
                var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                var loadingLabel = this.FindByName<Label>("LoadingLabel");

                if (loadingOverlay != null)
                {
                    loadingOverlay.IsVisible = false;
                    if (loadingLabel != null)
                        loadingLabel.Text = "Загрузка сообщений...";
                }
            }
        }

        private string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                ".tar" => "application/x-tar",
                ".gz" => "application/gzip",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        private async void OnEmojiClicked(object sender, EventArgs e)
        {
            var emojis = new[] { "😊", "😂", "❤️", "👍", "🔥", "🎉", "😢", "🤔", "👏", "🙏", "😎", "🥳", "😍", "🤯", "😭" };
            var selected = await DisplayActionSheet(
                _localizationService.GetText("ChooseEmoji") ?? "Выберите эмодзи",
                _localizationService.GetText("Cancel") ?? "Отмена",
                null,
                emojis);

            if (selected != null && selected != _localizationService.GetText("Cancel"))
            {
                var messageEntry = this.FindByName<Entry>("MessageEntry");
                if (messageEntry != null)
                    messageEntry.Text += selected;
            }
        }

        private FileMessagePayload ParseFileMessage(string messageText)
        {
            try
            {
                var parts = messageText.Split('|');
                if (parts.Length >= 7 && parts[0] == "[FILE]" && parts[1].Equals("BASE64", StringComparison.OrdinalIgnoreCase))
                {
                    return new FileMessagePayload
                    {
                        StorageDescriptor = $"data:{parts[2]};base64,{parts[3]}",
                        FileName = parts[4],
                        FileSize = parts[5],
                        FileType = parts[6],
                        MimeType = parts[2]
                    };
                }

                if (parts.Length >= 5 && parts[0] == "[FILE]")
                {
                    return new FileMessagePayload
                    {
                        StorageDescriptor = parts[1],
                        FileName = parts[2],
                        FileSize = parts[3],
                        FileType = parts[4],
                        MimeType = GetMimeType(parts[2])
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка парсинга: {ex.Message}");
            }

            return new FileMessagePayload();
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async Task<string> GetUserAvatarAsync(int userId)
        {
            if (_avatarCache.TryGetValue(userId, out var cached))
                return cached;

            try
            {
                var avatarData = await _dbService.GetUserAvatarAsync(userId);
                if (string.IsNullOrEmpty(avatarData))
                    avatarData = "default_avatar.png";

                _avatarCache[userId] = avatarData;
                return avatarData;
            }
            catch
            {
                return "default_avatar.png";
            }
        }

        private async void OnFileMessageTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is GroupChatMessage message && message.IsFileMessage)
            {
                try
                {
                    var fileData = ParseFileMessage(message.MessageText);
                    var filePath = await _fileService.ResolveFilePath(fileData.StorageDescriptor, fileData.FileName, "ChatFiles");

                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    {
                        await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                            _localizationService.GetText("FileNotFound") ?? "Файл не найден",
                            _localizationService.GetText("OK") ?? "OK");
                        return;
                    }

                    var action = await DisplayActionSheet(
                        $"{_localizationService.GetText("File") ?? "Файл"}: {fileData.FileName} ({fileData.FileSize})",
                        _localizationService.GetText("Cancel") ?? "Отмена",
                        null,
                        $"📥 {_localizationService.GetText("Download") ?? "Скачать"}",
                        $"📁 {_localizationService.GetText("Open") ?? "Открыть"}",
                        $"ℹ️ {_localizationService.GetText("Info") ?? "Информация"}");

                    if (action?.Contains("Скачать") == true || action?.Contains("Download") == true)
                    {
                        await DownloadFile(filePath, fileData.FileName);
                    }
                    else if (action?.Contains("Открыть") == true || action?.Contains("Open") == true)
                    {
                        await OpenFile(filePath);
                    }
                    else if (action?.Contains("Информация") == true || action?.Contains("Info") == true)
                    {
                        await ShowFileInfo(filePath, fileData);
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        ex.Message, _localizationService.GetText("OK") ?? "OK");
                }
            }
        }

        private async Task ShowFileInfo(string filePath, FileMessagePayload fileData)
        {
            var fileInfo = new FileInfo(filePath);
            var message = $"{_localizationService.GetText("FileName") ?? "Имя"}: {fileData.FileName}\n" +
                         $"{_localizationService.GetText("FileType") ?? "Тип"}: {fileData.FileType}\n" +
                         $"{_localizationService.GetText("FileSize") ?? "Размер"}: {fileData.FileSize}\n" +
                         $"{_localizationService.GetText("Path") ?? "Путь"}: {filePath}\n" +
                         $"{_localizationService.GetText("Created") ?? "Создан"}: {fileInfo.CreationTime:dd.MM.yyyy HH:mm}\n" +
                         $"{_localizationService.GetText("Modified") ?? "Изменен"}: {fileInfo.LastWriteTime:dd.MM.yyyy HH:mm}";

            await DisplayAlert(_localizationService.GetText("FileInfo") ?? "Информация о файле",
                message, _localizationService.GetText("OK") ?? "OK");
        }

        private async Task DownloadFile(string filePath, string fileName)
        {
            try
            {
                var success = await _fileService.DownloadFileAsync(filePath, fileName);
                if (success)
                {
                    await DisplayAlert(_localizationService.GetText("Success") ?? "Успех",
                        $"{_localizationService.GetText("FileDownloaded") ?? "Файл скачан"}: {fileName}",
                        _localizationService.GetText("OK") ?? "OK");
                }
                else
                {
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        _localizationService.GetText("FailedToDownload") ?? "Не удалось скачать файл",
                        _localizationService.GetText("OK") ?? "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                    $"{_localizationService.GetText("DownloadError") ?? "Ошибка скачивания"}: {ex.Message}",
                    _localizationService.GetText("OK") ?? "OK");
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
                await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                    $"{_localizationService.GetText("OpenError") ?? "Ошибка открытия"}: {ex.Message}",
                    _localizationService.GetText("OK") ?? "OK");
            }
        }

        private async void OnScrollToBottomClicked(object sender, EventArgs e)
        {
            ScrollToBottom();
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            string searchText = await DisplayPromptAsync(
                _localizationService.GetText("Search") ?? "Поиск",
                _localizationService.GetText("SearchInChat") ?? "Введите текст для поиска",
                keyboard: Keyboard.Text);

            if (!string.IsNullOrEmpty(searchText))
            {
                await SearchAndScrollToMessage(searchText);
            }
        }

        private async Task SearchAndScrollToMessage(string searchText)
        {
            try
            {
                var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                var loadingLabel = this.FindByName<Label>("LoadingLabel");

                if (loadingOverlay != null)
                {
                    loadingOverlay.IsVisible = true;
                    if (loadingLabel != null)
                        loadingLabel.Text = "Поиск...";
                }

                var allMessages = new List<GroupChatMessage>();
                foreach (var group in GroupedMessages)
                {
                    allMessages.AddRange(group.Messages);
                }

                var results = allMessages
                    .Where(m => !m.IsSystemMessage &&
                               m.MessageText.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(m => m.SentAt)
                    .ToList();

                if (results.Any())
                {
                    var options = results.Take(20).Select(m =>
                        $"{m.SenderName}: {m.MessageText.Substring(0, Math.Min(40, m.MessageText.Length))}...").ToArray();

                    var selected = await DisplayActionSheet(
                        $"Найдено {results.Count} сообщений",
                        "Отмена",
                        null,
                        options);

                    if (selected != null && selected != "Отмена")
                    {
                        var selectedMessage = results.FirstOrDefault(m =>
                            $"{m.SenderName}: {m.MessageText.Substring(0, Math.Min(40, m.MessageText.Length))}..." == selected);

                        if (selectedMessage != null)
                        {
                            await ScrollToMessage(selectedMessage);
                        }
                    }
                }
                else
                {
                    await DisplayAlert(
                        _localizationService.GetText("Search") ?? "Поиск",
                        _localizationService.GetText("NoResults") ?? "Ничего не найдено",
                        _localizationService.GetText("OK") ?? "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                    ex.Message, _localizationService.GetText("OK") ?? "OK");
            }
            finally
            {
                var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                var loadingLabel = this.FindByName<Label>("LoadingLabel");

                if (loadingOverlay != null)
                {
                    loadingOverlay.IsVisible = false;
                    if (loadingLabel != null)
                        loadingLabel.Text = "Загрузка сообщений...";
                }
            }
        }

        private async Task ScrollToMessage(GroupChatMessage targetMessage)
        {
            try
            {
                foreach (var group in GroupedMessages)
                {
                    var message = group.Messages.FirstOrDefault(m => m.MessageId == targetMessage.MessageId);
                    if (message != null)
                    {
                        // Сохраняем оригинальный цвет
                        var originalColor = message.BackgroundColor;

                        // Подсвечиваем желтым
                        message.BackgroundColor = Colors.Yellow;

                        var index = GroupedMessages.IndexOf(group);
                        if (index >= 0)
                        {
                            var messagesScrollView = this.FindByName<ScrollView>("MessagesScrollView");
                            if (messagesScrollView != null)
                            {
                                await messagesScrollView.ScrollToAsync(0, index * 150, true);
                            }
                        }

                        // Возвращаем оригинальный цвет через 2 секунды
                        await Task.Delay(2000);
                        message.BackgroundColor = originalColor;

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка прокрутки: {ex.Message}");
            }
        }

        private async void OnMembersClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GroupMembersPage(_group.GroupId, _user, _dbService, _settingsService));
        }

        private async void OnChangeGroupAvatarClicked(object sender, EventArgs e)
        {
            if (!IsTeacher) return;

            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = _localizationService.GetText("SelectGroupAvatar") ?? "Выберите аватар для группы",
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
                    var avatarUrl = await _dbService.SaveGroupAvatarAsync(_group.GroupId, stream, result.FileName, _user.UserId);

                    if (avatarUrl != null)
                    {
                        GroupAvatarUrl = avatarUrl;
                        await DisplayAlert(_localizationService.GetText("Success") ?? "Успех",
                            _localizationService.GetText("AvatarUpdated") ?? "Аватар группы обновлен",
                            _localizationService.GetText("OK") ?? "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                    ex.Message, _localizationService.GetText("OK") ?? "OK");
            }
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

    public class FileMessagePayload
    {
        public string StorageDescriptor { get; set; } = string.Empty;
        public string FileName { get; set; } = "Unknown file";
        public string FileType { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string MimeType { get; set; } = "application/octet-stream";
    }
}