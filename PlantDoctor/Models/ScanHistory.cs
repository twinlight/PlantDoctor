using SQLite;

namespace PlantDoctor.Models
{
    [Table("ScanHistory")]
    public class ScanHistory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ImagePath { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string CropName { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string InferenceMode { get; set; } = string.Empty;
        public DateTime ScannedAt { get; set; } = DateTime.Now;
        public string ColorHex { get; set; } = string.Empty;
    }
}