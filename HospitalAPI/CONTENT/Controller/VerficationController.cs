using Microsoft.AspNetCore.Mvc;
using HospitalAPI.DTO_s;
using HospitalAPI.Packages;

namespace HospitalAPI.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class VerificationController : ControllerBase
    {
        private readonly IPKG_VERIFICATION _verificationPackage;
        private readonly ILogger<VerificationController> _logger;

        public VerificationController(
            IPKG_VERIFICATION verificationPackage,
            ILogger<VerificationController> logger)
        {
            _verificationPackage = verificationPackage;
            _logger = logger;
        }

   
   
        [HttpPost("send")]
        public async Task<IActionResult> SendVerificationCode([FromBody] EmailVerificationDto request)
        {
            try
            {
                var result = await _verificationPackage.GenerateAndStoreVerificationCodeAsync(request.Email);
                if (!result)
                {
                    return StatusCode(500, new { message = "Failed to send verification code" });
                }
                return Ok(new { message = "Verification code sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification code to {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred while sending verification code" });
            }
        }


    
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyCode([FromBody] EmailVerificationDto request)
        {
            try
            {
                var isValid = await _verificationPackage.VerifyCodeAsync(request.Email, request.VerificationCode);
                if (!isValid)
                {
                    return BadRequest(new { message = "Invalid or expired verification code" });
                }
                return Ok(new { message = "Code verified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying code for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred while verifying code" });
            }
        }
    }
}