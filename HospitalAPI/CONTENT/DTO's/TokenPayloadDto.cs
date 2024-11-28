using Microsoft.AspNetCore.Identity;

namespace HospitalAPI.DTO_s
{
    public class TokenPayloadDto
    {
        public int UserId { get; set; }
        public int? PatientId { get; set; }
        public int? DoctorId { get; set; }
        public string Role {  get; set; }
    }
}
