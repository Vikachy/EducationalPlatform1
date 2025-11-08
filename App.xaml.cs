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

            // Применяем сохраненную тему
            ApplyTheme(AppSettings.CurrentTheme);

            // Настройка REAL EMAIL - ЗАМЕНИТЕ НА ВАШИ ДАННЫЕ
            ConfigureRealEmailService(
                smtpServer: "smtp.gmail.com",
                port: 587,
                username: "mituxina85@gmail.com",
                password: "uexa rvjo zcrb kvvx"
            );

            // Подписываемся на изменение темы - ИСПРАВЛЕНО: правильная сигнатура метода
            AppSettings.ThemeChanged += (s, theme) => OnThemeChanged(theme);

            Console.WriteLine($"🎨 Тема при запуске: {AppSettings.CurrentTheme}");
        }

        private void ApplyTheme(string theme)
        {
            if (theme == "teen")
            {
                // Применяем подростковую тему
                Resources["PrimaryColor"] = Color.FromArgb("#FF6B9C");
                Resources["SecondaryColor"] = Color.FromArgb("#4ECDC4");
                Resources["BackgroundColor"] = Color.FromArgb("#F0F8FF");
                Resources["TextColor"] = Color.FromArgb("#2C3E50");
            }
            else
            {
                // Стандартная тема
                Resources["PrimaryColor"] = Color.FromArgb("#2E86AB");
                Resources["SecondaryColor"] = Color.FromArgb("#A23B72");
                Resources["BackgroundColor"] = Colors.White;
                Resources["TextColor"] = Colors.Black;
            }
        }

        // ИСПРАВЛЕНО: правильная сигнатура метода
        private void OnThemeChanged(string newTheme)
        {
            // Обновляем тему во всем приложении
            ApplyTheme(newTheme);
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