using AuthProjWebApi.Packages;
using HospitalAPI.DTO_s;
using HospitalAPI.Models;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Security.Claims;

namespace HospitalAPI.Packages
{
    public interface IPKG_APPONTMENT
    {
        public List<Appointment> GetDoctorAppointments(int DoctorId);
        public List<Appointment> GetPatientAppointments(int PatientId);
        public int CreateAppointment(CreateAppointmentDTO dto);
        //List<DoctorAvailabilityDto> GetDoctorAvailability(int DoctorId);
    }
    public class PKG_APPOINTMENT : PKG_BASE, IPKG_APPONTMENT
    {
        private readonly ILogger<PKG_APPOINTMENT> logger;

        public PKG_APPOINTMENT(ILogger<PKG_APPOINTMENT> logger)
        {
            this.logger = logger;
        }
        public List <Appointment> GetDoctorAppointments(int doctorId)
        {
            var appointments = new List<Appointment>();
            using (var conn = new OracleConnection(ConnStr))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.get_doctor_appointments";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_doctor_id", OracleDbType.Int32).Value = doctorId;
                    cmd.Parameters.Add("p_result",OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    using(var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            appointments.Add(new Appointment
                            {
                                AppointmentId = reader.GetInt32(reader.GetOrdinal("id")),
                                DoctorId = doctorId,
                                PatientName = $"{reader["patient_first_name"]} {reader["patient_last_name"]}",
                                AppointmentDateTime = reader.GetDateTime(reader.GetOrdinal("appointment_date")),
                                TimeSlot = reader["time_slot"].ToString(),
                                Description = reader["description"]?.ToString(),
                                UserId = reader.GetInt32(reader.GetOrdinal("patient_id"))

                            });
                        }
                    }
                }
            }
            return appointments;
        }
        public List<Appointment> GetPatientAppointments(int patientId)
        {
            var appointments = new List<Appointment>();

            using (var conn = new OracleConnection(ConnStr))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.get_patient_appointments";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_patient_id", OracleDbType.Int32).Value = patientId;
                    cmd.Parameters.Add("p_result", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            appointments.Add(new Appointment
                            {
                                AppointmentId = reader.GetInt32(reader.GetOrdinal("id")),
                                DoctorId = reader.GetInt32(reader.GetOrdinal("doctor_id")),
                                DoctorName = $"{reader["doctor_first_name"]} {reader["doctor_last_name"]}",
                                AppointmentDateTime = reader.GetDateTime(reader.GetOrdinal("appointment_date")),
                                TimeSlot = reader["time_slot"].ToString(),
                                Description = reader["description"]?.ToString(),
                                UserId = patientId
                            });
                        }
                    }
                }
            }
            return appointments;
        }

        public int CreateAppointment(CreateAppointmentDTO appointment)
        {
            try
            {
                using (var conn = new OracleConnection(ConnStr))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.create_appointment";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("p_doctor_id", OracleDbType.Int32).Value = appointment.DoctorId;
                        cmd.Parameters.Add("p_patient_id", OracleDbType.Int32).Value = appointment.PatientId;
                        cmd.Parameters.Add("p_date", OracleDbType.Date).Value = appointment.AppointmentDate.Date;
                        cmd.Parameters.Add("p_time_slot", OracleDbType.Varchar2).Value = appointment.TimeSlot;
                        cmd.Parameters.Add("p_description", OracleDbType.Varchar2).Value =
                            appointment.Description ?? (object)DBNull.Value;

                        var outputParam = cmd.Parameters.Add("p_appointment_id", OracleDbType.Int32);
                        outputParam.Direction = ParameterDirection.Output;

                        cmd.ExecuteNonQuery();

                        if (outputParam.Value == DBNull.Value)
                        {
                            throw new Exception("Failed to get appointment ID from database");
                        }

                        return ((OracleDecimal)outputParam.Value).ToInt32();
                    }
                }
            }
            catch (OracleException ex)
            {
                logger.LogError(ex, "Database error creating appointment for Doctor {DoctorId} and Patient {PatientId}",
                    appointment.DoctorId, appointment.PatientId);
                throw new Exception("Failed to create appointment: " + ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating appointment");
                throw;
            }
        }
    }
}
