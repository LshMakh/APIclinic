using Oracle.ManagedDataAccess.Client;
using System.Data;
using AuthProjWebApi.Packages;
using HospitalAPI.Services;

namespace HospitalAPI.Packages
{
    public interface IPKG_VERIFICATION
    {
        Task<bool> GenerateAndStoreVerificationCodeAsync(string email);
        Task<bool> VerifyCodeAsync(string email, string code);
    }

    public class PKG_VERIFICATION : PKG_BASE, IPKG_VERIFICATION
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<PKG_VERIFICATION> _logger;

        public PKG_VERIFICATION(
            IEmailService emailService,
            ILogger<PKG_VERIFICATION> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<bool> GenerateAndStoreVerificationCodeAsync(string email)
        {
            var code = GenerateRandomCode();

            try
            {
                using (var conn = new OracleConnection(ConnStr))
                {
                    await conn.OpenAsync();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "olerning.PKG_LSH_VERIFICATION.create_verification_code";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value = email;
                        cmd.Parameters.Add("p_code", OracleDbType.Varchar2).Value = code;

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Send email only after successful database operation
                await _emailService.SendVerificationEmailAsync(email, code);
                return true;
            }
            catch (OracleException ex)
            {
                _logger.LogError(ex, "Database error while storing verification code for {Email}", email);
                throw new Exception("Failed to store verification code", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in verification process for {Email}", email);
                throw;
            }
        }

        public async Task<bool> VerifyCodeAsync(string email, string code)
        {
            try
            {
                using (var conn = new OracleConnection(ConnStr))
                {
                    await conn.OpenAsync();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "olerning.PKG_LSH_VERIFICATION.verify_code";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value = email;
                        cmd.Parameters.Add("p_code", OracleDbType.Varchar2).Value = code;
                        cmd.Parameters.Add("p_is_valid", OracleDbType.Int32).Direction = ParameterDirection.Output;

                        await cmd.ExecuteNonQueryAsync();

                        return Convert.ToInt32(cmd.Parameters["p_is_valid"].Value.ToString()) == 1;
                    }
                }
            }
            catch (OracleException ex)
            {
                _logger.LogError(ex, "Database error while verifying code for {Email}", email);
                throw new Exception("Failed to verify code", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in verification process for {Email}", email);
                throw;
            }
        }

        private string GenerateRandomCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}