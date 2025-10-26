using Microsoft.Extensions.DependencyInjection;

namespace EducationalPlatform.Services
{
    public static class ServiceHelper
    {
        public static T GetService<T>() where T : class
        {
            var service = Application.Current?.Handler?.MauiContext?.Services.GetService<T>();
            return service ?? throw new InvalidOperationException($"Service {typeof(T)} not registered");
        }
    }
}