using PlantDoctor.Models;

namespace PlantDoctor.Services
{
    public class InferenceCoordinator
    {
        private readonly OnnxInferenceService _onnxService;
        private readonly ApiInferenceService _apiService;
        private readonly ConnectivityService _connectivityService;
        private readonly DatabaseService _databaseService;

        public InferenceCoordinator(
            OnnxInferenceService onnxService,
            ApiInferenceService apiService,
            ConnectivityService connectivityService,
            DatabaseService databaseService)
        {
            _onnxService = onnxService;
            _apiService = apiService;
            _connectivityService = connectivityService;
            _databaseService = databaseService;
        }

        public async Task<PredictionResult> PredictAsync(string imagePath)
        {
            PredictionResult result;

            if (_connectivityService.IsConnected)
            {
                try
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[COORDINATOR] Online — using FastAPI");
#endif
                    result = await _apiService.PredictAsync(imagePath);
                }
                catch (Exception ex)
                {
                    // API failed — fall back to ONNX silently
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[COORDINATOR] API failed ({ex.Message}), falling back to ONNX");
#endif
                    result = await _onnxService.PredictAsync(imagePath);
                    result.InferenceMode = "Offline (fallback)";
                }
            }
            else
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[COORDINATOR] Offline — using ONNX");
#endif
                result = await _onnxService.PredictAsync(imagePath);
            }

            // Attach disease info from local database
            result.DiseaseInfo = await _databaseService.GetDiseaseInfoAsync(result.ClassIndex);

            return result;
        }
    }
}