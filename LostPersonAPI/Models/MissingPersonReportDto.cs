namespace LostPersonAPI.Models
{
    // This object represents the data coming in from the frontend form
    public class MissingPersonReportDto
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Status { get; set; } = "Active"; // Default status
        public DateTime LastSeenDate { get; set; }
        public double LastSeenLatitude { get; set; }
        public double LastSeenLongitude { get; set; }
        public string? PhotoUrl { get; set; }
        public int Height { get; set; }
        public int Weight { get; set; }
        public string SkinColor { get; set; }
        public string Clothing { get; set; }
        public string? MedicalCondition { get; set; }
    }
}
