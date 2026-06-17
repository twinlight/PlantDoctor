using Microsoft.Extensions.DependencyInjection;

namespace PlantDoctor
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Application.Current!.UserAppTheme = AppTheme.Light; // 👈 add this line
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}