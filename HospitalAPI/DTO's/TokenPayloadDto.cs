using Microsoft.AspNetCore.Identity;

namespace HospitalAPI.DTO_s
{
    public class TokenPayloadDto
    {
        public int UserId { get; set; }
        public string Role {  get; set; }
    }
}
