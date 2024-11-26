using AuthProjWebApi.Packages;
using HospitalAPI.CONTENT.DTO_s;
using HospitalAPI.Models;
using HospitalAPI.Services;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace HospitalAPI.Packages
{
    public interface IPKG_DOCTOR
    {
        //public void RegisterDoctor(Doctor doctor);
        public Task<(bool success, string message)> RegisterDoctor(Doctor doctor, IFormFile photo, IFormFile cv);
        public List<Doctor> GetDoctorCards();
        public Doctor GetDoctorById(int id);
        public bool DeleteDoctorById(int id);
        public Task<byte[]> GetDoctorPhoto(int id);
        public Task<byte[]> GetDoctorCV(int id);
        public Task<(bool success, string message)> UpdateDoctor(int doctorId, UpdateDoctorDto doctor, IFormFile? photo, IFormFile? cv);



        public int GetCategoryCount(string categoryName);
        public Task<string> ExtractAndStoreCvTextAsync(int doctorId);
    }

    public class PKG_DOCTOR:PKG_BASE,IPKG_DOCTOR
    {
        private IPKG_USERS _userPackage;
        private ILogger<PKG_PATIENT> _logger;
        private IPdfService _pdfService;

        public PKG_DOCTOR(IPKG_USERS userPackage, ILogger<PKG_PATIENT> logger, IPdfService pdfservice)
        {
            _userPackage = userPackage;
            _logger = logger;
            _pdfService = pdfservice;

        }
        private const int MAX_PHOTO_SIZE = 5 * 1024 * 1024; // 5MB
        private const int MAX_CV_SIZE = 10 * 1024 * 1024;   // 10MB
        private readonly string[] ALLOWED_PHOTO_TYPES = { "image/jpeg", "image/png" };
        private readonly string[] ALLOWED_CV_TYPES = { "application/pdf" };
        private bool ValidateFile(IFormFile file, int maxSize, string[] allowedTypes, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file.Length > maxSize)
            {
                errorMessage = $"File size exceeds maximum limit of {maxSize / (1024 * 1024)}MB";
                return false;
            }

            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                errorMessage = $"File type not allowed. Allowed types: {string.Join(", ", allowedTypes)}";
                return false;
            }

            return true;
        }
        public async Task<(bool success, string message)> RegisterDoctor(Doctor doctor, IFormFile photo, IFormFile cv)
        {
            if (_userPackage.CheckUserEmailExists(doctor.Email))
            {
                _logger.LogWarning("Attempted to register doctor with existing email: {Email}", doctor.Email);
                return (false, $"A user with email {doctor.Email} already exists.");
            }

            // Validate files
            if (!ValidateFile(photo, MAX_PHOTO_SIZE, ALLOWED_PHOTO_TYPES, out string photoError))
            {
                return (false, $"Photo validation failed: {photoError}");
            }

            if (!ValidateFile(cv, MAX_CV_SIZE, ALLOWED_CV_TYPES, out string cvError))
            {
                return (false, $"CV validation failed: {cvError}");
            }

            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_DOCTORS.register_doctor";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Convert files to byte arrays
                    using var photoStream = new MemoryStream();
                    await photo.CopyToAsync(photoStream);
                    byte[] photoData = photoStream.ToArray();

                    using var cvStream = new MemoryStream();
                    await cv.CopyToAsync(cvStream);
                    byte[] cvData = cvStream.ToArray();

                    cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value = doctor.Email;
                    cmd.Parameters.Add("p_password", OracleDbType.Varchar2).Value = doctor.Password;
                    cmd.Parameters.Add("p_first_name", OracleDbType.Varchar2).Value = doctor.FirstName;
                    cmd.Parameters.Add("p_last_name", OracleDbType.Varchar2).Value = doctor.LastName;
                    cmd.Parameters.Add("p_specialty", OracleDbType.Varchar2).Value = doctor.Specialty;
                    cmd.Parameters.Add("p_photo_data", OracleDbType.Blob).Value = photoData;
                    cmd.Parameters.Add("p_cv_data", OracleDbType.Blob).Value = cvData;
                    cmd.Parameters.Add("p_personal_number", OracleDbType.Varchar2).Value = doctor.PersonalNumber;
                    cmd.Parameters.Add("p_user_id", OracleDbType.Int32).Direction = ParameterDirection.Output;

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                        return (true, "Doctor registered successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error registering doctor");
                        return (false, "Failed to register doctor");
                    }
                }
            }
        }
        public async Task<byte[]> GetDoctorPhoto(int id)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_DOCTORS.get_doctor_photo";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;

                    var photoParam = new OracleParameter
                    {
                        ParameterName = "p_photo_data",
                        OracleDbType = OracleDbType.Blob,
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(photoParam);

                    var statusParam = new OracleParameter
                    {
                        ParameterName = "p_status",
                        OracleDbType = OracleDbType.Decimal, 
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(statusParam);

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();

                        var status = ((OracleDecimal)statusParam.Value).ToInt32();
                        if (status <= 0)
                        {
                            _logger.LogWarning("No photo found for doctor ID: {Id}", id);
                            return Array.Empty<byte>();
                        }

                        if (photoParam.Value == DBNull.Value || photoParam.Value == null)
                        {
                            return Array.Empty<byte>();
                        }

                        // Handle the BLOB data
                        if (photoParam.Value is OracleBlob blob)
                        {
                            byte[] buffer = new byte[blob.Length];
                            await blob.ReadAsync(buffer, 0, (int)blob.Length);
                            return buffer;
                        }
                        else if (photoParam.Value is byte[] byteArray)
                        {
                            return byteArray;
                        }

                        _logger.LogWarning("Unexpected photo data type for doctor ID: {Id}", id);
                        return Array.Empty<byte>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving doctor photo for ID: {Id}", id);
                        throw;
                    }
                }
            }
        }

        public async Task<byte[]> GetDoctorCV(int id)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_DOCTORS.get_doctor_cv";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new OracleParameter
                    {
                        ParameterName = "p_id",
                        OracleDbType = OracleDbType.Int32,
                        Direction = ParameterDirection.Input,
                        Value = id
                    });

                    cmd.Parameters.Add(new OracleParameter
                    {
                        ParameterName = "p_cv_data",
                        OracleDbType = OracleDbType.Blob,
                        Direction = ParameterDirection.Output
                    });

                    cmd.Parameters.Add(new OracleParameter
                    {
                        ParameterName = "p_status",
                        OracleDbType = OracleDbType.Int32,
                        Direction = ParameterDirection.Output
                    });

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();

                        // Check status first
                        var status = Convert.ToInt32(cmd.Parameters["p_status"].Value.ToString());
                        if (status <= 0)
                        {
                            _logger.LogWarning("No CV found for doctor ID: {Id}, Status: {Status}", id, status);
                            return Array.Empty<byte>();
                        }

                        var cvParam = cmd.Parameters["p_cv_data"];
                        if (cvParam.Value == DBNull.Value || cvParam.Value == null)
                        {
                            return Array.Empty<byte>();
                        }

                        // Handle the BLOB data
                        if (cvParam.Value is OracleBlob blob)
                        {
                            byte[] buffer = new byte[blob.Length];
                            await blob.ReadAsync(buffer, 0, (int)blob.Length);
                            return buffer;
                        }
                        else if (cvParam.Value is byte[] byteArray)
                        {
                            return byteArray;
                        }

                        _logger.LogWarning("Unexpected CV data type for doctor ID: {Id}", id);
                        return Array.Empty<byte>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving CV for doctor ID: {Id}", id);
                        throw;
                    }
                }
            }
        }
        public async Task<string> ExtractAndStoreCvTextAsync(int doctorId)
        {
            try
            {
                // Get the CV data
                var cvData = await GetDoctorCV(doctorId);
                if (cvData == null || cvData.Length == 0)
                {
                    throw new InvalidOperationException($"No CV found for doctor ID {doctorId}");
                }

                // Extract text from PDF
                var cvText = await _pdfService.ExtractTextFromPdfAsync(cvData);

           

                return cvText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CV for doctor {DoctorId}", doctorId);
                throw;
            }
        }
        public int GetCategoryCount(string categoryName)
        {
            using (OracleConnection conn = new OracleConnection(ConnStr))
            {
                try
                {
                    conn.Open();
                    using (OracleCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "olerning.PKG_LSH_DOCTORS.get_category_count";
                        cmd.CommandType = CommandType.StoredProcedure;

                       
                        cmd.Parameters.Add("p_category", OracleDbType.Varchar2).Value = categoryName;

                        var resultParameter = cmd.Parameters.Add("p_result", OracleDbType.Decimal);
                        resultParameter.Direction = ParameterDirection.Output;

                        cmd.ExecuteNonQuery();

                        var oracleDecimal = (OracleDecimal)resultParameter.Value;
                        return (int)oracleDecimal.Value;
                    }
                }
                catch (OracleException ex)
                {
                    _logger.LogError(ex, "Database error getting Doctor count with {category}", categoryName);
                    throw new Exception($"Database error getting doctor count: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting Doctor count with {category}", categoryName);
                    throw new Exception($"Error getting doctor count: {ex.Message}", ex);
                }
            }
        }

        //public async void RegisterDoctor(Doctor doctor)
        //{
        //    if (_userPackage.CheckUserEmailExists(doctor.Email))
        //    {
        //        _logger.LogWarning("Attempted to register doctor with existing email: {Email}", doctor.Email);
        //        throw new Exception($"A user with email {doctor.Email} already exists.");
        //    }


        //    string connstr = ConnStr;
        //    OracleConnection conn = new OracleConnection();
        //    conn.ConnectionString = connstr;
        //    conn.Open();


        //    OracleCommand cmd = conn.CreateCommand();
        //    cmd.Connection = conn;
        //    cmd.CommandText = "olerning.PKG_LSH_DOCTORS.register_doctor";
        //    cmd.CommandType = CommandType.StoredProcedure;
        //    cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value = doctor.Email;
        //    cmd.Parameters.Add("p_password", OracleDbType.Varchar2).Value = doctor.Password;
        //    cmd.Parameters.Add("p_first_name", OracleDbType.Varchar2).Value = doctor.FirstName;
        //    cmd.Parameters.Add("p_last_name", OracleDbType.Varchar2).Value = doctor.LastName;
        //    cmd.Parameters.Add("p_specialty", OracleDbType.Varchar2).Value = doctor.Specialty;
        //    cmd.Parameters.Add("p_photo_url", OracleDbType.Varchar2).Value = doctor.PhotoUrl;
        //    cmd.Parameters.Add("p_cv_url", OracleDbType.Varchar2).Value = doctor.CvUrl;
        //    cmd.Parameters.Add("p_personal_number", OracleDbType.Varchar2).Value = doctor.PersonalNumber;
        //    cmd.Parameters.Add("p_user_id", OracleDbType.Int32).Direction = ParameterDirection.Output;

        //    cmd.ExecuteNonQuery();
        //    conn.Close();
        //}
        
 
        public List<Doctor> GetDoctorCards()
        {

            List<Doctor> docs = new List<Doctor>();
            string connstr = ConnStr;


            OracleConnection conn = new OracleConnection();
            conn.ConnectionString = connstr;
            conn.Open();

            OracleCommand cmd = conn.CreateCommand();
            cmd.Connection = conn;
            cmd.CommandText = "olerning.PKG_LSH_DOCTORS.get_doctor_cards";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("p_result", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            OracleDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Doctor doc = new Doctor();
                doc.UserId = reader.GetInt32(reader.GetOrdinal("userid"));
                doc.DoctorId = reader.GetInt32(reader.GetOrdinal("doctorid"));
                doc.Rating = reader.GetInt32(reader.GetOrdinal("rating"));
                doc.FirstName = reader["firstname"].ToString();
                doc.LastName = reader["lastname"].ToString();
                doc.Email = reader["email"].ToString();
                doc.PersonalNumber = reader["personalnumber"].ToString();
                doc.Specialty = reader["specialty"].ToString();
                doc.PhotoUrl = reader["photourl"].ToString();

                docs.Add(doc);
            }
            conn.Close();
            return docs;


        }

        public Doctor GetDoctorById(int id)
        {
            Doctor doctor = null;
     

            using (OracleConnection conn = new OracleConnection(ConnStr))
            {
                try
                {
                    conn.Open();

                    using (OracleCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "olerning.PKG_LSH_DOCTORS.get_doctor_by_id";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                        cmd.Parameters.Add("p_result", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                doctor = new Doctor
                                {
                                    DoctorId = reader.GetInt32(reader.GetOrdinal("doctorid")),
                                    Rating = reader.GetInt32(reader.GetOrdinal("rating")),
                                    Email = reader["email"].ToString(),
                                    UserId = reader.GetInt32(reader.GetOrdinal("userid")),
                                    PersonalNumber = reader["personalnumber"].ToString(),
                                    FirstName = reader["firstname"].ToString(),
                                    LastName = reader["lastname"].ToString(),
                                    Specialty = reader["specialty"].ToString(),
                                    PhotoUrl = reader["photourl"].ToString()
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }

            return doctor;
        }

        public bool DeleteDoctorById(int id)
        {
            using (OracleConnection conn = new OracleConnection(ConnStr))
            {
                try
                {
                    conn.Open();
                    using (OracleCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "olerning.PKG_LSH_DOCTORS.delete_doctor_by_id";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                       

                        cmd.ExecuteNonQuery();
                        return true;


                    }
                }
                catch (OracleException ex)
                {
                    _logger.LogError(ex, "Database error deleting Doctor with {Id}", id);
                    throw new Exception($"Database error deleting Doctor: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database error deleting Doctor with {Id}", id);
                    throw new Exception($"Error deleting doctor: {ex.Message}", ex);
                }
            }
        }
        public async Task<(bool success, string message)> UpdateDoctor(int doctorId, UpdateDoctorDto doctor, IFormFile? photo = null, IFormFile? cv = null)
        {
            byte[]? photoData = null;
            byte[]? cvData = null;

            try
            {
                // Check if any update is requested
                if (doctor == null && photo == null && cv == null)
                {
                    return (false, "No updates provided");
                }

                // Process photo if provided
                if (photo != null)
                {
                    if (!ValidateFile(photo, MAX_PHOTO_SIZE, ALLOWED_PHOTO_TYPES, out string photoError))
                    {
                        return (false, photoError);
                    }
                    using var photoStream = new MemoryStream();
                    await photo.CopyToAsync(photoStream);
                    photoData = photoStream.ToArray();
                }

                // Process CV if provided
                if (cv != null)
                {
                    if (!ValidateFile(cv, MAX_CV_SIZE, ALLOWED_CV_TYPES, out string cvError))
                    {
                        return (false, cvError);
                    }
                    using var cvStream = new MemoryStream();
                    await cv.CopyToAsync(cvStream);
                    cvData = cvStream.ToArray();
                }

                using var conn = new OracleConnection(ConnStr);
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "olerning.PKG_LSH_DOCTORS.update_doctor";
                cmd.CommandType = CommandType.StoredProcedure;

                // Add parameters
                cmd.Parameters.Add("p_doctor_id", OracleDbType.Int32).Value = doctorId;

                // Only add non-null parameters
                cmd.Parameters.Add("p_first_name", OracleDbType.Varchar2).Value =
                    !string.IsNullOrWhiteSpace(doctor?.FirstName) ? doctor.FirstName : DBNull.Value;

                cmd.Parameters.Add("p_last_name", OracleDbType.Varchar2).Value =
                    !string.IsNullOrWhiteSpace(doctor?.LastName) ? doctor.LastName : DBNull.Value;

                cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value =
                    !string.IsNullOrWhiteSpace(doctor?.Email) ? doctor.Email : DBNull.Value;

                cmd.Parameters.Add("p_specialty", OracleDbType.Varchar2).Value =
                    !string.IsNullOrWhiteSpace(doctor?.Specialty) ? doctor.Specialty : DBNull.Value;

                cmd.Parameters.Add("p_personal_number", OracleDbType.Varchar2).Value =
                    !string.IsNullOrWhiteSpace(doctor?.PersonalNumber) ? doctor.PersonalNumber : DBNull.Value;

                cmd.Parameters.Add("p_photo_data", OracleDbType.Blob).Value =
                    photoData != null ? (object)photoData : DBNull.Value;

                cmd.Parameters.Add("p_cv_data", OracleDbType.Blob).Value =
                    cvData != null ? (object)cvData : DBNull.Value;

                var successParam = cmd.Parameters.Add("p_success", OracleDbType.Int32);
                successParam.Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync();

                int success = Convert.ToInt32(successParam.Value.ToString());

              

                return success == 1
                    ? (true, "Doctor updated successfully")
                    : (false, "Failed to update doctor. Email might already be in use or no valid updates were provided.");
            }
            catch (OracleException ex)
            {
                _logger.LogError(ex, "Database error updating doctor with ID {DoctorId}: {Message}",
                    doctorId, ex.Message);
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor with ID {DoctorId}: {Message}",
                    doctorId, ex.Message);
                return (false, "An unexpected error occurred while updating the doctor");
            }
        }

    }
}
