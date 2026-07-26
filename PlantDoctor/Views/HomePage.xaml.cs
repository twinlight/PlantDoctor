namespace PlantDoctor.Views
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private async void OnScanTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(CapturePage));
        }

        private async void OnHistoryTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(HistoryPage));
        }
    }
}
