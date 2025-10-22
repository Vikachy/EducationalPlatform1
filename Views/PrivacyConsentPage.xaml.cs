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
3.3. Коммуникация с преподавателями
3.4. Выдача сертификатов и достижений
3.5. Улучшение качества образовательных услуг

4. Срок действия согласия
Согласие действует бессрочно до момента отзыва Пользователем.

5. Права Пользователя
5.1. Право на доступ к своим персональным данным
5.2. Право на исправление неточных данных
5.3. Право на отзыв согласия

Версия 1.0 от 01.01.2024";

            // Устанавливаем текст для отображения
            if (ConsentLabel != null)
            {
                ConsentLabel.Text = _consentText;
            }
        }

        private async void CheckExistingConsent()
        {
            try
            {
                // Проверяем, есть ли уже согласие у пользователя
                bool hasConsent = await CheckUserPrivacyConsentAsync(_currentUser.UserId);
                if (hasConsent)
                {
                    // Если согласие уже есть, переходим на главную
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
                // Сохраняем согласие в базу данных
                bool success = await SavePrivacyConsentAsync(
                    _currentUser.UserId,
                    _consentText ?? string.Empty,
                    "1.0");

                if (success)
                {
                    // Начисляем бонус за принятие согласия
                    await AddGameCurrencyAsync(_currentUser.UserId, 50, "privacy_consent_bonus");

                    await DisplayAlert("Успех", "Согласие принято! 🎉 Вам начислено 50 монет за регистрацию.", "OK");
                    await NavigateToMainPage();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось сохранить согласие", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка сохранения: {ex.Message}", "OK");
            }
        }

        private async void OnDeclineClicked(object sender, EventArgs e)
        {
            bool result = await DisplayAlert("Подтверждение",
                "Без принятия согласия использование платформы невозможно. Вы уверены, что хотите отказаться?",
                "Да, отказаться", "Вернуться");

            if (result)
            {
                // Удаляем пользователя, если он отказался от согласия
                await DeactivateUserAsync(_currentUser.UserId);
                await DisplayAlert("Информация", "Ваш аккаунт деактивирован. Вы можете зарегистрироваться снова, если передумаете.", "OK");
                await Navigation.PopToRootAsync();
            }
        }

        private async Task NavigateToMainPage()
        {
            await Navigation.PushAsync(new MainDashboardPage(_currentUser, _dbService, _settingsService));

            // Удаляем страницу согласия из стека навигации
            var existingPages = Navigation.NavigationStack.ToList();
            foreach (var page in existingPages)
            {
                if (page is PrivacyConsentPage)
                {
                    Navigation.RemovePage(page);
                    break;
                }
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // Блокируем кнопку назад на этой странице
            return true;
        }

        // Временные методы-заглушки (добавьте их в DatabaseService позже)
        private Task<bool> CheckUserPrivacyConsentAsync(int userId)
        {
            // Временная реализация - всегда возвращаем false для тестирования
            return Task.FromResult(false);
        }

        private Task<bool> SavePrivacyConsentAsync(int userId, string consentText, string version)
        {
            // Временная реализация - всегда возвращаем true для тестирования
            return Task.FromResult(true);
        }

        private Task<bool> AddGameCurrencyAsync(int userId, int amount, string reason)
        {
            // Временная реализация
            _currentUser.GameCurrency += amount;
            return Task.FromResult(true);
        }

        private Task<bool> DeactivateUserAsync(int userId)
        {
            // Временная реализация
            return Task.FromResult(true);
        }
    }
}