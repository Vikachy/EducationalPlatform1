using EducationalPlatform.Services;

namespace EducationalPlatform
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Принудительно ставим стандартную тему при старте
            var settings = new SettingsService();
            settings.ApplyTheme("standard");

            // НАСТРОЙКА REAL EMAIL - ЗАМЕНИТЕ НА ВАШИ ДАННЫЕ
            ConfigureRealEmailService(
                smtpServer: "smtp.gmail.com",
                port: 587,
                username: "mituxina85@gmail.com", // ваш Gmail
                password: "uexa rvjo zcrb kvvx" // пароль из шага 1.2
            );
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
    }
}