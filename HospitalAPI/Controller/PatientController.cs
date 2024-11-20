using AuthProjWebApi.Auth;
using HospitalAPI.Models;
using HospitalAPI.Packages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HospitalAPI.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        IPKG_PATIENT package;
        private readonly ILogger<UserController> _logger;
        private readonly IJwtManager jwtManager;

        public PatientController(IPKG_PATIENT package, IJwtManager jwtManager,ILogger<UserController>logger)
        {
            this.package = package;
            this.jwtManager = jwtManager;
            _logger = logger;
        }
        [HttpPost]
        public IActionResult RegisterPatient(User user)
        {
            try
            {
                package.RegisterPatient(user);
                return Ok(new { message = "Patient registered successfully" });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already exists"))
                {
                    return Conflict(new { message = ex.Message });
                }
                _logger.LogError(ex, "Error registering patient");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while registering the patient" });
            }
        }

    }
}
