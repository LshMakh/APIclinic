using AuthProjWebApi.Packages;
using HospitalAPI.DTO_s;
using HospitalAPI.Models;
using HospitalAPI.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Linq.Expressions;
using System.Text;

namespace HospitalAPI.Packages
{
    public interface IPKG_USERS
    {

        public TokenPayloadDto? Authenticate(UserLoginDto logindata);
        public object? GetUserInfo(int userId);
        public bool CheckUserEmailExists(string email);
        public bool ChangePassword(int id, string currentPass, string newPass);
        public bool ResetPassword(string email);
        public bool ChangePasswordAdmin(int id, string password);
    }
    public class PKG_USERS : PKG_BASE, IPKG_USERS
    {
        private readonly ILogger<PKG_USERS> _logger;
        private readonly IEmailService _emailService;

        public PKG_USERS(ILogger<PKG_USERS> logger, IEmailService emailService)
        {
            _emailService = emailService;
            _logger = logger;
        }
        private string GenerateRandomPassword()
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var random = new Random();
            var password = new StringBuilder();

            password.Append(upperCase[random.Next(upperCase.Length)]);
            password.Append(lowerCase[random.Next(lowerCase.Length)]);
            password.Append(numbers[random.Next(numbers.Length)]);
            password.Append(special[random.Next(special.Length)]);

            const string allChars = upperCase + lowerCase + numbers + special;
            for (int i = 0; i < 8; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            return new string(password.ToString().ToCharArray()
                .OrderBy(x => random.Next()).ToArray());
        }
        public bool ResetPassword(string email)
        {
            using (OracleConnection conn = new OracleConnection(ConnStr))
            {
                try
                {
                    conn.Open();
                    using (OracleCommand cmd = conn.CreateCommand())
                    {
                        string newPassword = GenerateRandomPassword();

                        cmd.CommandText = "olerning.PKG_LSH_USERS.reset_password";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value = email;
                        cmd.Parameters.Add("p_new_password", OracleDbType.Varchar2).Value = newPassword;
                        cmd.Parameters.Add("p_success", OracleDbType.Int32).Direction = ParameterDirection.Output;

                         cmd.ExecuteNonQuery();

                        bool success = Convert.ToInt32(cmd.Parameters["p_success"].Value.ToString()) == 1;

                        if (success)
                        {
                             _emailService.SendPasswordResetEmailAsync(email, newPassword);
                        }

                        return success;
                    }
                }
                catch (OracleException ex)
                {
                    _logger.LogError(ex, "Database error resetting password for email {Email}", email);
                    throw new Exception("Database error while resetting password", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resetting password for email {Email}", email);
                    throw;
                }
            }
        }
        public bool CheckUserEmailExists(string email)
        {
            using (OracleConnection conn = new OracleConnection(ConnStr))
            {
                try
                {
                    conn.Open();
                    using (OracleCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "olerning.PKG_LSH_USERS.get_user_by_email";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value = email;
                        cmd.Parameters.Add("p_exists", OracleDbType.Int32).Direction = ParameterDirection.Output;

                        cmd.ExecuteNonQuery();

                        int exists = Convert.ToInt32(cmd.Parameters["p_exists"].Value.ToString());
                        return exists == 1;
                    }
                }
                catch (OracleException ex)
                {
                    _logger.LogError(ex, "Database error checking email existence for {Email}", email);
                    throw new Exception($"Database error checking email existence: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking email existence for {Email}", email);
                    throw new Exception($"Error checking email existence: {ex.Message}", ex);
                }
            }
        }
        public object? GetUserInfo(int userId)
        {
            using (OracleConnection conn = new OracleConnection(ConnStr))
            {
                try
                {
                    conn.Open();
                    using (OracleCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "olerning.PKG_LSH_USERS.get_user_details";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("p_userid", OracleDbType.Int32).Value = userId;
                        cmd.Parameters.Add("p_result", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string role = reader["role"].ToString().ToUpper();

                                switch (role)
                                {
                                    case "DOCTOR":
                                        return new Doctor
                                        {
                                            UserId = reader.GetInt32(reader.GetOrdinal("userid")),
                                            Rating = reader.GetInt32(reader.GetOrdinal("rating")),
                                            Role = role,
                                            DoctorId = reader.GetInt32(reader.GetOrdinal("doctorid")),
                                            FirstName = reader["firstname"].ToString(),
                                            LastName = reader["lastname"].ToString(),
                                            Email = reader["email"].ToString(),
                                            PersonalNumber = reader["personalnumber"].ToString(),
                                            Specialty = reader["specialty"].ToString(),
                                            //PhotoUrl = reader["photourl"].ToString(),
                                            //CvUrl = reader["cvurl"].ToString()
                                        };

                                    case "PATIENT":
                                        return new User
                                        {
                                            UserId = reader.GetInt32(reader.GetOrdinal("userid")),
                                            PatientId = reader.GetInt32(reader.GetOrdinal("patientid")),
                                            Role = role,
                                            FirstName = reader["firstname"].ToString(),
                                            LastName = reader["lastname"].ToString(),
                                            Email = reader["email"].ToString(),
                                            PersonalNumber = reader["personalnumber"].ToString()

                                        };
                                    case "ADMIN":
                                        return new AdminDetailsDto
                                        {
                                            UserId = reader.GetInt32(reader.GetOrdinal("userid")),
                                            Role = role
                                            
                                        };

                                    default:
                                        _logger.LogWarning("Unknown role {Role} for user {UserId}", role, userId);
                                        return null;
                                }
                            }
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving user information for userId: {UserId}", userId);
                    throw;
                }
            }
        }




        public TokenPayloadDto? Authenticate(UserLoginDto loginData)
        {
            using (OracleConnection conn = new OracleConnection(ConnStr))
            {
                try
                {
                    conn.Open();
                    using (OracleCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "olerning.PKG_LSH_USERS.authenticate_user";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value = loginData.Email;
                        cmd.Parameters.Add("p_password", OracleDbType.Varchar2).Value = loginData.Password;
                        cmd.Parameters.Add("p_result", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (reader["USERID"] != DBNull.Value)
                                {
                                    return new TokenPayloadDto
                                    {
                                        UserId = Convert.ToInt32(reader["USERID"]),
                                        Role = reader["ROLE"].ToString(),
                                        PatientId = reader["patientid"] != DBNull.Value ? Convert.ToInt32(reader["patientid"]):null,
                                        DoctorId = reader["doctorid"] != DBNull.Value ? Convert.ToInt32(reader["doctorid"]):null
                                    };
                                }
                            }
                            return null;
                        }
                    }
                }
                catch (OracleException ex)
                {
                    throw new Exception($"Database error during authentication: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error during authentication: {ex.Message}", ex);
                }
            }
        }
        public bool ChangePasswordAdmin(int userId, string password)
        {
            using(OracleConnection conn = new OracleConnection(ConnStr))
            {
                try
                {
                    conn.Open();
                    using(OracleCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "olerning.PKG_LSH_USERS.change_password_admin";
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = userId;
                        cmd.Parameters.Add("p_password", OracleDbType.Varchar2).Value = password;
                        cmd.Parameters.Add("p_success", OracleDbType.Int32).Direction = ParameterDirection.Output;

                        cmd.ExecuteNonQuery();

                        return Convert.ToInt32(cmd.Parameters["p_success"].Value.ToString()) == 1;
                    }
                }
                catch (OracleException ex)
                {
                    _logger.LogError(ex, "Database error changing password for user {UserId}", userId);
                    throw new Exception("Database error while changing password", ex);
                }
            }
        }
        public bool ChangePassword(int userId, string currentPassword, string newPassword)
        {
            using (OracleConnection conn = new OracleConnection(ConnStr))
            {
                try
                {
                    conn.Open();
                    using (OracleCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "olerning.PKG_LSH_USERS.change_password";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("p_user_id", OracleDbType.Int32).Value = userId;
                        cmd.Parameters.Add("p_current_password", OracleDbType.Varchar2).Value = currentPassword;
                        cmd.Parameters.Add("p_new_password", OracleDbType.Varchar2).Value = newPassword;
                        cmd.Parameters.Add("p_success", OracleDbType.Int32).Direction = ParameterDirection.Output;

                        cmd.ExecuteNonQuery();

                        return Convert.ToInt32(cmd.Parameters["p_success"].Value.ToString()) == 1;
                    }
                }
                catch (OracleException ex)
                {
                    _logger.LogError(ex, "Database error changing password for user {UserId}", userId);
                    throw new Exception("Database error while changing password", ex);
                }
            }
        }
    }
    } 

    

