using AuthProjWebApi.Packages;
using HospitalAPI.Models;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace HospitalAPI.Packages
{

    public interface IPKG_PATIENT
    {
        public void RegisterPatient(User patient);

    }


    public class PKG_PATIENT : PKG_BASE,IPKG_PATIENT
    {
        private readonly IPKG_USERS _userPackage;
        private readonly ILogger<PKG_PATIENT> _logger;

        public PKG_PATIENT(IPKG_USERS userPackage, ILogger<PKG_PATIENT> logger)
        {
            _userPackage = userPackage;
            _logger = logger;
        }
        public void RegisterPatient(User patient)
        {
            if (_userPackage.CheckUserEmailExists(patient.Email))
            {
                _logger.LogWarning("Attempted to register patient with existing email: {Email}", patient.Email);
                throw new Exception($"A user with email {patient.Email} already exists.");
            }

            string connstr = ConnStr;
            OracleConnection conn = new OracleConnection();
            conn.ConnectionString = connstr;
            conn.Open();

            OracleCommand cmd = conn.CreateCommand();
            cmd.Connection = conn;
            cmd.CommandText = "olerning.PKG_LSH_PATIENTS.register_patient";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value = patient.Email;
            cmd.Parameters.Add("p_password", OracleDbType.Varchar2).Value = patient.Password;
            cmd.Parameters.Add("p_first_name", OracleDbType.Varchar2).Value = patient.FirstName;
            cmd.Parameters.Add("p_last_name", OracleDbType.Varchar2).Value = patient.LastName;
            cmd.Parameters.Add("p_personal_number", OracleDbType.Varchar2).Value = patient.PersonalNumber;
            cmd.Parameters.Add("p_user_id", OracleDbType.Int32).Direction = ParameterDirection.Output;

            cmd.ExecuteNonQuery();
            conn.Close();
        }
    }
}
