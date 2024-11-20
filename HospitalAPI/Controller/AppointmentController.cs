using HospitalAPI.DTO_s;
using HospitalAPI.Packages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HospitalAPI.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly IPKG_APPONTMENT package;
        private readonly ILogger<AppointmentController> logger;

        public AppointmentController(IPKG_APPONTMENT package, ILogger<AppointmentController> logger)
        {
            this.package = package;
            this.logger = logger;
        }

        [HttpPost]
        public IActionResult CreateAppointment([FromBody] CreateAppointmentDTO dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                dto.PatientId = userId;

                var appointmentId = package.CreateAppointment(dto);
                return Ok(appointmentId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating appointment");
                return StatusCode(500, "An error occurred while creating the appointment");
            }
        }


        [HttpGet("doctor/{doctorId}")]
        public IActionResult GetDoctorAppointments(int doctorId)
        {
            try
            {
              

                var appointments =  package.GetDoctorAppointments(
                    doctorId
                );

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting doctor appointments");
                return StatusCode(500, "An error occurred while retrieving appointments");
            }
        }

        [HttpGet("patient/{patientId}")]
        public IActionResult GetPatientAppointments(int patientId)
        {
            try
            {
               

                var appointments = package.GetPatientAppointments(
                    patientId);

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting patient appointments");
                return StatusCode(500, "An error occurred while retrieving appointments");
            }
        }


    }
}
