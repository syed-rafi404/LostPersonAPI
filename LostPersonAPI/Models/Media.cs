using System;
using System.ComponentModel.DataAnnotations;
namespace LostPersonAPI.Models
{
    public class Media
    {
        public int MediaID { get; set; }
        public int ReportID { get; set; } // Foreign Key
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public string Status { get; set; }
        public DateTime UploadTime { get; set; }
    }
}
