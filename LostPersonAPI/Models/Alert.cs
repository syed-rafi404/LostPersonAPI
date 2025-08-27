using System.ComponentModel.DataAnnotations;
namespace LostPersonAPI.Models
{
    public class Alert
    {
        public int AlertID { get; set; }
        public int ReportID { get; set; } // Foreign Key
        public int NewMessageTo { get; set; } // Foreign Key
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public int? FoundMessageTo { get; set; }
    }
}
