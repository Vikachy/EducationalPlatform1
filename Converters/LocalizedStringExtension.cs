using EducationalPlatform.Services;

namespace EducationalPlatform.Converters
{
    [ContentProperty(nameof(Key))]
    public class LocalizedStringExtension : IMarkupExtension<string>
    {
        private static LocalizationService? _localizationService;
        private static SettingsService? _settingsService;

        public string Key { get; set; } = string.Empty;

        public string ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return string.Empty;

            // Инициализируем сервисы при первом использовании
            if (_localizationService == null)
            {
                _localizationService = App.AppLocalization;
            }

            if (_settingsService == null)
            {
                _settingsService = App.AppSettings;
            }

            // Устанавливаем стиль из темы
            var currentTheme = _settingsService?.CurrentTheme ?? "standard";
            var isTeenStyle = currentTheme == "teen";

            _localizationService?.SetTeenStyle(isTeenStyle);

            return _localizationService?.GetText(Key) ?? Key;
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return ProvideValue(serviceProvider);
        }
    }
}