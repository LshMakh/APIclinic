namespace HospitalAPI.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public string DoctorName { get; set; }
        public string PatientName { get; set; }
        public int UserId { get; set; } // Patient ID
        public int DoctorId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public string TimeSlot { get; set; }
        public string Description { get; set; } // "Scheduled" or "Cancelled"
    }
}
