using PlantDoctor.Views;

namespace PlantDoctor
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Application.Current!.UserAppTheme = AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new SplashPage());
        }
    }
}