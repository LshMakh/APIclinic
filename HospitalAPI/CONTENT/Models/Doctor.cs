namespace HospitalAPI.Models
{
    public class Doctor:User
    {
        public int? DoctorId { get; set; }
        public string Specialty { get; set; }
        public int? Rating { get; set; }
        public string? CvUrl { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
