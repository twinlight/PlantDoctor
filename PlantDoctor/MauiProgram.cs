using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using PlantDoctor.Services;
using PlantDoctor.Views;

namespace PlantDoctor
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<ConnectivityService>();
            builder.Services.AddSingleton<OnnxInferenceService>();
            builder.Services.AddSingleton<ApiInferenceService>();
            builder.Services.AddSingleton<InferenceCoordinator>(sp => new InferenceCoordinator(
                sp.GetRequiredService<OnnxInferenceService>(),
                sp.GetRequiredService<ApiInferenceService>(),
                sp.GetRequiredService<ConnectivityService>(),
                sp.GetRequiredService<DatabaseService>()
            ));

            // Views
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<CapturePage>();
            builder.Services.AddTransient<ResultPage>();
            builder.Services.AddTransient<HistoryPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}