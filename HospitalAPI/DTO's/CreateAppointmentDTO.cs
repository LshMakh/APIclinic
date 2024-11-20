using System.ComponentModel.DataAnnotations;

namespace HospitalAPI.DTO_s
{
    public class CreateAppointmentDTO
    {
        [Required]
        public int DoctorId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [RegularExpression(@"^(09|1[0-6]):00 - (10|1[0-7]):00$",
        ErrorMessage = "Time slot must be in format 'HH:00 - HH:00' between 09:00 and 17:00")]
        public string TimeSlot { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }
    }
}
