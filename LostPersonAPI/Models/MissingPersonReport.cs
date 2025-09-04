using System;
using System.ComponentModel.DataAnnotations;
namespace LostPersonAPI.Models
{
    public class MissingPersonReport
    {
        [Key]
        public int ReportID { get; set; }
        public int ReportedBy { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string? Gender { get; set; }
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public string? SkinColor { get; set; }
        public string? Clothing { get; set; }
        public string? MedicalCondition { get; set; }
        public DateTime LastSeenDate { get; set; }
        public double? LastSeenLatitude { get; set; }
        public double? LastSeenLongitude { get; set; }
        public string? Status { get; set; }
        public DateTime ReportingDate { get; set; }
        public string? PhotoUrl { get; set; }
        public string? EyeColor { get; set; }
        public string? HairColor { get; set; }
        public bool HasGlasses { get; set; }
        public string? ScarsMarks { get; set; }
        public string? UniqueCharacteristics { get; set; }
    }
}
