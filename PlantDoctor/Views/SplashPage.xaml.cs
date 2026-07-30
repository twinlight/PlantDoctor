namespace PlantDoctor.Views
{
    public partial class SplashPage : ContentPage
    {
        public SplashPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Animate button in
            GetStartedButton.Opacity = 0;
            GetStartedButton.TranslationY = 20;
            await Task.WhenAll(
                GetStartedButton.FadeTo(1, 600),
                GetStartedButton.TranslateTo(0, 0, 600, Easing.CubicOut)
            );
        }

        private async void OnGetStartedTapped(object sender, EventArgs e)
        {
            GetStartedButton.IsEnabled = false;

            // Fade out and navigate
            await this.FadeTo(0, 300);
            Application.Current!.MainPage = new AppShell();
        }
    }
}