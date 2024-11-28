namespace HospitalAPI.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string TimeSlot { get; set; }
        public string Description { get; set; }
        public bool IsBlocked { get; set; }

        // Navigation properties for frontend display
        public string DoctorFirstName { get; set; }
        public string DoctorLastName { get; set; }
        public string DoctorSpecialty { get; set; }
        public string PatientFirstName { get; set; }
        public string PatientLastName { get; set; }
    }
}
