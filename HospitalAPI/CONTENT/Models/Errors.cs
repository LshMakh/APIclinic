namespace HospitalAPI.CONTENT.Models
{
    public class ApiErrorResponse
    {
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public bool IsError => true;
    }

  
    public class BusinessException : Exception
    {
        public string ErrorCode { get; }

        public BusinessException(string message, string errorCode = "BUSINESS_ERROR")
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    
    public static class ErrorCodes
    {
        public const string NotFound = "NOT_FOUND";
        public const string ValidationError = "VALIDATION_ERROR";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Conflict = "CONFLICT";
        public const string DatabaseError = "DATABASE_ERROR";
        public const string GeneralError = "GENERAL_ERROR";

       
        public const string SlotNotAvailable = "SLOT_NOT_AVAILABLE";
        public const string InvalidAppointmentDate = "INVALID_APPOINTMENT_DATE";
        public const string AppointmentNotFound = "APPOINTMENT_NOT_FOUND";

       
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string EmailAlreadyExists = "EMAIL_EXISTS";
    }
}
