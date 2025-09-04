using System;

namespace LostPersonAPI.Models
{
    public class Admin
    {
        public int ID { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string Password { get; set; }
        public DateTime RegisteredDate { get; set; }
    }
}
