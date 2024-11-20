using HospitalAPI.DTO_s;
using HospitalAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthProjWebApi.Auth
{
    public interface IJwtManager
    {
        Token GetToken(TokenPayloadDto user);
    }

    public class JwtManager : IJwtManager
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtManager> _logger;

        public JwtManager(IConfiguration configuration, ILogger<JwtManager> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Token GetToken(TokenPayloadDto user)
        {
            try
            {
                // Get JWT settings from configuration
                var key = Encoding.UTF8.GetBytes(_configuration["JWT:Key"]);
                var tokenHandler = new JwtSecurityTokenHandler();

                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Role, user.Role),
                   
                };

       

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(1), // Token expires after 1 hour
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature
                    )
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return new Token { AccessToken = tokenHandler.WriteToken(token) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating token for user {UserId}", user.UserId);
                throw;
            }
        }

    }

    public class Token
    {
        public string AccessToken { get; set; }
    }
}