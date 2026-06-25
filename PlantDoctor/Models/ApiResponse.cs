using System.Text.Json.Serialization;

namespace PlantDoctor.Models
{
    public class ApiResponse
    {
        [JsonPropertyName("class_index")]
        public int ClassIndex { get; set; }

        [JsonPropertyName("class_name")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }

        [JsonPropertyName("inference_time_ms")]
        public long InferenceTimeMs { get; set; }

        [JsonPropertyName("all_probabilities")]
        public Dictionary<string, float>? AllProbabilities { get; set; }
    }
}