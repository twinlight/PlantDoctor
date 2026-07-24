namespace PlantDoctor.Helpers
{
    /// <summary>
    /// Resolves services from the application root provider, which survives Android activity recreation.
    /// Handler.MauiContext.Services can be disposed after camera/picker or process resume.
    /// </summary>
    public static class ServiceHelper
    {
        public static T GetRequiredService<T>() where T : notnull
            => GetServices().GetRequiredService<T>();

        public static IServiceProvider GetServices()
        {
            var services = IPlatformApplication.Current?.Services;
            if (services == null)
                throw new InvalidOperationException("Application services are not available.");

            return services;
        }
    }
}
