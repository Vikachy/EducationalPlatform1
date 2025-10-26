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
        private readonly LocalizationService _localizationService;
        private readonly StudyGroup _currentGroup;
        private readonly Timer _refreshTimer;

        public ObservableCollection<GroupChatMessage> Messages { get; set; } = new();
        public string GroupName { get; set; }
        public int OnlineCount { get; set; }

        public ChatPage(StudyGroup group, User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = ServiceHelper.GetService<LocalizationService>();
            _currentGroup = group;

            BindingContext = this;
            GroupName = group.GroupName;
            OnlineCount = group.StudentCount;

            // Настраиваем автообновление сообщений каждые 5 секунд
            _refreshTimer = new Timer(RefreshMessages, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            LoadMessages();
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
                    Messages.Add(message);
                }

                // Прокручиваем к последнему сообщению, если есть новые
                if (newMessages.Count > 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        MessagesCollectionView.ScrollTo(Messages[^1], position: ScrollToPosition.End, animate: true);
                    });
                }
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
                var result = await FilePicker.Default.PickAsync();
                if (result != null)
                {
                    await DisplayAlert("Информация", "Функция отправки файлов будет добавлена в следующих версиях", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка выбора файла: {ex.Message}", "OK");
            }
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