namespace HospitalAPI.DTO_s
{
    public class EmailVerificationDto
    {
        public string Email { get; set; }
        public string? VerificationCode { get; set; }
        public DateTime ExpirationTime { get; set; }
        public int AttemptCount { get; set; }
    }
}
