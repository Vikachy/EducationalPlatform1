// Services/ThemeColorService.cs
namespace EducationalPlatform.Services
{
    public static class ThemeColorService
    {
        // Словарь для хранения цветов тем по названию
        private static readonly Dictionary<string, ThemeColors> ThemeColors = new()
        {
            // Стандартная тема (по умолчанию)
            ["standard"] = new ThemeColors
            {
                PrimaryColor = "#457b9d",
                SecondaryColor = "#1d3557",
                BackgroundColor = "#f1faee",
                AccentColor = "#e63946",
                DangerColor = "#e63946",
                TextColor = "#1d3557",
                LightTextColor = "#f1faee"
            },

            // Подростковая тема
            ["teen"] = new ThemeColors
            {
                PrimaryColor = "#5a9bc5",
                SecondaryColor = "#2a3f6b",
                BackgroundColor = "#121826",
                AccentColor = "#ff5a6e",
                DangerColor = "#ff5a6e",
                TextColor = "#003049",
                LightTextColor = "#d6e4ff"
            },

            // Океан
            ["ocean"] = new ThemeColors
            {
                PrimaryColor = "#2E86AB",
                SecondaryColor = "#003049",
                BackgroundColor = "#E9F2F5",
                AccentColor = "#F4A261",
                DangerColor = "#E76F51",
                TextColor = "#003049",
                LightTextColor = "#E9F2F5"
            },

            // Пурпур
            ["purple"] = new ThemeColors
            {
                PrimaryColor = "#A23B72",
                SecondaryColor = "#2D1B3C",
                BackgroundColor = "#F7E6F0",
                AccentColor = "#F5A65B",
                DangerColor = "#D90368",
                TextColor = "#2D1B3C",
                LightTextColor = "#F7E6F0"
            },

            // Джунгли
            ["jungle"] = new ThemeColors
            {
                PrimaryColor = "#2A9D8F",
                SecondaryColor = "#264653",
                BackgroundColor = "#EAF4E4",
                AccentColor = "#E9C46A",
                DangerColor = "#E76F51",
                TextColor = "#264653",
                LightTextColor = "#EAF4E4"
            },

            // Закат
            ["sunset"] = new ThemeColors
            {
                PrimaryColor = "#E76F51",
                SecondaryColor = "#6B4F3C",
                BackgroundColor = "#FFF1E6",
                AccentColor = "#E9C46A",
                DangerColor = "#C44569",
                TextColor = "#6B4F3C",
                LightTextColor = "#FFF1E6"
            },

            // Ночь
            ["night"] = new ThemeColors
            {
                PrimaryColor = "#1A1A2E",
                SecondaryColor = "#16213E",
                BackgroundColor = "#0F3460",
                AccentColor = "#E94560",
                DangerColor = "#E94560",
                TextColor = "#ECF0F1",
                LightTextColor = "#1A1A2E"
            },

            // Пустыня
            ["desert"] = new ThemeColors
            {
                PrimaryColor = "#F4A261",
                SecondaryColor = "#5E3A1C",
                BackgroundColor = "#FAEDCD",
                AccentColor = "#E76F51",
                DangerColor = "#BC6C25",
                TextColor = "#5E3A1C",
                LightTextColor = "#FAEDCD"
            },

            // Осень
            ["autumn"] = new ThemeColors
            {
                PrimaryColor = "#D96C06",
                SecondaryColor = "#5E3A1C",
                BackgroundColor = "#FDF0D5",
                AccentColor = "#BF5700",
                DangerColor = "#A84008",
                TextColor = "#5E3A1C",
                LightTextColor = "#FDF0D5"
            },

            // Зима
            ["winter"] = new ThemeColors
            {
                PrimaryColor = "#5D9B9B",
                SecondaryColor = "#2F4858",
                BackgroundColor = "#E5F2F2",
                AccentColor = "#BFD7EA",
                DangerColor = "#F46036",
                TextColor = "#2F4858",
                LightTextColor = "#E5F2F2"
            },

            // Лето
            ["summer"] = new ThemeColors
            {
                PrimaryColor = "#FF9F1C",
                SecondaryColor = "#2E1B3C",
                BackgroundColor = "#FFF3E0",
                AccentColor = "#2E86AB",
                DangerColor = "#C32F27",
                TextColor = "#2E1B3C",
                LightTextColor = "#FFF3E0"
            },

            // Галактика
            ["galaxy"] = new ThemeColors
            {
                PrimaryColor = "#4A0D67",
                SecondaryColor = "#1B065E",
                BackgroundColor = "#120B2E",
                AccentColor = "#FF6F61",
                DangerColor = "#FF4F4F",
                TextColor = "#E6E6FA",
                LightTextColor = "#4A0D67"
            },

            // Морская
            ["sea"] = new ThemeColors
            {
                PrimaryColor = "#0A9396",
                SecondaryColor = "#005F73",
                BackgroundColor = "#CAF0F8",
                AccentColor = "#FFB703",
                DangerColor = "#AE2012",
                TextColor = "#005F73",
                LightTextColor = "#CAF0F8"
            },

            // Космос
            ["space"] = new ThemeColors
            {
                PrimaryColor = "#1B2A4E",
                SecondaryColor = "#121E3A",
                BackgroundColor = "#0B1622",
                AccentColor = "#FFD966",
                DangerColor = "#B22222",
                TextColor = "#E0E1DD",
                LightTextColor = "#1B2A4E"
            },

            // Винтаж
            ["vintage"] = new ThemeColors
            {
                PrimaryColor = "#8B5A2B",
                SecondaryColor = "#5E3A1C",
                BackgroundColor = "#F8F0E3",
                AccentColor = "#B76E79",
                DangerColor = "#9B2C2C",
                TextColor = "#3D2B1F",
                LightTextColor = "#F8F0E3"
            },

            // НОВЫЕ ТЕМЫ (10-15 штук)

            // Весна
            ["spring"] = new ThemeColors
            {
                PrimaryColor = "#88B04B",      // Нежно-зеленый
                SecondaryColor = "#6B5B95",     // Лавандовый
                BackgroundColor = "#FFF5E6",    // Кремовый
                AccentColor = "#FF6F61",        // Коралловый
                DangerColor = "#FF6F61",
                TextColor = "#6B5B95",
                LightTextColor = "#FFF5E6"
            },

            // Мятная
            ["mint"] = new ThemeColors
            {
                PrimaryColor = "#98D8C8",       // Мятный
                SecondaryColor = "#4A6FA5",     // Синий
                BackgroundColor = "#F0F7F4",    // Светло-мятный
                AccentColor = "#FFB347",        // Оранжевый
                DangerColor = "#FF6B6B",
                TextColor = "#4A6FA5",
                LightTextColor = "#F0F7F4"
            },

            // Розовая
            ["pink"] = new ThemeColors
            {
                PrimaryColor = "#FFB6C1",       // Светло-розовый
                SecondaryColor = "#C71585",     // Темно-розовый
                BackgroundColor = "#FFF0F5",    // Лавандовый румянец
                AccentColor = "#FF69B4",        // Горячий розовый
                DangerColor = "#FF1493",
                TextColor = "#C71585",
                LightTextColor = "#FFF0F5"
            },

            // Изумрудная
            ["emerald"] = new ThemeColors
            {
                PrimaryColor = "#50C878",       // Изумрудный
                SecondaryColor = "#2E7D32",     // Темно-зеленый
                BackgroundColor = "#E8F5E9",    // Светло-зеленый
                AccentColor = "#FFD700",        // Золотой
                DangerColor = "#FF5252",
                TextColor = "#2E7D32",
                LightTextColor = "#E8F5E9"
            },

            // Сапфировая
            ["sapphire"] = new ThemeColors
            {
                PrimaryColor = "#0F52BA",       // Сапфировый
                SecondaryColor = "#002D62",     // Темно-синий
                BackgroundColor = "#E6F0FA",    // Светло-голубой
                AccentColor = "#FFD700",        // Золотой
                DangerColor = "#FF4D4D",
                TextColor = "#002D62",
                LightTextColor = "#E6F0FA"
            },

            // Рубиновая
            ["ruby"] = new ThemeColors
            {
                PrimaryColor = "#E0115F",       // Рубиновый
                SecondaryColor = "#9B1D30",     // Темно-красный
                BackgroundColor = "#FFF0F0",    // Светло-розовый
                AccentColor = "#FFD700",        // Золотой
                DangerColor = "#8B0000",
                TextColor = "#9B1D30",
                LightTextColor = "#FFF0F0"
            },

            // Лавандовая
            ["lavender"] = new ThemeColors
            {
                PrimaryColor = "#967BB6",       // Лавандовый
                SecondaryColor = "#4B0082",     // Индиго
                BackgroundColor = "#F8F4FF",    // Призрачно-белый
                AccentColor = "#FFB6C1",        // Светло-розовый
                DangerColor = "#FF6B6B",
                TextColor = "#4B0082",
                LightTextColor = "#F8F4FF"
            },

            // Шоколадная
            ["chocolate"] = new ThemeColors
            {
                PrimaryColor = "#7B3F00",       // Шоколадный
                SecondaryColor = "#4A2C1A",     // Темно-коричневый
                BackgroundColor = "#F5E6D3",    // Бежевый
                AccentColor = "#FFA07A",        // Светло-лососевый
                DangerColor = "#FF6347",
                TextColor = "#4A2C1A",
                LightTextColor = "#F5E6D3"
            },

            // Арктическая
            ["arctic"] = new ThemeColors
            {
                PrimaryColor = "#A4D0E0",       // Ледяной голубой
                SecondaryColor = "#2C3E50",     // Темно-синий
                BackgroundColor = "#F0F8FF",    // Алиса голубой
                AccentColor = "#E0F2FE",        // Светло-голубой
                DangerColor = "#FF6B6B",
                TextColor = "#2C3E50",
                LightTextColor = "#F0F8FF"
            },

            // Тропическая
            ["tropical"] = new ThemeColors
            {
                PrimaryColor = "#FF6B6B",       // Коралловый
                SecondaryColor = "#4ECDC4",     // Бирюзовый
                BackgroundColor = "#FFE66D",    // Желтый
                AccentColor = "#FF9F1C",        // Оранжевый
                DangerColor = "#C92A2A",
                TextColor = "#2E1B3C",
                LightTextColor = "#FFE66D"
            },

            // Пастельная
            ["pastel"] = new ThemeColors
            {
                PrimaryColor = "#B4D4E5",       // Пастельный голубой
                SecondaryColor = "#D6A2B8",     // Пастельный розовый
                BackgroundColor = "#F9F3E6",    // Пастельный желтый
                AccentColor = "#B8D9B0",        // Пастельный зеленый
                DangerColor = "#E8A4A4",
                TextColor = "#7C6A6A",
                LightTextColor = "#F9F3E6"
            },

            // Неоновая
            ["neon"] = new ThemeColors
            {
                PrimaryColor = "#39FF14",       // Неоново-зеленый
                SecondaryColor = "#FF1493",     // Неоново-розовый
                BackgroundColor = "#000000",    // Черный
                AccentColor = "#00FFFF",        // Неоново-голубой
                DangerColor = "#FF3131",
                TextColor = "#FFFFFF",
                LightTextColor = "#000000"
            },

            // Ретро
            ["retro"] = new ThemeColors
            {
                PrimaryColor = "#E6B800",       // Золотистый
                SecondaryColor = "#8B4513",     // Коричневый
                BackgroundColor = "#F5DEB3",    // Пшеничный
                AccentColor = "#CD5C5C",        // Индийский красный
                DangerColor = "#B22222",
                TextColor = "#8B4513",
                LightTextColor = "#F5DEB3"
            },

            // Футуристическая
            ["futuristic"] = new ThemeColors
            {
                PrimaryColor = "#00FFFF",       // Голубой
                SecondaryColor = "#FF00FF",     // Пурпурный
                BackgroundColor = "#0A0A0A",    // Почти черный
                AccentColor = "#FFFF00",        // Желтый
                DangerColor = "#FF0000",
                TextColor = "#FFFFFF",
                LightTextColor = "#0A0A0A"
            },

            // Мона Лиза
            ["monalisa"] = new ThemeColors
            {
                PrimaryColor = "#C49A6C",       // Бежевый
                SecondaryColor = "#8B5A2B",     // Коричневый
                BackgroundColor = "#F2E3D5",    // Кремовый
                AccentColor = "#A0522D",        // Сиена
                DangerColor = "#8B3A3A",
                TextColor = "#5D3A1A",
                LightTextColor = "#F2E3D5"
            }
        };

        public static ThemeColors GetThemeColors(string themeKey)
        {
            if (ThemeColors.TryGetValue(themeKey.ToLower(), out var colors))
                return colors;

            return ThemeColors["standard"]; // По умолчанию стандартная
        }

        public static string GetThemeKeyFromName(string themeName)
        {
            string name = themeName.ToLower();

            if (name.Contains("стандарт") || name.Contains("standard")) return "standard";
            if (name.Contains("подростк") || name.Contains("teen")) return "teen";
            if (name.Contains("океан") || name.Contains("ocean")) return "ocean";
            if (name.Contains("пурпур") || name.Contains("purple")) return "purple";
            if (name.Contains("джунгли") || name.Contains("jungle")) return "jungle";
            if (name.Contains("закат") || name.Contains("sunset")) return "sunset";
            if (name.Contains("ночь") || name.Contains("night")) return "night";
            if (name.Contains("пустыня") || name.Contains("desert")) return "desert";
            if (name.Contains("осень") || name.Contains("autumn")) return "autumn";
            if (name.Contains("зима") || name.Contains("winter")) return "winter";
            if (name.Contains("лето") || name.Contains("summer")) return "summer";
            if (name.Contains("галактик") || name.Contains("galaxy")) return "galaxy";
            if (name.Contains("морск") || name.Contains("sea")) return "sea";
            if (name.Contains("космос") || name.Contains("space")) return "space";
            if (name.Contains("винтаж") || name.Contains("vintage")) return "vintage";

            // НОВЫЕ ТЕМЫ
            if (name.Contains("весна") || name.Contains("spring")) return "spring";
            if (name.Contains("мята") || name.Contains("mint")) return "mint";
            if (name.Contains("розов") || name.Contains("pink")) return "pink";
            if (name.Contains("изумруд") || name.Contains("emerald")) return "emerald";
            if (name.Contains("сапфир") || name.Contains("sapphire")) return "sapphire";
            if (name.Contains("рубин") || name.Contains("ruby")) return "ruby";
            if (name.Contains("лаванд") || name.Contains("lavender")) return "lavender";
            if (name.Contains("шоколад") || name.Contains("chocolate")) return "chocolate";
            if (name.Contains("арктик") || name.Contains("arctic")) return "arctic";
            if (name.Contains("тропик") || name.Contains("tropical")) return "tropical";
            if (name.Contains("пастель") || name.Contains("pastel")) return "pastel";
            if (name.Contains("неон") || name.Contains("neon")) return "neon";
            if (name.Contains("ретро") || name.Contains("retro")) return "retro";
            if (name.Contains("футур") || name.Contains("futuristic")) return "futuristic";
            if (name.Contains("мона лиза") || name.Contains("monalisa")) return "monalisa";

            return "standard";
        }

        public static Color FromHex(string hex) => Color.FromArgb(hex);
    }

    public class ThemeColors
    {
        public string PrimaryColor { get; set; } = "#457b9d";
        public string SecondaryColor { get; set; } = "#1d3557";
        public string BackgroundColor { get; set; } = "#f1faee";
        public string AccentColor { get; set; } = "#e63946";
        public string DangerColor { get; set; } = "#e63946";
        public string TextColor { get; set; } = "#1d3557";
        public string LightTextColor { get; set; } = "#f1faee";

        public Color Primary => Color.FromArgb(PrimaryColor);
        public Color Secondary => Color.FromArgb(SecondaryColor);
        public Color Background => Color.FromArgb(BackgroundColor);
        public Color Accent => Color.FromArgb(AccentColor);
        public Color Danger => Color.FromArgb(DangerColor);
        public Color Text => Color.FromArgb(TextColor);
        public Color LightText => Color.FromArgb(LightTextColor);
    }
}