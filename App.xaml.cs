using EducationalPlatform.Services;

namespace EducationalPlatform
{
    public partial class App : Application
    {
        public static SettingsService AppSettings { get; private set; }
        public static LocalizationService AppLocalization { get; private set; }

        public App()
        {
            LoadDefaultTheme();

            InitializeComponent();

            // Инициализируем сервисы
            AppSettings = new SettingsService();
            AppLocalization = new LocalizationService();

            // Инициализируем LocalizationService с SettingsService
            AppLocalization.Initialize(AppSettings);

            AppSettings.InitializeTheme();

            // Настройка REAL EMAIL - ЗАМЕНИТЕ НА ВАШИ ДАННЫЕ
            ConfigureRealEmailService(
                smtpServer: "smtp.gmail.com",
                port: 587,
                username: "mituxina85@gmail.com",
                password: "uexa rvjo zcrb kvvx"
            );

            AppSettings.ThemeChanged += (s, theme) => OnThemeChanged(theme);
        }

        private void LoadDefaultTheme()
        {
            // Загружаем стандартную тему
            var defaultColors = ThemeColorService.GetThemeColors("standard");

            Resources["PrimaryColor"] = defaultColors.Primary;
            Resources["SecondaryColor"] = defaultColors.Secondary;
            Resources["BackgroundColor"] = defaultColors.Background;
            Resources["AccentColor"] = defaultColors.Accent;
            Resources["DangerColor"] = defaultColors.Danger;
            Resources["TextColor"] = defaultColors.Text;
            Resources["LightTextColor"] = defaultColors.LightText;
        }

        // ИСПРАВЛЕННЫЙ метод для обновления темы
        private void OnThemeChanged(string newTheme)
        {
            AppSettings.ApplyTheme(newTheme);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        public static void ConfigureRealEmailService(string smtpServer, int port, string username, string password)
        {
            Preferences.Set("SmtpServer", smtpServer);
            Preferences.Set("SmtpPort", port);
            Preferences.Set("SmtpUsername", username);
            Preferences.Set("SmtpPassword", password);
            Preferences.Set("SenderEmail", username);
            Preferences.Set("SenderName", "Educational Platform");
            Preferences.Set("EnableSsl", true);

            Console.WriteLine($"Email настроен: {username}");
        }

        protected override void CleanUp()
        {
            AppSettings.ThemeChanged -= (s, theme) => OnThemeChanged(theme);
            base.CleanUp();
        }
    }
}