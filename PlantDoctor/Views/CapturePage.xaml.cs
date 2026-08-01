using PlantDoctor.Helpers;
using PlantDoctor.Models;
using PlantDoctor.Services;

namespace PlantDoctor.Views
{
    public partial class CapturePage : ContentPage
    {
        private string? _selectedImagePath;
        private readonly InferenceCoordinator _coordinator;
        private readonly DatabaseService _databaseService;

        public CapturePage()
        {
            InitializeComponent();
            _coordinator = ServiceHelper.GetRequiredService<InferenceCoordinator>();
            _databaseService = ServiceHelper.GetRequiredService<DatabaseService>();
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
            if (!await EnsureCameraPermissionAsync())
                return;

            try
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = "Capture plant leaf"
                });
                if (photo != null)
                    await LoadImageAsync(photo);
            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlert("Not Supported", "Camera is not available on this device.", "OK");
            }
            catch (PermissionException)
            {
                await DisplayAlert("Permission Denied", "Camera permission is required.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CapturePage] Camera error: {ex}");
                await DisplayAlert("Error", $"Could not open camera: {ex.Message}", "OK");
            }
        }

        private async Task<bool> EnsureCameraPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Camera>();

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission Denied", "Camera permission is required.", "OK");
                return false;
            }

            return true;
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
            var extension = Path.GetExtension(photo.FileName);
            if (string.IsNullOrEmpty(extension))
                extension = ".jpg";

            var localPath = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid():N}{extension}");
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

            AnalyseButton.Text = "⏳  Analysing...";
            AnalyseButton.IsEnabled = false;

            try
            {
                // Coordinator auto-picks online (FastAPI) or offline (ONNX)
                var result = await _coordinator.PredictAsync(_selectedImagePath);

                // Confidence threshold
                if (result.Confidence < 0.6f)
                {
                    await DisplayAlert(
                        "📷 Cannot Identify",
                        $"Confidence too low ({result.Confidence * 100:F0}%) to make a reliable diagnosis.\n\nPlease:\n• Ensure it is a plant leaf\n• Fill the frame with the leaf\n• Use natural daylight\n• Avoid blurry or dark images",
                        "Try Again");
                    return;
                }

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
