using PlantDoctor.Models;
using System.Diagnostics;
using System.Text.Json;

namespace PlantDoctor.Services
{
    public class ApiInferenceService : IInferenceService   // ← was : InferenceCoordinator
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://twilight11-plantdoctor-api.hf.space";

        public ApiInferenceService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<PredictionResult> PredictAsync(string imagePath)
        {
            var stopwatch = Stopwatch.StartNew();

            using var imageStream = File.OpenRead(imagePath);
            var fileName = Path.GetFileName(imagePath);
            var extension = Path.GetExtension(imagePath).ToLower();
            var mimeType = extension == ".png" ? "image/png" : "image/jpeg";

            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync($"{BaseUrl}/predict", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            stopwatch.Stop();

            if (apiResponse == null)
                throw new Exception("Empty response from API.");

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[API] Response: {responseBody}");
#endif

            return new PredictionResult
            {
                ClassIndex = apiResponse.ClassIndex,
                ClassName = apiResponse.ClassName,
                Confidence = apiResponse.Confidence,
                InferenceMode = "Online",
                InferenceTimeMs = apiResponse.InferenceTimeMs
            };
        }
    }
}