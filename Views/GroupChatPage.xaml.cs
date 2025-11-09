using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class GroupChatPage : ContentPage
    {
        private readonly StudyGroup _group;
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private System.Timers.Timer? _refreshTimer;

        public ObservableCollection<GroupChatMessage> Messages { get; } = new ObservableCollection<GroupChatMessage>();
        public new string Title => $"Chat: {_group.GroupName}";

        // Поддерживаемые форматы файлов
        private readonly FilePickerFileType _supportedFileTypes = new(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".zip", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".txt", ".xls", ".xlsx", ".jpg", ".png" } },
                { DevicePlatform.macOS, new[] { ".zip", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".txt", ".xls", ".xlsx", ".jpg", ".png" } }
            });

        public GroupChatPage(StudyGroup group, User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();

            _group = group;
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _fileService = new FileService();

            BindingContext = this;

            LoadMessages();
            StartAutoRefresh();
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
        }

        private void StartAutoRefresh()
        {
            _refreshTimer = new System.Timers.Timer(3000);
            _refreshTimer.Elapsed += async (s, e) => await RefreshMessages();
            _refreshTimer.Start();
        }

        private async void LoadMessages()
        {
            try
            {
                var messages = await _dbService.GetGroupChatMessagesAsync(_group.GroupId);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Messages.Clear();
                    foreach (var message in messages)
                    {
                        message.IsMyMessage = message.SenderId == _user.UserId;

                        // Загружаем аватар
                        if (message.IsMyMessage)
                        {
                            message.SenderAvatar = _user.AvatarUrl ?? "default_avatar.png";
                        }
                        else if (string.IsNullOrEmpty(message.SenderAvatar))
                        {
                            message.SenderAvatar = "default_avatar.png";
                        }

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

                    // Прокрутка к последнему сообщению
                    if (Messages.Count > 0)
                    {
                        MessagesCollectionView.ScrollTo(Messages.Count - 1, position: ScrollToPosition.End, animate: false);
                    }
                });
            }
            catch (Exception ex)
            {
                var localizationService = new LocalizationService();
                localizationService.SetLanguage(_settingsService?.CurrentLanguage ?? "en");
                await DisplayAlert(localizationService.GetText("error"), $"Failed to load messages: {ex.Message}", "OK");
            }
        }

        private async Task RefreshMessages()
        {
            try
            {
                var messages = await _dbService.GetGroupChatMessagesAsync(_group.GroupId);
                var newMessages = messages.Where(m => !Messages.Any(existing => existing.MessageId == m.MessageId)).ToList();

                if (newMessages.Any())
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        foreach (var message in newMessages)
                        {
                            message.IsMyMessage = message.SenderId == _user.UserId;

                            if (message.IsMyMessage)
                            {
                                message.SenderAvatar = _user.AvatarUrl ?? "default_avatar.png";
                            }
                            else if (string.IsNullOrEmpty(message.SenderAvatar))
                            {
                                message.SenderAvatar = "default_avatar.png";
                            }

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

                        if (Messages.Count > 0)
                        {
                            MessagesCollectionView.ScrollTo(Messages.Count - 1, position: ScrollToPosition.End, animate: true);
                        }
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
                await _dbService.MarkMessagesAsReadAsync(_group.GroupId, _user.UserId);
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
            if (string.IsNullOrEmpty(messageText)) return;

            try
            {
                bool success = await _dbService.SendGroupChatMessageAsync(_group.GroupId, _user.UserId, messageText);
                
                if (success)
                {
                    MessageEntry.Text = string.Empty;
                    await RefreshMessages();
                }
                else
                {
                    var localizationService = new LocalizationService();
                    localizationService.SetLanguage(_settingsService?.CurrentLanguage ?? "en");
                    await DisplayAlert(localizationService.GetText("error"), localizationService.GetText("message_send_failed") ?? "Failed to send message", "OK");
                }
            }
            catch (Exception ex)
            {
                var localizationService = new LocalizationService();
                localizationService.SetLanguage(_settingsService?.CurrentLanguage ?? "en");
                await DisplayAlert(localizationService.GetText("error"), $"{localizationService.GetText("error")}: {ex.Message}", "OK");
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

                    // Формат: [FILE]|путь|оригинальное_имя|размер|тип
                    var message = $"[FILE]|{savedPath}|{fileResult.FileName}|{fileSize}|{Path.GetExtension(fileResult.FileName)}";

                    var success = await _dbService.SendGroupChatMessageAsync(_group.GroupId, _user.UserId, message);
                    if (success)
                    {
                        await DisplayAlert("Успех", "Файл отправлен", "OK");
                        await RefreshMessages();
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
                    Console.WriteLine($"🎯 Тап по файловому сообщению: {message.FileName}");

                    var fileData = ParseFileMessage(message.MessageText);
                    Console.WriteLine($"📁 Данные файла: {fileData.FileName}, путь: {fileData.FilePath}");

                    // Проверяем существование файла
                    bool fileExists = File.Exists(fileData.FilePath);
                    Console.WriteLine($"📂 Файл существует: {fileExists}");

                    var action = await DisplayActionSheet(
                        $"File: {fileData.FileName} ({fileData.FileSize})",
                        "Cancel",
                        null,
                        "📥 Download file",
                        "📁 Open file",
                        "🔍 Show file info");

                    if (action == "📥 Download file")
                    {
                        await DownloadFile(fileData.FilePath, fileData.FileName);
                    }
                    else if (action == "📁 Open file")
                    {
                        await OpenFile(fileData.FilePath);
                    }
                    else if (action == "🔍 Show file info")
                    {
                        await DisplayAlert("File Info",
                            $"Name: {fileData.FileName}\n" +
                            $"Type: {fileData.FileType}\n" +
                            $"Size: {fileData.FileSize}\n" +
                            $"Path: {fileData.FilePath}\n" +
                            $"Exists: {fileExists}", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка обработки файла: {ex.Message}");
                    await DisplayAlert("Error", $"Failed to process file: {ex.Message}", "OK");
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

        private async void OnMembersClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Участники", $"В группе {_group.StudentCount} участников", "OK");
        }
    }
}