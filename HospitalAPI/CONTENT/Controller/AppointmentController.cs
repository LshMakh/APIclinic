using HospitalAPI.CONTENT.DTO_s;
using HospitalAPI.CONTENT.Packages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HospitalAPI.CONTENT.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly IPKG_APPOINTMENT _appointmentPackage;
        private readonly ILogger<AppointmentController> _logger;

        public AppointmentController(IPKG_APPOINTMENT appointmentPackage, ILogger<AppointmentController> logger)
        {
            _appointmentPackage = appointmentPackage;
            _logger = logger;
            
        }

        [HttpPost("book")]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        try
        {
            var patientId = int.Parse(User.FindFirst("PatientId")?.Value);
            var (success, appointmentId) = await _appointmentPackage.CreateAppointment(
                dto.DoctorId, patientId, dto.AppointmentDate, dto.TimeSlot, dto.Description);

            if (!success)
            {
                return BadRequest(new { message = "Time slot is not available" });
            }

            return Ok(new { appointmentId, message = "Appointment booked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment");
            return StatusCode(500, new { message = "An error occurred while booking the appointment" });
        }
    }

    [HttpPost("block")]
    public async Task<IActionResult> BlockTimeSlot([FromBody] BlockTimeSlotDto dto)
    {
        try
        {
            var doctorId = int.Parse(User.FindFirst("DoctorId")?.Value);
            var success = await _appointmentPackage.BlockTimeSlot(doctorId, dto.AppointmentDate, dto.TimeSlot);

            if (!success)
            {
                return BadRequest(new { message = "Unable to block time slot" });
            }

            return Ok(new { message = "Time slot blocked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking time slot");
            return StatusCode(500, new { message = "An error occurred while blocking the time slot" });
        }
    }

    [HttpGet("doctor")]
    public async Task<IActionResult> GetDoctorAppointments()
    {
        try
        {
            var doctorId = int.Parse(User.FindFirst("DoctorId")?.Value);
            var appointments = await _appointmentPackage.GetDoctorAppointments(doctorId);
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctor appointments");
            return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
        }
    }

        [HttpGet("patient")]
        public async Task<IActionResult> GetPatientAppointments()
        {
            try
            {
                var patientId = int.Parse(User.FindFirst("PatientId")?.Value);
                var appointments = await _appointmentPackage.GetPatientAppointments(patientId);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient appointments");
                return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
            }
        }

        [HttpPut("{id}/description")]
        public async Task<IActionResult> UpdateAppointmentDescription(int id, [FromBody] UpdateAppointmentDto dto)
        {
            try
            {
                var success = await _appointmentPackage.UpdateAppointmentDescription(id, dto.Description);
                if (!success)
                {
                    return NotFound(new { message = "Appointment not found or cannot be updated" });
                }
                return Ok(new { message = "Appointment updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment description");
                return StatusCode(500, new { message = "An error occurred while updating the appointment" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            try
            {   
                var userId = int.Parse(User.FindFirst("PatientId")?.Value);
                var isDoctor = User.IsInRole("DOCTOR");

                var success = await _appointmentPackage.DeleteAppointment(id, userId, isDoctor);
                if (!success)
                {
                    return NotFound(new { message = "Appointment not found or cannot be deleted" });
                }
                return Ok(new { message = "Appointment deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting appointment");
                return StatusCode(500, new { message = "An error occurred while deleting the appointment" });
            }
        }

        [HttpGet("available-slots/{doctorId}")]
        public async Task<IActionResult> GetAvailableSlots(int doctorId, [FromQuery] DateTime date)
        {
            try
            {
                var slots = await _appointmentPackage.GetAvailableSlots(doctorId, date);
                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available slots");
                return StatusCode(500, new { message = "An error occurred while retrieving available slots" });
            }
        }

        [HttpGet("check-availability/{doctorId}")]
        public async Task<IActionResult> CheckSlotAvailability(
            int doctorId,
            [FromQuery] DateTime date,
            [FromQuery] string timeSlot)
        {
            try
            {
                var isAvailable = await _appointmentPackage.CheckSlotAvailability(doctorId, date, timeSlot);
                return Ok(new { isAvailable });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking slot availability");
                return StatusCode(500, new { message = "An error occurred while checking slot availability" });
            }
        }

    }
}
