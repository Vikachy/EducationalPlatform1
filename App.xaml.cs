using EducationalPlatform.Services;

namespace EducationalPlatform
{
    public partial class App : Application
    {
        public static SettingsService AppSettings { get; private set; }

        public App()
        {
            InitializeComponent();

            // Инициализируем настройки
            AppSettings = new SettingsService();

            // Инициализируем тему через SettingsService
            AppSettings.InitializeTheme();

            // Настройка REAL EMAIL - ЗАМЕНИТЕ НА ВАШИ ДАННЫЕ
            ConfigureRealEmailService(
                smtpServer: "smtp.gmail.com",
                port: 587,
                username: "mituxina85@gmail.com",
                password: "uexa rvjo zcrb kvvx"
            );

            // Подписываемся на изменение темы
            AppSettings.ThemeChanged += (s, theme) => OnThemeChanged(theme);

            Console.WriteLine($"🎨 Тема при запуске: {AppSettings.CurrentTheme}");
        }

        // ИСПРАВЛЕННЫЙ метод для обновления темы
        private void OnThemeChanged(string newTheme)
        {
            // Просто вызываем метод из SettingsService
            AppSettings.ApplyTheme(newTheme);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        // Метод для настройки реального email
        public static void ConfigureRealEmailService(string smtpServer, int port, string username, string password)
        {
            Preferences.Set("SmtpServer", smtpServer);
            Preferences.Set("SmtpPort", port);
            Preferences.Set("SmtpUsername", username);
            Preferences.Set("SmtpPassword", password);
            Preferences.Set("SenderEmail", username);
            Preferences.Set("SenderName", "Educational Platform");
            Preferences.Set("EnableSsl", true);

            Console.WriteLine($"✅ Email настроен: {username}");
        }

        protected override void CleanUp()
        {
            // Отписываемся от событий при закрытии приложения
            AppSettings.ThemeChanged -= (s, theme) => OnThemeChanged(theme);
            base.CleanUp();
        }
    }
}