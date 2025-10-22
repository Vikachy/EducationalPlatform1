using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace EducationalPlatform.Services
{
    public class SettingsService
    {
        private const string LANGUAGE_KEY = "AppLanguage";
        private const string THEME_KEY = "AppTheme";
        private const string DEFAULT_LANGUAGE = "ru";
        private const string DEFAULT_THEME = "standard";

        public event EventHandler<string>? ThemeChanged;
        public event EventHandler<string>? LanguageChanged;

        // Статическое событие для глобального обновления
        public static event EventHandler<string>? GlobalThemeChanged;
        public static event EventHandler<string>? GlobalLanguageChanged;

        public string CurrentLanguage
        {
            get => Preferences.Get(LANGUAGE_KEY, DEFAULT_LANGUAGE);
            set
            {
                if (CurrentLanguage != value)
                {
                    Preferences.Set(LANGUAGE_KEY, value);
                    LanguageChanged?.Invoke(this, value);
                    GlobalLanguageChanged?.Invoke(this, value);
                    ApplyLanguageToApp(value);
                }
            }
        }

        public string CurrentTheme
        {
            get => Preferences.Get(THEME_KEY, DEFAULT_THEME);
            set
            {
                if (CurrentTheme != value)
                {
                    Preferences.Set(THEME_KEY, value);
                    ThemeChanged?.Invoke(this, value);
                    GlobalThemeChanged?.Invoke(this, value);
                    ApplyThemeToApp(value);
                }
            }
        }

        public void ApplyTheme(string theme)
        {
            CurrentTheme = theme;
        }

        public void ApplyLanguage(string language)
        {
            CurrentLanguage = language;
        }

        private void ApplyThemeToApp(string theme)
        {
            if (Application.Current != null)
            {
                Microsoft.Maui.Controls.Application.Current.Dispatcher.Dispatch(() =>
                {
                    if (theme == "teen")
                    {
                        // Подростковая тема
                        Application.Current.Resources["PrimaryColor"] = Color.FromArgb("#669bbc");
                        Application.Current.Resources["SecondaryColor"] = Color.FromArgb("#003049");
                        Application.Current.Resources["BackgroundColor"] = Color.FromArgb("#fdf0d5");
                        Application.Current.Resources["AccentColor"] = Color.FromArgb("#c1121f");
                        Application.Current.Resources["TextColor"] = Color.FromArgb("#003049");
                        Application.Current.Resources["LightTextColor"] = Color.FromArgb("#fdf0d5");
                    }
                    else
                    {
                        // Стандартная тема
                        Application.Current.Resources["PrimaryColor"] = Color.FromArgb("#457b9d");
                        Application.Current.Resources["SecondaryColor"] = Color.FromArgb("#1d3557");
                        Application.Current.Resources["BackgroundColor"] = Color.FromArgb("#f1faee");
                        Application.Current.Resources["AccentColor"] = Color.FromArgb("#e63946");
                        Application.Current.Resources["TextColor"] = Color.FromArgb("#1d3557");
                        Application.Current.Resources["LightTextColor"] = Color.FromArgb("#f1faee");
                    }

                    // Обновляем NavigationBar и StatusBar
                    UpdateNavigationBar(theme);
                });
            }
        }

        private void ApplyLanguageToApp(string language)
        {
            // Обновляем тексты на всех страницах через события
            UpdateAllPagesText(language);
        }

        private void UpdateNavigationBar(string theme)
        {
            // Можно добавить обновление цвета navigation bar если нужно
        }

        private void UpdateAllPagesText(string language)
        {
            // Логика обновления текстов будет в отдельных страницах
        }

        // Методы для получения локализованных строк
        public string GetLocalizedString(string key)
        {
            return CurrentLanguage == "ru" ? GetRussianString(key) : GetEnglishString(key);
        }

        private string GetRussianString(string key)
        {
            return key switch
            {
                "Welcome" => "Добро пожаловать",
                "Courses" => "Курсы",
                "Progress" => "Прогресс",
                "Profile" => "Профиль",
                "Settings" => "Настройки",
                "MainDashboard" => "Главная",
                "TeacherPanel" => "Панель учителя",
                "AdminPanel" => "Админ панель",
                "MyCourses" => "Мои курсы",
                "Achievements" => "Достижения",
                "Shop" => "Магазин",
                "Statistics" => "Статистика",
                "Appearance" => "Внешний вид",
                "ContinueStreak" => "Продолжить серию",
                "TodayTasks" => "Сегодняшние задачи",
                "AllCourses" => "Все курсы",
                "AllNews" => "Все новости",
                "Complete" => "Завершено",
                "StartLearning" => "Начать обучение",
                "ViewProgress" => "Смотреть прогресс",
                "MyAwards" => "Мои награды",
                "Buy" => "Покупать",
                _ => key
            };
        }

        private string GetEnglishString(string key)
        {
            return key switch
            {
                "Welcome" => "Welcome",
                "Courses" => "Courses",
                "Progress" => "Progress",
                "Profile" => "Profile",
                "Settings" => "Settings",
                "MainDashboard" => "Main Dashboard",
                "TeacherPanel" => "Teacher Panel",
                "AdminPanel" => "Admin Panel",
                "MyCourses" => "My Courses",
                "Achievements" => "Achievements",
                "Shop" => "Shop",
                "Statistics" => "Statistics",
                "Appearance" => "Appearance",
                "ContinueStreak" => "Continue Streak",
                "TodayTasks" => "Today's Tasks",
                "AllCourses" => "All Courses",
                "AllNews" => "All News",
                "Complete" => "Complete",
                "StartLearning" => "Start Learning",
                "ViewProgress" => "View Progress",
                "MyAwards" => "My Awards",
                "Buy" => "Buy",
                _ => key
            };
        }

        public string GetRandomGreeting(string userName)
        {
            string currentLanguage = CurrentLanguage;
            var greetings = currentLanguage == "ru" ?
                new[]
                {
                    $"С возвращением, {userName}! 🔥",
                    $"Рады видеть, {userName}! ✨",
                    $"Привет, {userName}! Сегодня будет круто! 🚀",
                    $"С возвращением! Готов учиться? 📚",
                    $"С возвращением! Мы скучали 💙",
                    $"Привет, {userName}! Новые знания ждут! 🌟",
                    $"Рад тебя видеть, {userName}! 💪",
                    $"С возвращением! Время кодить! 💻",
                    $"Спасибо, что не забыл про меня, {userName}! 💕",
                    $"Ты вернулся! Мы так ждали! 🎉",
                    $"Привет, {userName}! Твоя серия продолжается! 🔥",
                    $"С возвращением, {userName}! Готов к новым вызовам? ⚡",
                    $"Мы скучали по тебе, {userName}! 💙",
                    $"Привет, {userName}! Время стать лучше! 🌟",
                    $"С возвращением! Твои навыки ждут! 💻"
                } :
                new[]
                {
                    $"Welcome back, {userName}! 🔥",
                    $"Great to see you, {userName}! ✨",
                    $"Hello {userName}! Today will be awesome! 🚀",
                    $"Welcome back! Ready to learn? 📚",
                    $"Welcome back! We missed you 💙",
                    $"Hello {userName}! New knowledge awaits! 🌟",
                    $"Glad to see you, {userName}! 💪",
                    $"Welcome back! Time to code! 💻",
                    $"Thanks for not forgetting me, {userName}! 💕",
                    $"You're back! We've been waiting! 🎉",
                    $"Hello {userName}! Your streak continues! 🔥",
                    $"Welcome back, {userName}! Ready for new challenges? ⚡",
                    $"We missed you, {userName}! 💙",
                    $"Hello {userName}! Time to get better! 🌟",
                    $"Welcome back! Your skills are waiting! 💻"
                };

            var random = new Random();
            return greetings[random.Next(greetings.Length)];
        }

        public string GetStreakMessage(int streakDays)
        {
            string currentLanguage = CurrentLanguage;
            if (currentLanguage == "ru")
            {
                return streakDays switch
                {
                    0 => "Начни свою серию! 💫",
                    1 => "1 день подряд! 🔥",
                    < 7 => $"{streakDays} дня подряд! 🔥",
                    < 30 => $"{streakDays} дней подряд! ⭐",
                    _ => $"{streakDays} дней подряд! 🏆"
                };
            }
            else
            {
                return streakDays switch
                {
                    0 => "Start your streak! 💫",
                    1 => "1 day in a row! 🔥",
                    < 7 => $"{streakDays} days in a row! 🔥",
                    < 30 => $"{streakDays} days in a row! ⭐",
                    _ => $"{streakDays} days in a row! 🏆"
                };
            }
        }

        public string GetThemeDisplayName(string theme)
        {
            string language = CurrentLanguage;
            if (theme == "standard")
            {
                return language == "ru" ? "Стандартная" : "Standard";
            }
            else if (theme == "teen")
            {
                return language == "ru" ? "Для подростков" : "For Teens";
            }
            return theme;
        }

        public string GetLanguageDisplayName(string languageCode)
        {
            return languageCode switch
            {
                "ru" => "Русский",
                "en" => "English",
                _ => languageCode
            };
        }
    }
}