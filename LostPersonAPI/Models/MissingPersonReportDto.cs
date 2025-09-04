namespace LostPersonAPI.Models
{
    public class MissingPersonReportDto
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime LastSeenDate { get; set; }
        public double LastSeenLatitude { get; set; }
        public double LastSeenLongitude { get; set; }
        public string? PhotoUrl { get; set; }
        public int Height { get; set; }
        public int Weight { get; set; }
        public string SkinColor { get; set; }
        public string Clothing { get; set; }
        public string? MedicalCondition { get; set; }
        public string? EyeColor { get; set; }
        public string? HairColor { get; set; }
        public bool HasGlasses { get; set; }
        public string? ScarsMarks { get; set; }
        public string? UniqueCharacteristics { get; set; }
    }
}
