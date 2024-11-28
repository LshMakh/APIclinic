using System.ComponentModel.DataAnnotations;

namespace HospitalAPI.CONTENT.DTO_s
{
    public class CreateAppointmentDto
    {
       
        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public string TimeSlot { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }
    }

    public class UpdateAppointmentDto
    {
        [Required]
        [MaxLength(500)]
        public string Description { get; set; }
    }

    public class TimeSlotDto
    {
        public string TimeSlot { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsBlocked { get; set; }
        public int? PatientId { get; set; }
    }

    public class BlockTimeSlotDto
    {
        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public string TimeSlot { get; set; }
    }
}
