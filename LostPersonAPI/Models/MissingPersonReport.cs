using System;
using System.ComponentModel.DataAnnotations;
namespace LostPersonAPI.Models
{
    public class MissingPersonReport
    {

        [Key]
        public int ReportID { get; set; }
        public int ReportedBy { get; set; } // Foreign Key
        public int? ApprovedBy { get; set; } // Foreign Key, can be null
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
    }
}
