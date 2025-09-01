using System;

namespace LostPersonAPI.Models
{

    public class Assigned
    {

        public int Id { get; set; }
        public int ReportID { get; set; } 
        public int VolunteerID { get; set; } 
        public DateTime AssignedAt { get; set; }
        public string Status { get; set; }
    }
}
