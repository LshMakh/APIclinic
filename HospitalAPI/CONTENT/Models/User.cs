namespace HospitalAPI.Models
{
    public class User
    {
        public int? UserId { get; set; }
        public int? PatientId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string PersonalNumber { get; set; }
        public string? Role { get; set; } // "Patient", "Doctor" or "Admin"
       
    }
}
