using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace EducationalPlatform.Views
{
    public partial class GroupChatPage : ContentPage
    {
        private readonly StudyGroup _group;
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private readonly Dictionary<int, string> _avatarCache = new();
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
            _fileService = ServiceHelper.GetService<FileService>();

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
            _refreshTimer.Elapsed += async (s, e) => await RefreshMessages(); // Исправлено - добавлен async
            _refreshTimer.Start();
        }

        private async void LoadMessages()
        {
            try
            {
                // Показываем индикатор загрузки
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (this.FindByName<ActivityIndicator>("LoadingIndicator") is ActivityIndicator indicator)
                    {
                        indicator.IsVisible = true;
                        indicator.IsRunning = true;
                    }
                });

                var messages = await _dbService.GetGroupChatMessagesAsync(_group.GroupId);

                foreach (var message in messages)
                {
                    message.IsMyMessage = message.SenderId == _user.UserId;
                    
                    // Аватар, имя и эмодзи уже должны быть загружены из БД
                    // Обновляем аватар только если он не установлен или это default
                    if (string.IsNullOrEmpty(message.SenderAvatar) || message.SenderAvatar == "default_avatar.png")
                    {
                        message.SenderAvatar = await GetUserAvatarAsync(message.SenderId);
                    }
                    
                    // Эмодзи уже должно быть загружено из БД, но если нет - загружаем
                    if (string.IsNullOrEmpty(message.UserEmoji))
                    {
                        var equippedItems = await _dbService.GetEquippedItemsAsync(message.SenderId);
                        message.UserEmoji = equippedItems.EmojiIcon;
                    }
                    
                    // Убеждаемся, что SenderName установлен
                    if (string.IsNullOrEmpty(message.SenderName))
                    {
                        // Если имя не загружено из БД, загружаем отдельно
                        try
                        {
                            using var connection = new Microsoft.Data.SqlClient.SqlConnection(_dbService.ConnectionString);
                            await connection.OpenAsync();
                            var query = "SELECT FirstName + ' ' + LastName as FullName FROM Users WHERE UserId = @UserId";
                            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
                            command.Parameters.AddWithValue("@UserId", message.SenderId);
                            var result = await command.ExecuteScalarAsync();
                            message.SenderName = result?.ToString() ?? "Пользователь";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка загрузки имени пользователя: {ex.Message}");
                            message.SenderName = "Пользователь";
                        }
                    }

                    if (message.MessageText?.StartsWith("[FILE]") == true)
                    {
                        message.IsFileMessage = true;
                        var fileData = ParseFileMessage(message.MessageText);
                        message.FileName = fileData.FileName;
                        message.FileType = fileData.FileType;
                        message.FileSize = fileData.FileSize;
                        message.FilePath = fileData.StorageDescriptor;
                    }
                    else
                    {
                        message.IsFileMessage = false;
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (this.FindByName<ActivityIndicator>("LoadingIndicator") is ActivityIndicator indicator)
                    {
                        indicator.IsVisible = false;
                        indicator.IsRunning = false;
                    }

                    Messages.Clear();
                    foreach (var message in messages)
                    {
                        Messages.Add(message);
                    }

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
                    foreach (var message in newMessages)
                    {
                        message.IsMyMessage = message.SenderId == _user.UserId;
                        message.SenderAvatar = await GetUserAvatarAsync(message.SenderId);

                        if (message.MessageText?.StartsWith("[FILE]") == true)
                        {
                            message.IsFileMessage = true;
                            var fileData = ParseFileMessage(message.MessageText);
                            message.FileName = fileData.FileName;
                            message.FileType = fileData.FileType;
                            message.FileSize = fileData.FileSize;
                            message.FilePath = fileData.StorageDescriptor;
                        }
                        else
                        {
                            message.IsFileMessage = false;
                        }

                        var equippedItemsTask = _dbService.GetEquippedItemsAsync(message.SenderId);
                        _ = equippedItemsTask.ContinueWith(task =>
                        {
                            if (task.IsCompletedSuccessfully)
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    message.UserEmoji = task.Result.EmojiIcon;
                                });
                            }
                        });
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        foreach (var message in newMessages)
                        {
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
                Console.WriteLine($"📎 Начинаем выбор файла для отправки");
                
                // Расширяем поддерживаемые типы файлов для Android и iOS
                var fileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".zip", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".txt", ".xls", ".xlsx", ".jpg", ".png", ".mp4" } },
                        { DevicePlatform.macOS, new[] { ".zip", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".txt", ".xls", ".xlsx", ".jpg", ".png", ".mp4" } },
                        { DevicePlatform.Android, new[] { "*/*" } }, // Android поддерживает все типы
                        { DevicePlatform.iOS, new[] { "public.data" } } // iOS поддерживает все типы данных
                    });

                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите файл для отправки",
                    FileTypes = fileTypes
                });

                if (result != null)
                {
                    Console.WriteLine($"✅ Файл выбран: {result.FileName}, путь: {result.FullPath}");
                    await SendFileAsync(result);
                }
                else
                {
                    Console.WriteLine($"ℹ️ Выбор файла отменен");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка выбора файла: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Ошибка", $"Не удалось прикрепить файл: {ex.Message}", "OK");
            }
        }

        private async Task SendFileAsync(FileResult fileResult)
        {
            try
            {
                Console.WriteLine($"📎 Начинаем отправку файла: {fileResult.FileName}");
                
                // Читаем файл в байты для сохранения
                byte[] fileBytes;
                using (var stream = await fileResult.OpenReadAsync())
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                Console.WriteLine($"📦 Размер файла: {fileBytes.Length} байт");

                var fileSize = FormatFileSize(fileBytes.Length);
                var base64Payload = Convert.ToBase64String(fileBytes);
                var mimeType = _fileService.GetMimeType(Path.GetExtension(fileResult.FileName));

                // Новый формат: [FILE]|BASE64|mime|base64|originalName|size|extension
                var message = $"[FILE]|BASE64|{mimeType}|{base64Payload}|{fileResult.FileName}|{fileSize}|{Path.GetExtension(fileResult.FileName)}";

                Console.WriteLine($"💬 Отправляем сообщение с файлом: {message.Substring(0, Math.Min(100, message.Length))}...");

                var success = await _dbService.SendGroupChatMessageAsync(_group.GroupId, _user.UserId, message);
                if (success)
                {
                    Console.WriteLine($"✅ Файл успешно отправлен");
                    await RefreshMessages();
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось отправить сообщение в БД");
                    await DisplayAlert("Ошибка", "Не удалось отправить файл", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки файла: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Ошибка", $"Ошибка отправки файла: {ex.Message}", "OK");
            }
        }

        private FileMessagePayload ParseFileMessage(string messageText)
        {
            try
            {
                var parts = messageText.Split('|');
                if (parts.Length >= 7 && parts[0] == "[FILE]" && parts[1].Equals("BASE64", StringComparison.OrdinalIgnoreCase))
                {
                    var mime = parts[2];
                    var base64 = parts[3];
                    var fileName = parts[4];
                    var fileSize = parts[5];
                    var extension = parts[6];
                    var descriptor = $"data:{mime};base64,{base64}";

                    return new FileMessagePayload
                    {
                        StorageDescriptor = descriptor,
                        FileName = fileName,
                        FileType = extension,
                        FileSize = fileSize,
                        MimeType = mime
                    };
                }

                if (parts.Length >= 5 && parts[0] == "[FILE]")
                {
                    var relativePath = parts[1];

                    string normalizedPath;
                    if (Path.IsPathRooted(relativePath))
                    {
                        normalizedPath = relativePath;
                    }
                    else
                    {
                        normalizedPath = Path.Combine(FileSystem.AppDataDirectory, relativePath);
                    }

                    normalizedPath = Path.GetFullPath(normalizedPath);

                    return new FileMessagePayload
                    {
                        StorageDescriptor = normalizedPath,
                        FileName = parts[2],
                        FileSize = parts[3],
                        FileType = parts[4],
                        MimeType = _fileService.GetMimeType(parts[4])
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка парсинга файлового сообщения: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return new FileMessagePayload();
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes >= 1 << 30) return $"{(bytes / (1 << 30)):F1} GB";
            if (bytes >= 1 << 20) return $"{(bytes / (1 << 20)):F1} MB";
            if (bytes >= 1 << 10) return $"{(bytes / (1 << 10)):F1} KB";
            return $"{bytes} B";
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
                Console.WriteLine($"❌ Ошибка загрузки аватара {userId}: {ex.Message}");
                return "default_avatar.png";
            }
        }

        private async void OnFileMessageTapped(object sender, TappedEventArgs e)
        {
            try
            {
                GroupChatMessage? message = null;
                
                if (e.Parameter is GroupChatMessage msg)
                {
                    message = msg;
                }
                else if (sender is Border border && border.BindingContext is GroupChatMessage borderMsg)
                {
                    message = borderMsg;
                }

                if (message == null || !message.IsFileMessage)
                {
                    Console.WriteLine("⚠️ Сообщение не является файловым или не найдено");
                    return;
                }

                Console.WriteLine($"🎯 Тап по файловому сообщению: {message.FileName}");

                // Всегда парсим заново, чтобы получить актуальный путь
                var fileData = ParseFileMessage(message.MessageText);
                string fileDescriptor = fileData.StorageDescriptor;
                string fileName = fileData.FileName;
                string fileType = fileData.FileType;
                string fileSize = fileData.FileSize;
                
                // Восстанавливаем фактический путь к файлу
                var filePath = await _fileService.ResolveFilePath(fileDescriptor, fileName, "ChatFiles");

                // Обновляем данные в сообщении
                message.FilePath = filePath;
                message.FileName = fileName;
                message.FileType = fileType;
                message.FileSize = fileSize;

                Console.WriteLine($"📁 Данные файла: {fileName}, путь: {filePath}");

                // Проверяем существование файла
                bool fileExists = File.Exists(filePath);
                Console.WriteLine($"📂 Файл существует: {fileExists}");

                if (!fileExists)
                {
                    // Пробуем найти файл в других возможных местах
                    var alternativePaths = new List<string>
                    {
                        Path.Combine(FileSystem.AppDataDirectory, "Documents", fileName),
                        Path.Combine(FileSystem.AppDataDirectory, fileName),
                        filePath
                    };

                    bool found = false;
                    foreach (var altPath in alternativePaths)
                    {
                        if (File.Exists(altPath))
                        {
                            filePath = altPath;
                            fileExists = true;
                            found = true;
                            Console.WriteLine($"✅ Файл найден по альтернативному пути: {altPath}");
                            break;
                        }
                    }

                    if (!found)
                    {
                        await DisplayAlert("Ошибка", 
                            $"Файл не найден.\n\n" +
                            $"Имя: {fileName}\n" +
                            $"Ожидаемый путь: {filePath}\n\n" +
                            $"Файл мог быть удален или находится на другом устройстве.", 
                            "OK");
                        return;
                    }
                }

                var action = await DisplayActionSheet(
                    $"Файл: {fileName} ({fileSize})",
                    "Отмена",
                    null,
                    "📥 Скачать файл",
                    "📁 Открыть файл",
                    "🔍 Информация о файле");

                if (action == "📥 Скачать файл")
                {
                    await DownloadFile(filePath, fileName);
                }
                else if (action == "📁 Открыть файл")
                {
                    await OpenFile(filePath);
                }
                else if (action == "🔍 Информация о файле")
                {
                    await DisplayAlert("Информация о файле",
                        $"Имя: {fileName}\n" +
                        $"Тип: {fileType}\n" +
                        $"Размер: {fileSize}\n" +
                        $"Путь: {filePath}\n" +
                        $"Существует: {fileExists}", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки файла: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Ошибка", $"Не удалось обработать файл: {ex.Message}", "OK");
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

        private class FileMessagePayload
        {
            public string StorageDescriptor { get; set; } = string.Empty;
            public string FileName { get; set; } = "Неизвестный файл";
            public string FileType { get; set; } = string.Empty;
            public string FileSize { get; set; } = string.Empty;
            public string MimeType { get; set; } = "application/octet-stream";
        }
    }
}