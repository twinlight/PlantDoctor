namespace PlantDoctor.Models
{
    public class PredictionResult
    {
        public int ClassIndex { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string InferenceMode { get; set; } = string.Empty; // "Offline" or "Online"
        public long InferenceTimeMs { get; set; }
        public DiseaseInfo? DiseaseInfo { get; set; }
    }
}