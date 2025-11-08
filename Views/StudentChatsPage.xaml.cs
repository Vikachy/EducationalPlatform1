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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
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

        private async void LoadAllChats()
        {
            try
            {
                AllChats.Clear();
                IsBusy = true;

                Console.WriteLine($"🔍 Загружаем чаты для студента {_currentUser.UserId}");

                // ВРЕМЕННО КОММЕНТИРУЕМ - вызывает ошибку
                // await _dbService.CheckChatTableStructure();

                var chats = await _dbService.GetStudentAllChatsAsync(_currentUser.UserId);

                Console.WriteLine($"📊 Получено чатов: {chats?.Count ?? 0}");

                if (chats == null || !chats.Any())
                {
                    Console.WriteLine("ℹ️ Чатов не найдено, проверяем группы студента...");
                    await CheckStudentGroups();
                    return;
                }

                foreach (var chat in chats)
                {
                    Console.WriteLine($"💬 Добавляем чат: {chat.ChatName}, ID: {chat.ChatId}");
                    AllChats.Add(chat);
                }

                OnPropertyChanged(nameof(AllChats));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки чатов: {ex.Message}");
                await DisplayAlert("Ошибка", $"Не удалось загрузить чаты: {ex.Message}", "OK");
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
                command.Parameters.AddWithValue("@StudentId", _currentUser.UserId); // ← ЭТО int

                using var reader = await command.ExecuteReaderAsync();

                int groupCount = 0;
                while (await reader.ReadAsync())
                {
                    groupCount++;
                    // ЭТА СТРОКА 156 - она корректна
                    Console.WriteLine($"👥 Студент состоит в группе: {reader.GetString("GroupName")} (Курс: {reader.GetString("CourseName")})");
                }

                if (groupCount == 0)
                {
                    await DisplayAlert("Информация", "Вы не состоите ни в одной активной группе", "OK");
                }
                else
                {
                    await DisplayAlert("Диагностика",
                        $"Вы состоите в {groupCount} группах, но чаты не найдены. " +
                        "Обратитесь к преподавателю для настройки чатов.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка проверки групп: {ex.Message}");
            }
        }

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
                    await MarkMessagesAsRead(selectedChat);

                    // Обновляем счетчик непрочитанных
                    selectedChat.UnreadMessages = 0;

                    // Запускаем автообновление
                    StartAutoRefresh(selectedChat);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть чат: {ex.Message}", "OK");
                }
            }

            // Сбрасываем выделение
            if (sender is CollectionView collectionView)
            {
                collectionView.SelectedItem = null;
            }
        }

        private void UpdateChatHeader(StudentChatItem chat)
        {
            switch (chat.ChatType)
            {
                case "group":
                    ChatTitleLabel.Text = chat.ChatName;
                    ChatSubtitleLabel.Text = $"{chat.CourseName} • Участников: {chat.ParticipantCount}";
                    break;
                case "teacher":
                    ChatTitleLabel.Text = chat.ChatName;
                    ChatSubtitleLabel.Text = chat.TeacherSubject ?? "Преподаватель";
                    break;
                case "support":
                    ChatTitleLabel.Text = chat.ChatName;
                    ChatSubtitleLabel.Text = chat.Description;
                    break;
            }
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
                            messages = groupMessages.Select(m => new ChatMessage
                            {
                                MessageId = m.MessageId,
                                SenderId = m.SenderId,
                                MessageText = m.MessageText,
                                SentAt = m.SentAt,
                                IsRead = m.IsRead,
                                SenderName = m.SenderName,
                                SenderAvatar = m.SenderAvatar,
                                IsMyMessage = m.SenderId == _currentUser.UserId
                            }).ToList();
                        }
                        break;
                    case "teacher":
                        if (chat.TeacherId.HasValue)
                        {
                            var privateMessages = await _dbService.GetPrivateChatMessagesAsync(_currentUser.UserId, chat.TeacherId.Value);
                            messages = privateMessages.Select(m => new ChatMessage
                            {
                                MessageId = m.MessageId,
                                SenderId = m.SenderId,
                                MessageText = m.MessageText,
                                SentAt = m.SentAt,
                                IsRead = m.IsRead,
                                SenderName = m.SenderName,
                                SenderAvatar = m.SenderAvatar,
                                IsMyMessage = m.SenderId == _currentUser.UserId
                            }).ToList();
                        }
                        break;
                    case "support":
                        // TODO: Добавить метод для загрузки сообщений поддержки
                        messages = new List<ChatMessage>();
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
                await DisplayAlert("Ошибка", $"Ошибка загрузки сообщений: {ex.Message}", "OK");
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
                    await DisplayAlert("Ошибка", "Не удалось отправить сообщение", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка отправки: {ex.Message}", "OK");
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
                await LoadChatMessages(chat);
                await MarkMessagesAsRead(chat);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка обновления сообщений: {ex.Message}");
            }
        }

        private async Task RefreshChatList()
        {
            try
            {
                var chats = await _dbService.GetStudentAllChatsAsync(_currentUser.UserId);
                if (chats == null) return;

                // Обновляем существующие чаты
                foreach (var existingChat in AllChats.ToList())
                {
                    var updatedChat = chats.FirstOrDefault(c => c.ChatId == existingChat.ChatId && c.ChatType == existingChat.ChatType);
                    if (updatedChat != null)
                    {
                        existingChat.LastMessage = updatedChat.LastMessage;
                        existingChat.LastMessageTime = updatedChat.LastMessageTime;
                        existingChat.UnreadMessages = updatedChat.UnreadMessages;
                    }
                }
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
                    PickerTitle = "Выберите файл для отправки",
                    FileTypes = FilePickerFileType.Images // Можно расширить для других типов
                });

                if (result != null)
                {
                    // TODO: Реализовать отправку файлов
                    await DisplayAlert("Информация", $"Файл {result.FileName} выбран для отправки", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось прикрепить файл: {ex.Message}", "OK");
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
                await DisplayAlert("Ошибка", $"Не удалось открыть мои курсы: {ex.Message}", "OK");
            }
        }
    }
}