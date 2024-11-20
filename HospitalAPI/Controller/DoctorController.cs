using AuthProjWebApi.Auth;
using HospitalAPI.Models;
using HospitalAPI.Packages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HospitalAPI.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DoctorController : ControllerBase
    {
        IPKG_DOCTOR package;
        private readonly ILogger<DoctorController> _logger;
        private readonly IJwtManager jwtManager;

        public DoctorController(IPKG_DOCTOR package, IJwtManager jwtManager, ILogger<DoctorController> logger)
        {
            this.package = package;
            this.jwtManager = jwtManager;
            _logger = logger;
        }
        [HttpPost]
        public IActionResult RegisterDoctor(Doctor doctor)
        {
            try
            {
                package.RegisterDoctor(doctor);
                return Ok(new { message = "Doctor registered successfully" });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already exists"))
                {
                    return Conflict(new { message = ex.Message });
                }
                _logger.LogError(ex, "Error registering doctor");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while registering the doctor" });
            }
        }

        [HttpGet("photo/{id}")]
        public IActionResult GetDoctorPhoto(int id)
        {
            try
            {
                var photoData = package.GetDoctorPhoto(id);
                if(photoData == null)
                {
                    return NotFound();
                }
                return File(photoData, "image/jpeg");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctor photo");
                return StatusCode(500, "Error retrieving photo");
            }

        }

        [HttpGet("cv/{id}")]
        public IActionResult GetDoctorCV(int id)
        {
            try
            {
                var cvData = package.GetDoctorCV(id);
                if (cvData == null)
                {
                    return NotFound();
                }
                return File(cvData, "application/pdf"); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctor CV");
                return StatusCode(500, "Error retrieving CV");
            }
        }
        
        [HttpGet]
        public IActionResult GetDoctorCards()
        {
            try
            {
                List<Doctor> docs = package.GetDoctorCards();

                if (docs == null)
                {
                    return NotFound("No doctor records found.");
                }

                if (!docs.Any())
                {
                    return NoContent();
                }

                return Ok(docs);
            }
            catch (InvalidOperationException ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status400BadRequest,
                    new { message = "Invalid operation while retrieving doctor records.", error = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An unexpected error occurred while retrieving doctor records.", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetDoctorById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "Invalid doctor ID. ID must be greater than 0." });
                }

                Doctor doc = package.GetDoctorById(id);

                if (doc == null)
                {
                    return NotFound(new { message = $"Doctor with ID {id} not found." });
                }

                return Ok(doc);
            }
            catch (InvalidOperationException ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status400BadRequest,
                    new { message = $"Invalid operation while retrieving doctor with ID {id}.", error = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = $"An unexpected error occurred while retrieving doctor with ID {id}.", error = ex.Message });
            }
        }
        [HttpDelete("{id}")]
        public IActionResult DeleteDoctorById(int id)
        {
            try
            {
                _logger.LogInformation("Deleting doctor with ID {DoctorId}", id);
                var status = package.DeleteDoctorById(id);
                if (!status)
                {
                    _logger.LogWarning("Doctor with ID {DoctorId} not found", id);
                    return NotFound();

                }
                else
                {
                    _logger.LogInformation("Successfully deleted doctor with ID {DoctorId}", id);
                    return Ok();
                }
            }
           
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting doctor with ID {DoctorId}", id);
                return Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }
    }                                                                                                                                                               
}
