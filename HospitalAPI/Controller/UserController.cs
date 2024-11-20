using AuthProjWebApi.Auth;
using HospitalAPI.DTO_s;
using HospitalAPI.Models;
using HospitalAPI.Packages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HospitalAPI.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        IPKG_USERS package;
        private readonly IJwtManager jwtManager;
        private readonly ILogger<UserController> _logger;

        public UserController(IPKG_USERS package, IJwtManager jwtManager, ILogger<UserController> logger)
        {
            this.package = package;
            this.jwtManager = jwtManager;
            _logger = logger;
        }

      
        [HttpPost]
        public IActionResult Authenticate([FromBody] UserLoginDto logindata)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = package.Authenticate(logindata);

                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                var token = jwtManager.GetToken(user);
                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication failed for user {Email}", logindata.Email);
                return StatusCode(500, new { message = "Authentication failed. Please try again later." });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetUserInfo(int id)
        {
            try
            {
                var user = package.GetUserInfo(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user information for ID: {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving user information" });
            }
        }

        [HttpGet("check-email/{email}")]
        public IActionResult CheckEmailExists(string email)
        {
            try
            {
                bool exists = package.CheckUserEmailExists(email);
                return Ok(new { exists = exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence for {Email}", email);
                return StatusCode(500, new { message = "An error occurred while checking email existence" });
            }
        }
        [HttpPost("change-password")]
        public IActionResult ChangePassword([FromBody] PasswordChangeDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var success = package.ChangePassword(userId, model.CurrentPassword, model.NewPassword);

                if (!success)
                {
                    return BadRequest(new { message = "Current password is incorrect" });
                }

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}",
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "An error occurred while changing password" });
            }
        }
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] string email)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var success = package.ResetPassword(email);

                if (!success)
                {
                    // Don't reveal whether the email exists or not for security
                    return Ok(new { message = "If your email is registered, you will receive a password reset email shortly." });
                }

                return Ok(new { message = "If your email is registered, you will receive a password reset email shortly." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password request for email {Email}", email);
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }

    }

}

