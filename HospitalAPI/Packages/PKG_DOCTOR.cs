using AuthProjWebApi.Packages;
using HospitalAPI.Models;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace HospitalAPI.Packages
{
    public interface IPKG_DOCTOR
    {
        public void RegisterDoctor(Doctor doctor);
        public List<Doctor> GetDoctorCards();
        public Doctor GetDoctorById(int id);
        public bool DeleteDoctorById(int id);
        public byte[] GetDoctorPhoto(int id);
        public byte[] GetDoctorCV(int id);
    }

    public class PKG_DOCTOR:PKG_BASE,IPKG_DOCTOR
    {
        private IPKG_USERS _userPackage;
        private ILogger<PKG_PATIENT> _logger;

        public PKG_DOCTOR(IPKG_USERS userPackage, ILogger<PKG_PATIENT> logger)
        {
            _userPackage = userPackage;
            _logger = logger;
        }

        public async void RegisterDoctor(Doctor doctor)
        {
            if (_userPackage.CheckUserEmailExists(doctor.Email))
            {
                _logger.LogWarning("Attempted to register doctor with existing email: {Email}", doctor.Email);
                throw new Exception($"A user with email {doctor.Email} already exists.");
            }

            using var photoStream = new MemoryStream();
            using var cvStream = new MemoryStream();

            await doctor.Photo.CopyToAsync(photoStream);
            await doctor.CV.CopyToAsync(cvStream);

            var photoData = photoStream.ToArray();
            var cvData = cvStream.ToArray();

            string connstr = ConnStr;
            OracleConnection conn = new OracleConnection();
            conn.ConnectionString = connstr;
            conn.Open();


            OracleCommand cmd = conn.CreateCommand();
            cmd.Connection = conn;
            cmd.CommandText = "olerning.PKG_LSH_DOCTORS.register_doctor";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value = doctor.Email;
            cmd.Parameters.Add("p_password", OracleDbType.Varchar2).Value = doctor.Password;
            cmd.Parameters.Add("p_first_name", OracleDbType.Varchar2).Value = doctor.FirstName;
            cmd.Parameters.Add("p_last_name", OracleDbType.Varchar2).Value = doctor.LastName;
            cmd.Parameters.Add("p_specialty", OracleDbType.Varchar2).Value = doctor.Specialty;
            cmd.Parameters.Add("p_photo_url", OracleDbType.Varchar2).Value = doctor.PhotoUrl;
            cmd.Parameters.Add("p_cv_url", OracleDbType.Varchar2).Value = doctor.CvUrl;
            cmd.Parameters.Add("p_photo_data", OracleDbType.Blob).Value = photoData;
            cmd.Parameters.Add("p_cv_data", OracleDbType.Blob).Value = cvData;
            cmd.Parameters.Add("p_personal_number", OracleDbType.Varchar2).Value = doctor.PersonalNumber;
            cmd.Parameters.Add("p_user_id", OracleDbType.Int32).Direction = ParameterDirection.Output;

            cmd.ExecuteNonQuery();
            conn.Close();
        }
        public byte[] GetDoctorPhoto(int id)
        {
            string connstr = ConnStr;
            OracleConnection conn = new OracleConnection();
            conn.ConnectionString = connstr;
            conn.Open();


            OracleCommand cmd = conn.CreateCommand();
            cmd.Connection = conn;
            cmd.CommandText = "olerning.PKG_LSH_DOCTORS.register_doctor";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
            cmd.Parameters.Add("p_photo_data",OracleDbType.Blob).Direction = ParameterDirection.Output;
            cmd.ExecuteNonQuery();
            return cmd.Parameters["p_photo_data"].Value as byte[];

        }
        public byte[] GetDoctorCV(int id)
        {
            string connstr = ConnStr;
            OracleConnection conn = new OracleConnection();
            conn.ConnectionString = connstr;
            conn.Open();

            OracleCommand cmd = conn.CreateCommand();
            cmd.Connection = conn;
            cmd.CommandText = "olerning.PKG_LSH_DOCTORS.get_doctor_cv";
            cmd.CommandType= CommandType.StoredProcedure;

            cmd.Parameters.Add("p_id", OracleDbType.Int32 ).Value = id;
            cmd.Parameters.Add("p_cv_data",OracleDbType.Blob ).Direction = ParameterDirection.Output;
            cmd.ExecuteNonQuery();
            return cmd.Parameters["p_cv_data"].Value as byte[];
        }
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

                        // Add input and output parameters
                        cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                        cmd.Parameters.Add("p_result", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        // Execute and read data
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
    }
}
