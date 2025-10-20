using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using EducationalPlatform.Resources.Styles;

namespace EducationalPlatform.Services
{
    public class SettingsService
    {
        private const string LANGUAGE_KEY = "AppLanguage";
        private const string THEME_KEY = "AppTheme";
        private const string DEFAULT_LANGUAGE = "ru";
        private const string DEFAULT_THEME = "standard";

        public string CurrentLanguage
        {
            get => Preferences.Get(LANGUAGE_KEY, DEFAULT_LANGUAGE);
            set => Preferences.Set(LANGUAGE_KEY, value);
        }

        public string CurrentTheme
        {
            get => Preferences.Get(THEME_KEY, DEFAULT_THEME);
            set => Preferences.Set(THEME_KEY, value);
        }

        public void ApplyTheme(string theme)
        {
            CurrentTheme = theme;
            ApplyThemeToApp();
        }

        public void ApplyLanguage(string language)
        {
            CurrentLanguage = language;
        }

        private void ApplyThemeToApp()
        {
            if (Application.Current != null)
            {
                var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
                mergedDictionaries.Clear();

                // Убедись что эти стили существуют в твоём проекте
                if (CurrentTheme == "teen")
                {
                    mergedDictionaries.Add(new Resources.Styles.TeenStyles());
                }
                else
                {
                    mergedDictionaries.Add(new Resources.Styles.StandardStyles());
                }
            }
        }

        public string GetRandomGreeting(string userName)
        {
            var greetings = CurrentLanguage == "ru" ?
                new[]
                {
                    $"С возвращением, {userName}! 🔥",
                    $"Рады видеть, {userName}! ✨",
                    $"Привет, {userName}! Сегодня будет круто! 🚀",
                    $"С возвращением! Готов учиться? 📚",
                    $"С возвращением! Мы скучали 💙",
                } :
                new[]
                {
                    $"Welcome back, {userName}! 🔥",
                    $"Great to see you, {userName}! ✨",
                    $"Hello {userName}! Today will be awesome! 🚀",
                    $"Welcome back! Ready to learn? 📚",
                    $"Welcome back! We missed you 💙",
                };

            var random = new Random();
            return greetings[random.Next(greetings.Length)];
        }

        public string GetStreakMessage(int streakDays)
        {
            if (CurrentLanguage == "ru")
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
    }
}