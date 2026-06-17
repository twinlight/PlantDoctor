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
            // Navigation to HistoryPage — wired up in Phase 4
            await DisplayAlert("Coming Soon", "History page coming next!", "OK");
        }
    }
}