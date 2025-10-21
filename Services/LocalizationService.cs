using System.Globalization;

namespace EducationalPlatform.Services
{
    public class LocalizationService
    {
        private readonly Dictionary<string, Dictionary<string, string>> _translations;

        public LocalizationService()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>
            {
                ["ru"] = new Dictionary<string, string>
                {
                    ["Welcome"] = "Добро пожаловать",
                    ["Courses"] = "Курсы",
                    ["Progress"] = "Прогресс",
                    ["Profile"] = "Профиль",
                    ["TeacherPanel"] = "Панель учителя",
                    ["AdminPanel"] = "Админ панель",
                    ["Settings"] = "Настройки",
                    ["MainDashboard"] = "Главная"
                },
                ["en"] = new Dictionary<string, string>
                {
                    ["Welcome"] = "Welcome",
                    ["Courses"] = "Courses",
                    ["Progress"] = "Progress",
                    ["Profile"] = "Profile",
                    ["TeacherPanel"] = "Teacher Panel",
                    ["AdminPanel"] = "Admin Panel",
                    ["Settings"] = "Settings",
                    ["MainDashboard"] = "Main Dashboard"
                }
            };
        }

        public string GetString(string key, string language = "ru")
        {
            if (_translations.ContainsKey(language) && _translations[language].ContainsKey(key))
            {
                return _translations[language][key];
            }
            return key; // Возвращаем ключ, если перевод не найден
        }

        public void UpdateAppShellLanguage(AppShell shell, string language)
        {
            // Здесь можно обновить тексты в AppShell при смене языка
            // Пока оставим заглушку, реализуем позже
        }
    }
}