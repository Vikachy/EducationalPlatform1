using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class SupportPage : ContentPage
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;

        public SupportPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
        }

        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            if (TicketTypePicker.SelectedItem == null)
            {
                await DisplayAlert("Ошибка", "Выберите тип обращения", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(SubjectEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите тему обращения", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(DescriptionEditor.Text))
            {
                await DisplayAlert("Ошибка", "Введите описание проблемы", "OK");
                return;
            }

            try
            {
                // Создаем обращение в базе данных
                bool success = await CreateSupportTicket(
                    TicketTypePicker.SelectedItem.ToString() ?? "Другое",
                    SubjectEntry.Text,
                    DescriptionEditor.Text
                );

                if (success)
                {
                    await DisplayAlert("Успех", "Ваше обращение отправлено! Мы свяжемся с вами в ближайшее время.", "OK");
                    
                    // Очищаем форму
                    TicketTypePicker.SelectedItem = null;
                    SubjectEntry.Text = "";
                    DescriptionEditor.Text = "";
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось отправить обращение. Попробуйте позже.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка отправки: {ex.Message}", "OK");
            }
        }

        private async Task<bool> CreateSupportTicket(string ticketType, string subject, string description)
        {
            try
            {
                return await _dbService.CreateSupportTicketAsync(_currentUser.UserId, subject, description, ticketType);
            }
            catch
            {
                return false;
            }
        }

        private async void OnFaqClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                string question = button.Text;
                string answer = GetFaqAnswer(question);
                
                await DisplayAlert("FAQ", answer, "OK");
            }
        }

        private string GetFaqAnswer(string question)
        {
            return question switch
            {
                "Как записаться на курс?" => "1. Перейдите в раздел 'Курсы'\n2. Выберите интересующий курс\n3. Нажмите 'Записаться на курс'\n4. Начните обучение!",
                "Как получить игровую валюту?" => "Игровую валюту можно получить:\n• Завершая курсы\n• Получая достижения\n• За ежедневный вход\n• За участие в конкурсах",
                "Проблемы с входом в систему" => "Если не можете войти:\n1. Проверьте правильность логина и пароля\n2. Убедитесь в наличии интернета\n3. Обратитесь в поддержку",
                "Как изменить настройки?" => "1. Перейдите в 'Профиль'\n2. Нажмите 'Настройки'\n3. Измените нужные параметры\n4. Сохраните изменения",
                _ => "Обратитесь в поддержку для получения подробной информации."
            };
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            OnBackClicked(null, null);
            return true;
        }
    }
}
