using SQLite;

namespace PlantDoctor.Models
{
    [Table("DiseaseInfo")]
    public class DiseaseInfo
    {
        [PrimaryKey]
        public int ClassIndex { get; set; }

        public string ClassName { get; set; } = string.Empty;
        public string CropName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // "Healthy", "Mild", "Moderate", "Severe"
        public string Description { get; set; } = string.Empty;
        public string Symptoms { get; set; } = string.Empty;
        public string ChemicalTreatment { get; set; } = string.Empty;
        public string OrganicTreatment { get; set; } = string.Empty;
        public string PreventiveMeasures { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty; // for UI badge color
    }
}