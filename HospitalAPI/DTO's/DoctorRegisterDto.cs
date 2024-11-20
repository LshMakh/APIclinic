namespace HospitalAPI.DTO_s
{
    public class DoctorRegisterDto:UserRegisterDto
    {
        public string Specialization { get; set; }
        public string CvUrl { get; set; }
        public string PhotoUrl { get; set; }
    }
}
