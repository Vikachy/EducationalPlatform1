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
        private System.Timers.Timer _refreshTimer;

        public ObservableCollection<GroupChatMessage> Messages { get; } = new ObservableCollection<GroupChatMessage>();

        public GroupChatPage(StudyGroup group, User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();

            _group = group;
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;

            Title = $"Чат: {group.GroupName}";
            BindingContext = this;

            LoadMessages();
            StartAutoRefresh();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
        }

        private void StartAutoRefresh()
        {
            _refreshTimer = new System.Timers.Timer(3000); // Обновление каждые 3 секунды
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
                await DisplayAlert("Ошибка", $"Не удалось загрузить сообщения: {ex.Message}", "OK");
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
                            Messages.Add(message);
                        }

                        if (Messages.Count > 0)
                        {
                            MessagesCollectionView.ScrollTo(Messages.Count - 1, position: ScrollToPosition.End, animate: true);
                        }
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
            try
            {
                var result = await FilePicker.Default.PickAsync();
                if (result != null)
                {
                    // Здесь можно добавить логику обработки файла
                    await DisplayAlert("Файл", $"Выбран файл: {result.FileName}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось прикрепить файл: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}