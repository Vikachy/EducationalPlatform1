using System.Collections.ObjectModel;
using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class ChatPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private readonly StudyGroup _currentGroup;
        private readonly Timer _refreshTimer;

        public ObservableCollection<GroupChatMessage> Messages { get; set; } = new();
        public string GroupName => _currentGroup.GroupName;
        public int OnlineCount => _currentGroup.StudentCount;

        // Поддерживаемые форматы файлов
        private readonly FilePickerFileType _supportedFileTypes = new(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".zip", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".txt", ".xls", ".xlsx", ".jpg", ".png" } },
                { DevicePlatform.macOS, new[] { ".zip", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".txt", ".xls", ".xlsx", ".jpg", ".png" } }
            });

        public ChatPage(StudyGroup group, User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _fileService = new FileService();
            _currentGroup = group;

            BindingContext = this;

            // Настраиваем автообновление сообщений каждые 5 секунд
            _refreshTimer = new Timer(RefreshMessages, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            LoadMessages();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            MarkMessagesAsRead();
        }

        private async void LoadMessages()
        {
            try
            {
                var messages = await _dbService.GetGroupChatMessagesAsync(_currentGroup.GroupId);
                Messages.Clear();
                foreach (var message in messages)
                {
                    message.IsMyMessage = message.SenderId == _currentUser.UserId;

                    // Парсим файловые сообщения
                    if (message.MessageText?.StartsWith("[FILE]") == true)
                    {
                        message.IsFileMessage = true;
                        var fileData = ParseFileMessage(message.MessageText);
                        message.FileName = fileData.FileName;
                        message.FileType = fileData.FileType;
                        message.FileSize = fileData.FileSize;
                    }

                    Messages.Add(message);
                }

                // Прокручиваем к последнему сообщению
                if (Messages.Count > 0)
                {
                    MessagesCollectionView.ScrollTo(Messages[^1], position: ScrollToPosition.End, animate: true);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка загрузки сообщений: {ex.Message}", "OK");
            }
        }

        private async void RefreshMessages(object? state)
        {
            try
            {
                var messages = await _dbService.GetGroupChatMessagesAsync(_currentGroup.GroupId);
                var newMessages = messages.Where(m => !Messages.Any(existing => existing.MessageId == m.MessageId)).ToList();

                foreach (var message in newMessages)
                {
                    message.IsMyMessage = message.SenderId == _currentUser.UserId;

                    if (message.MessageText?.StartsWith("[FILE]") == true)
                    {
                        message.IsFileMessage = true;
                        var fileData = ParseFileMessage(message.MessageText);
                        message.FileName = fileData.FileName;
                        message.FileType = fileData.FileType;
                        message.FileSize = fileData.FileSize;
                    }

                    Messages.Add(message);
                }

                // Прокручиваем к последнему сообщению, если есть новые
                if (newMessages.Count > 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        MessagesCollectionView.ScrollTo(Messages[^1], position: ScrollToPosition.End, animate: true);
                    });

                    await MarkMessagesAsRead();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления сообщений: {ex.Message}");
            }
        }

        private async Task MarkMessagesAsRead()
        {
            try
            {
                await _dbService.MarkMessagesAsReadAsync(_currentGroup.GroupId, _currentUser.UserId);
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
            var messageText = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(messageText))
                return;

            try
            {
                var success = await _dbService.SendGroupChatMessageAsync(_currentGroup.GroupId, _currentUser.UserId, messageText);
                if (success)
                {
                    MessageEntry.Text = string.Empty;
                    LoadMessages();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось отправить сообщение", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка отправки сообщения: {ex.Message}", "OK");
            }
        }

        private async void OnAttachFileClicked(object sender, EventArgs e)
        {
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
                await DisplayAlert("Ошибка", $"Ошибка выбора файла: {ex.Message}", "OK");
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

                    var ok = await _dbService.SendGroupChatMessageAsync(_currentGroup.GroupId, _currentUser.UserId, message);
                    if (ok)
                    {
                        await DisplayAlert("Файл", "Файл отправлен", "OK");
                        LoadMessages();
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

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _refreshTimer?.Dispose();
        }

        protected override bool OnBackButtonPressed()
        {
            _refreshTimer?.Dispose();
            return base.OnBackButtonPressed();
        }
    }
}