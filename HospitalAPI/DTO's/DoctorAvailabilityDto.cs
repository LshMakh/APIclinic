namespace HospitalAPI.DTO_s
{
    public class DoctorAvailabilityDto
    {
        public DateTime Date { get; set; }
        public List<TimeSlotDto> TimeSlots { get; set; }
    }
}
