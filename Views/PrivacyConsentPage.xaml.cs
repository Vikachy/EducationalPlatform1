using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Maui.Controls;

namespace EducationalPlatform.Views
{
    public partial class PrivacyConsentPage : ContentPage
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;
        private string? _consentText;

        public PrivacyConsentPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            LoadConsentText();
            CheckExistingConsent();
        }

        private void LoadConsentText()
        {
            _consentText = @"
СОГЛАСИЕ НА ОБРАБОТКУ ПЕРСОНАЛЬНЫХ ДАННЫХ

1. Общие положения
1.1. Настоящее Соглашение регулирует отношения между Пользователем и Образовательной платформой.
1.2. Регистрируясь на платформе, Пользователь выражает полное и безоговорочное согласие с условиями.

2. Состав персональных данных
2.1. Фамилия, имя, отчество
2.2. Электронная почта
2.3. Данные об успеваемости и прогрессе
2.4. Результаты тестирования
2.5. Фотографии (аватары)
2.6. Данные об использовании платформы

3. Цели обработки
3.1. Предоставление образовательных услуг
3.2. Анализ успеваемости и персонализация обучения
3.3. Коммуникация с преподавателями";

            ConsentLabel.Text = _consentText;
        }

        private async void CheckExistingConsent()
        {
            try
            {
                bool hasConsent = await _dbService.CheckUserPrivacyConsentAsync(_currentUser.UserId);
                if (hasConsent)
                {
                    await NavigateToMainPage();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки согласия: {ex.Message}");
            }
        }

        private async void OnAcceptClicked(object sender, EventArgs e)
        {
            try
            {
                AcceptButton.IsEnabled = false;
                DeclineButton.IsEnabled = false;

                // Сохраняем в настройках приложения вместо БД
                Preferences.Set($"PrivacyConsent_{_currentUser.UserId}", true);
                Preferences.Set($"PrivacyConsentVersion_{_currentUser.UserId}", "1.0");
                Preferences.Set($"PrivacyConsentDate_{_currentUser.UserId}", DateTime.Now.ToString());

                // Начисляем бонус
                await _dbService.AddGameCurrencyAsync(_currentUser.UserId, 50, "privacy_consent_bonus");
                _currentUser.HasPrivacyConsent = true;

                await DisplayAlert("Успех", "Согласие принято! 🎉 Вам начислено 50 монет.", "OK");
                await NavigateToMainPage();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
                AcceptButton.IsEnabled = true;
                DeclineButton.IsEnabled = true;
            }
        }

        private async void OnDeclineClicked(object sender, EventArgs e)
        {
            bool result = await DisplayAlert("Подтверждение",
                "Без принятия согласия использование платформы невозможно. Вы уверены, что хотите отказаться?",
                "Да, отказаться", "Вернуться");

            if (result)
            {
                try
                {
                    bool deactivated = await _dbService.DeactivateUserAsync(_currentUser.UserId);
                    if (deactivated)
                    {
                        await DisplayAlert("Информация", "Ваш аккаунт деактивирован. Вы можете зарегистрироваться снова, если передумаете.", "OK");

                        // Возвращаемся на страницу логина
                        if (Application.Current?.MainPage is NavigationPage navPage)
                        {
                            await navPage.PopToRootAsync();
                        }
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось деактивировать аккаунт. Пожалуйста, попробуйте еще раз.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Ошибка деактивации: {ex.Message}", "OK");
                }
            }
        }

        private async Task NavigateToMainPage()
        {
            try
            {
                // Обновляем серию входов
                await _dbService.UpdateLoginStreakAsync(_currentUser.UserId);

                // Создаем главную страницу
                var mainPage = new MainDashboardPage(_currentUser, _dbService, _settingsService);

                // Простая навигация
                await Navigation.PushAsync(mainPage);

                // Удаляем текущую страницу из стека
                Navigation.RemovePage(this);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка навигации: {ex.Message}", "OK");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            DisplayAlert("Внимание", "Для использования приложения необходимо принять соглашение.", "OK");
            return true;
        }
    }
}