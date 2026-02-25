using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace EducationalPlatform.Views
{
    public partial class PrivacyConsentPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private string? _consentText;

        public PrivacyConsentPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
                _currentUser = user ?? throw new ArgumentNullException(nameof(user));
                _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
                _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

                LoadConsentText();
                CheckConsentAndRedirectIfAlreadyAccepted();

                Console.WriteLine("[PrivacyConsentPage] Конструктор успешно выполнен");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrivacyConsentPage] Крах в конструкторе: {ex.Message}\n{ex.StackTrace}");
            }
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

4. Срок действия согласия
4.1. Согласие действительно в течение 1 года
4.2. По истечении срока действия согласие может быть продлено
";

            if (ConsentLabel != null)
                ConsentLabel.Text = _consentText;
        }

        private async void CheckConsentAndRedirectIfAlreadyAccepted()
        {
            try
            {
                bool hasConsent = await _dbService.CheckUserPrivacyConsentAsync(_currentUser.UserId);
                Console.WriteLine($"[PrivacyConsentPage] Проверка согласия UserId {_currentUser.UserId} → {hasConsent}");

                if (hasConsent)
                {
                    Console.WriteLine("[PrivacyConsentPage] Согласие уже есть → переход на главную страницу");
                    await NavigateToMainPage();
                }
                // если false — показываем текст согласия и кнопки
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrivacyConsentPage] Ошибка при проверке согласия: {ex.Message}");
                await DisplayAlert("Ошибка", "Не удалось проверить статус согласия", "OK");
            }
        }

        private async void OnAcceptClicked(object sender, EventArgs e)
        {
            try
            {
                if (AcceptButton != null) AcceptButton.IsEnabled = false;
                if (DeclineButton != null) DeclineButton.IsEnabled = false;

                string deviceId = await GetDeviceId();

                await _dbService.SavePrivacyConsentAsync(
                    _currentUser.UserId,
                    _consentText ?? "(текст согласия не загружен)",
                    "1.0"
                );

                await _dbService.AddGameCurrencyAsync(_currentUser.UserId, 50, "privacy_consent_bonus");

                _currentUser.PrivacyConsentAccepted = true;

                await DisplayAlert("Успех", "Согласие принято! Вам начислено 50 монет.", "OK");

                await NavigateToMainPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrivacyConsentPage] Ошибка при принятии согласия: {ex.Message}");
                await DisplayAlert("Ошибка", $"Не удалось сохранить согласие: {ex.Message}", "OK");
            }
            finally
            {
                if (AcceptButton != null) AcceptButton.IsEnabled = true;
                if (DeclineButton != null) DeclineButton.IsEnabled = true;
            }
        }

        private async void OnDeclineClicked(object sender, EventArgs e)
        {
            bool confirmed = await DisplayAlert("Подтверждение",
                "Без принятия согласия использование платформы невозможно.\nВы уверены, что хотите отказаться?",
                "Да, отказаться", "Вернуться");

            if (!confirmed) return;

            try
            {
                bool deactivated = await _dbService.DeactivateUserAsync(_currentUser.UserId);
                if (deactivated)
                {
                    await DisplayAlert("Информация",
                        "Ваш аккаунт деактивирован. Вы можете зарегистрироваться снова позже.",
                        "OK");

                    // Возвращаемся на страницу логина / регистрации
                    if (Application.Current?.MainPage is NavigationPage nav)
                    {
                        await nav.PopToRootAsync();
                    }
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось деактивировать аккаунт.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка деактивации: {ex.Message}", "OK");
            }
        }

        private async Task NavigateToMainPage()
        {
            try
            {
                // 1. Обновляем streak и пользователя
                await _dbService.UpdateLoginStreakAsync(_currentUser.UserId);
                var updatedUser = await _dbService.GetUserByIdAsync(_currentUser.UserId) ?? _currentUser;

                // 2. Создаём страницу
                MainDashboardPage? mainPage = null;
                try
                {
                    mainPage = new MainDashboardPage(updatedUser, _dbService, _settingsService);
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"Ошибка создания MainDashboardPage: {innerEx.Message}");
                    await DisplayAlert("Критическая ошибка", "Не удалось создать главную страницу.", "OK");
                    return;
                }

                if (mainPage == null)
                {
                    Console.WriteLine("MainDashboardPage создалась как null!");
                    await DisplayAlert("Ошибка", "Главная страница не была создана.", "OK");
                    return;
                }

                if (Application.Current == null)
                {
                    Console.WriteLine("Application.Current == null — невозможно изменить MainPage");
                    return;
                }

                Application.Current.MainPage = new NavigationPage(mainPage);

                Console.WriteLine("Навигация на MainDashboardPage выполнена успешно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NavigateToMainPage] Общая ошибка: {ex.Message}\n{ex.StackTrace}");
                await DisplayAlert("Ошибка навигации", $"Не удалось перейти на главную: {ex.Message}", "OK");
            }
        }

        private async Task<string> GetDeviceId()
        {
            try
            {
                var deviceId = await SecureStorage.GetAsync("global_device_id");
                if (string.IsNullOrEmpty(deviceId))
                {
                    deviceId = Guid.NewGuid().ToString();
                    await SecureStorage.SetAsync("global_device_id", deviceId);
                }
                return deviceId;
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }

        protected override bool OnBackButtonPressed()
        {
            DisplayAlert("Внимание", "Для продолжения необходимо принять соглашение.", "OK");
            return true; // блокируем стандартный back
        }
    }
}