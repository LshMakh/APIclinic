using AuthProjWebApi.Auth;
using HospitalAPI.CONTENT.DTO_s;
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
        //[HttpPost]
        //public IActionResult RegisterDoctor(Doctor doctor)
        //{
        //    try
        //    {
        //        package.RegisterDoctor(doctor);
        //        return Ok(new { message = "Doctor registered successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        if (ex.Message.Contains("already exists"))
        //        {
        //            return Conflict(new { message = ex.Message });
        //        }
        //        _logger.LogError(ex, "Error registering doctor");
        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new { message = "An error occurred while registering the doctor" });
        //    }
        //}
        [HttpPost]
        public async Task<IActionResult> RegisterDoctor([FromForm] Doctor doctor, IFormFile photo, IFormFile cv)
        {
            try
            {
                var (success, message) = await package.RegisterDoctor(doctor, photo, cv);
                if (!success)
                {
                    return BadRequest(new { message });
                }
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering doctor");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while registering the doctor" });
            }
        }

        [HttpGet("photo/{id}")]
        public async Task<IActionResult> GetDoctorPhoto(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving photo for doctor ID: {Id}", id);
                var photoData = await package.GetDoctorPhoto(id);

                _logger.LogInformation("Photo data length: {Length} bytes", photoData?.Length ?? 0);

                if (photoData == null || photoData.Length == 0)
                {
                    _logger.LogWarning("No photo found for doctor ID: {Id}", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully retrieved photo for doctor ID: {Id}", id);
                return File(photoData, "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctor photo for ID: {Id}", id);
                return StatusCode(500, "Error retrieving photo");
            }
        }

        [HttpGet("cv/{id}")]
        public async Task<IActionResult> GetDoctorCV(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving CV for doctor ID: {Id}", id);
                var cvData = await package.GetDoctorCV(id);

                if (cvData == null || cvData.Length == 0)
                {
                    _logger.LogWarning("No CV found for doctor ID: {Id}", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully retrieved CV for doctor ID: {Id}", id);
                return File(cvData, "application/pdf", $"doctor_cv_{id}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctor CV for ID: {Id}", id);
                return StatusCode(500, "Error retrieving CV");
            }
        }
        [HttpPost("extract-cv/{doctorId}")]
        public async Task<IActionResult> ExtractCvText(int doctorId)
        {
            try
            {
                var cvText = await package.ExtractAndStoreCvTextAsync(doctorId);
                return Ok(new { text = cvText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting CV text for doctor {DoctorId}", doctorId);
                return StatusCode(500, "An error occurred while processing the CV");
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

        [HttpGet("specialty-count/{categoryName}")]
        public ActionResult<int> GetSpecialtyCount(string categoryName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    return BadRequest("Category name cannot be empty");
                }

                var count = package.GetCategoryCount(categoryName);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting specialty count for {category}", categoryName);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, [FromForm] UpdateDoctorDto doctor, IFormFile? photo = null, IFormFile? cv = null)
        {
            try
            {
                _logger.LogInformation("Updating doctor with ID {DoctorId}", id);

              

                var (success, message) = await package.UpdateDoctor(id, doctor, photo, cv);

                if (!success)
                {
                    return BadRequest(new { message });
                }

                var updatedDoctor = package.GetDoctorById(id);
                return Ok(new { message = "Doctor Updated Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor with ID {DoctorId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the doctor" });
            }
        }

    }                                                                                                                                                               
}
