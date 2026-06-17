namespace PlantDoctor.Models
{
    public class ApiResponse
    {
        public int ClassIndex { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public long InferenceTimeMs { get; set; }
    }
}