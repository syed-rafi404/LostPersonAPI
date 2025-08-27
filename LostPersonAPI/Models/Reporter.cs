namespace LostPersonAPI.Models
{
    public class Reporter
    {
        public int ID { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? NID { get; set; } // National ID
    }
}
