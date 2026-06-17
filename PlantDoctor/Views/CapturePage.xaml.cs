using PlantDoctor.Models;
using PlantDoctor.Services;

namespace PlantDoctor.Views
{
    public partial class CapturePage : ContentPage
    {
        private string? _selectedImagePath;
        private readonly OnnxInferenceService _onnxService;
        private readonly DatabaseService _databaseService;

        public CapturePage(OnnxInferenceService onnxService, DatabaseService databaseService)
        {
            InitializeComponent();
            _onnxService = onnxService;
            _databaseService = databaseService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateConnectivityBadge();
        }

        private void UpdateConnectivityBadge()
        {
            var isConnected = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            if (isConnected)
            {
                ConnectivityIcon.Text = "🟢";
                ConnectivityLabel.Text = "Online — using cloud AI for enhanced accuracy";
                ConnectivityBadge.BackgroundColor = Color.FromArgb("#E8F5E9");
                ConnectivityLabel.TextColor = Color.FromArgb("#1B5E20");
            }
            else
            {
                ConnectivityIcon.Text = "🟡";
                ConnectivityLabel.Text = "Offline — using on-device AI model";
                ConnectivityBadge.BackgroundColor = Color.FromArgb("#FFF8E1");
                ConnectivityLabel.TextColor = Color.FromArgb("#FF8F00");
            }
        }

        private async void OnCameraTapped(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Denied", "Camera permission is required.", "OK");
                    return;
                }
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null) await LoadImageAsync(photo);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not open camera: {ex.Message}", "OK");
            }
        }

        private async void OnGalleryTapped(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo != null) await LoadImageAsync(photo);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not open gallery: {ex.Message}", "OK");
            }
        }

        private async Task LoadImageAsync(FileResult photo)
        {
            var localPath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
            using (var stream = await photo.OpenReadAsync())
            using (var fileStream = File.OpenWrite(localPath))
                await stream.CopyToAsync(fileStream);

            _selectedImagePath = localPath;
            SelectedImage.Source = ImageSource.FromFile(localPath);
            SelectedImage.IsVisible = true;
            PlaceholderLayout.IsVisible = false;
            ReSelectOverlay.IsVisible = true;
            AnalyseButton.IsEnabled = true;
            AnalyseButton.Opacity = 1.0;
        }

        private void OnClearImageTapped(object sender, EventArgs e)
        {
            _selectedImagePath = null;
            SelectedImage.IsVisible = false;
            SelectedImage.Source = null;
            PlaceholderLayout.IsVisible = true;
            ReSelectOverlay.IsVisible = false;
            AnalyseButton.IsEnabled = false;
            AnalyseButton.Opacity = 0.5;
        }

        private async void OnAnalyseTapped(object sender, EventArgs e)
        {
            if (_selectedImagePath == null) return;

            // Show loading state
            AnalyseButton.Text = "⏳  Analysing...";
            AnalyseButton.IsEnabled = false;

            try
            {
                // Run ONNX inference
                var result = await _onnxService.PredictAsync(_selectedImagePath);

                // Fetch disease info from database
                result.DiseaseInfo = await _databaseService.GetDiseaseInfoAsync(result.ClassIndex);

                // Save to history
                if (result.DiseaseInfo != null)
                {
                    await _databaseService.SaveScanAsync(new ScanHistory
                    {
                        ImagePath = _selectedImagePath,
                        DisplayName = result.DiseaseInfo.DisplayName,
                        CropName = result.DiseaseInfo.CropName,
                        Confidence = result.Confidence,
                        Severity = result.DiseaseInfo.Severity,
                        InferenceMode = result.InferenceMode,
                        ColorHex = result.DiseaseInfo.ColorHex,
                        ScannedAt = DateTime.Now
                    });
                }

                // Navigate to ResultPage passing the result
                var parameters = new Dictionary<string, object>
                {
                    { "Result", result },
                    { "ImagePath", _selectedImagePath }
                };
                await Shell.Current.GoToAsync(nameof(ResultPage), parameters);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Analysis Failed", $"Error: {ex.Message}", "OK");
            }
            finally
            {
                AnalyseButton.Text = "🔍  Analyse Plant";
                AnalyseButton.IsEnabled = true;
            }
        }
    }
}