using PlantDoctor.Services;

namespace PlantDoctor.Views
{
    public partial class LoadingPage : ContentPage
    {
        private readonly OnnxInferenceService _onnxService;
        private readonly DatabaseService _dbService;

        public LoadingPage(OnnxInferenceService onnxService, DatabaseService dbService)
        {
            InitializeComponent();
            _onnxService = onnxService;
            _dbService = dbService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await InitializeAppAsync();
        }

        private async Task InitializeAppAsync()
        {
            try
            {
                // Step 1 — warm up database
                StatusLabel.Text = "Loading treatment database...";
                await _dbService.GetDiseaseInfoAsync(0);
                await Task.Delay(400);

                // Step 2 — warm up ONNX model
                StatusLabel.Text = "Loading AI model...";
                await _onnxService.WarmUpAsync();
                await Task.Delay(400);

                // Step 3 — done
                StatusLabel.Text = "Ready!";
                await Task.Delay(300);

                // Navigate to main app
                await this.FadeTo(0, 300);
                Application.Current!.MainPage = new AppShell();
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error: {ex.Message}";
                await Task.Delay(2000);
                Application.Current!.MainPage = new AppShell();
            }
        }
    }
}