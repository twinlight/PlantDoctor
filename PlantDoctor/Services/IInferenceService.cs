using PlantDoctor.Models;

namespace PlantDoctor.Services
{
    public interface IInferenceService
    {
        Task<PredictionResult> PredictAsync(string imagePath);
    }
}