using Microsoft.Extensions.DependencyInjection;

namespace PlantDoctor
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Application.Current!.UserAppTheme = AppTheme.Light;

#if ANDROID
            Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"[Android] Unhandled: {args.Exception}");
                if (args.Exception.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"[Android] Inner: {args.Exception.InnerException}");
            };
#endif
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}