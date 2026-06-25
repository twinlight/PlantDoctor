using PlantDoctor.Models;

namespace PlantDoctor.Views
{
    [QueryProperty(nameof(Result), "Result")]
    [QueryProperty(nameof(ImagePath), "ImagePath")]
    public partial class ResultPage : ContentPage
    {
        private PredictionResult? _result;
        private string? _imagePath;

        public PredictionResult? Result
        {
            get => _result;
            set
            {
                _result = value;
                PopulateUI();
            }
        }

        public string? ImagePath
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                if (!string.IsNullOrEmpty(value))
                    ScannedImage.Source = ImageSource.FromFile(value);
            }
        }

        public ResultPage()
        {
            InitializeComponent();
        }

        private void PopulateUI()
        {
            if (_result == null) return;

            var info = _result.DiseaseInfo;
            if (info == null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Diagnosis badge
                var badgeColor = Color.FromArgb(info.ColorHex);
                DiagnosisBadge.BackgroundColor = badgeColor;

                CropLabel.Text = info.CropName.ToUpper();
                DiseaseNameLabel.Text = info.DisplayName;
                SeverityLabel.Text = $"Severity: {info.Severity}";
                ConfidenceLabel.Text = $"{_result.Confidence * 100:F0}%";

                // Show warning for moderate confidence
                if (_result.Confidence < 0.75f)
                {
                    ConfidenceWarningBorder.IsVisible = true;
                    ConfidenceWarningLabel.Text = $"Moderate confidence ({_result.Confidence * 100:F0}%) — consider retaking with better lighting";
                }
                else
                {
                    ConfidenceWarningBorder.IsVisible = false;
                }

                // Severity icon
                SeverityIcon.Text = info.Severity switch
                {
                    "Healthy" => "✅",
                    "Mild" => "⚠️",
                    "Moderate" => "🟠",
                    "Severe" => "🔴",
                    _ => "❓"
                };

                // Inference info
                ModeIcon.Text = _result.InferenceMode == "Online" ? "🌐" : "📱";
                ModeLabel.Text = $"{_result.InferenceMode} inference";
                SpeedLabel.Text = $"{_result.InferenceTimeMs} ms";

                // Content
                DescriptionLabel.Text = info.Description;
                SymptomsLabel.Text = info.Symptoms;
                PreventiveLabel.Text = info.PreventiveMeasures;

                // Hide treatment section if healthy
                if (info.Severity == "Healthy")
                {
                    TreatmentSection.IsVisible = false;
                }
                else
                {
                    TreatmentSection.IsVisible = true;
                    ChemicalLabel.Text = info.ChemicalTreatment;
                    OrganicLabel.Text = info.OrganicTreatment;
                }
            });
        }

        private async void OnScanAnotherTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void OnViewHistoryTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(HistoryPage));
        }
    }
}   