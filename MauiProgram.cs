using EducationalPlatform;
using EducationalPlatform.Services;
using Microsoft.Extensions.Logging;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureEssentials(essentials =>
            {
                essentials.UseVersionTracking();
            });

        // Регистрируем сервисы
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<LocalizationService>();
        builder.Services.AddSingleton<CaptchaService>();
        builder.Services.AddSingleton<FileService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}