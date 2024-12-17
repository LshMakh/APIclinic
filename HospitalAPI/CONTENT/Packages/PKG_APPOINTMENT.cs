using AuthProjWebApi.Packages;
using HospitalAPI.CONTENT.DTO_s;
using HospitalAPI.Models;
using Mailjet.Client.Resources;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace HospitalAPI.CONTENT.Packages
{
    public interface IPKG_APPOINTMENT
    {
        Task<(bool success, int appointmentId)> CreateAppointment(int doctorId, int patientId, DateTime date, string timeSlot, string description);
        Task<bool> BlockTimeSlot(int doctorId, DateTime date, string timeSlot);
        Task<List<Appointment>> GetDoctorAppointments(int doctorId);
        Task<List<Appointment>> GetPatientAppointments(int patientId);
        Task<bool> UpdateAppointmentDescription(int appointmentId, string description);
        Task<bool> DeleteAppointment(int appointmentId);
        Task<bool> CheckSlotAvailability(int doctorId, DateTime date, string timeSlot);
        Task<List<TimeSlotDto>> GetAvailableSlots(int doctorId, DateTime date);
        Task<int> GetUserAppointmentCount(int userId);
        Task<int> GetDoctorAppointmentCount(int doctorId);
        Task<bool> CheckPatientTimeSlotAvailability(int patientId, DateTime date, string timeSlot);

    }
    public class PKG_APPOINTMENT : PKG_BASE, IPKG_APPOINTMENT
    {
        private readonly ILogger<PKG_APPOINTMENT> _logger;

        public PKG_APPOINTMENT(ILogger<PKG_APPOINTMENT> logger)
        {
            _logger = logger;
        }

        public async Task<int> GetDoctorAppointmentCount(int doctorId)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.get_doctor_appointment_count";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_doctor_id", OracleDbType.Int32).Value = doctorId;
                    var p_count = new OracleParameter
                    {
                        ParameterName = "p_count",
                        OracleDbType = OracleDbType.Int32,
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(p_count);

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                        var oracleDemical = (OracleDecimal)p_count.Value;
                        return (int)oracleDemical.Value;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting appointment count for user {UserId}", doctorId);
                        return 0;
                    }
                }
            }
        }
        public async Task<int> GetUserAppointmentCount(int userId)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.get_user_appointment_count";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_user_id", OracleDbType.Int32).Value = userId;

                    var p_count = new OracleParameter
                    {
                        ParameterName = "p_count",
                        OracleDbType = OracleDbType.Int32,
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(p_count);

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                        var oracleDemical = (OracleDecimal)p_count.Value;
                        return (int)oracleDemical.Value;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting appointment count for user {UserId}", userId);
                        return 0;
                    }
                }
            }
        }

        public async Task<(bool success, int appointmentId)> CreateAppointment(int doctorId, int patientId, DateTime date,
        string timeSlot, string description)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.create_appointment";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_doctor_id", OracleDbType.Int32).Value = doctorId;
                    cmd.Parameters.Add("p_patient_id", OracleDbType.Int32).Value = patientId;
                    cmd.Parameters.Add("p_appointment_date", OracleDbType.Date).Value = date.Date;
                    cmd.Parameters.Add("p_time_slot", OracleDbType.Varchar2).Value = timeSlot;
                    cmd.Parameters.Add("p_description", OracleDbType.Varchar2).Value = description ?? "";

                    var p_appointment_id = new OracleParameter("p_appointment_id", OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(p_appointment_id);

                    var p_success = new OracleParameter("p_success", OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(p_success);

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                        return (
                            Convert.ToInt32(p_success.Value.ToString()) == 1,
                            Convert.ToInt32(p_appointment_id.Value.ToString())
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating appointment for doctor {DoctorId}", doctorId);
                        return (false, 0);
                    }
                }
            }
        }

        public async Task<bool> BlockTimeSlot(int doctorId, DateTime date, string timeSlot)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.block_time_slot";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_doctor_id", OracleDbType.Int32).Value = doctorId;
                    cmd.Parameters.Add("p_appointment_date", OracleDbType.Date).Value = date.Date;
                    cmd.Parameters.Add("p_time_slot", OracleDbType.Varchar2).Value = timeSlot;

                    var p_success = new OracleParameter("p_success", OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(p_success);

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                        return Convert.ToInt32(p_success.Value.ToString()) == 1;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error blocking time slot for doctor {DoctorId}", doctorId);
                        return false;
                    }
                }
            }
        }

        public async Task<List<Appointment>> GetDoctorAppointments(int doctorId)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.get_doctor_appointments";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_doctor_id", OracleDbType.Int32).Value = doctorId;

                    cmd.Parameters.Add("p_result", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    try
                    {
                        var appointments = new List<Appointment>();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                appointments.Add(new Appointment
                                {
                                    AppointmentId = reader.IsDBNull(reader.GetOrdinal("appointment_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("appointment_id")),
                                    DoctorId = reader.IsDBNull(reader.GetOrdinal("doctor_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("doctor_id")),
                                    PatientId = reader.IsDBNull(reader.GetOrdinal("patient_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("patient_id")),
                                    AppointmentDate = reader.GetDateTime(reader.GetOrdinal("appointment_date")),
                                    TimeSlot = reader.GetString(reader.GetOrdinal("time_slot")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                    IsBlocked = Convert.ToBoolean(reader.IsDBNull(reader.GetOrdinal("is_blocked")) ? 0 : reader.GetInt32(reader.GetOrdinal("is_blocked"))),
                                    PatientFirstName = reader.IsDBNull(reader.GetOrdinal("patient_firstname")) ? null : reader.GetString(reader.GetOrdinal("patient_firstname")),
                                    PatientLastName = reader.IsDBNull(reader.GetOrdinal("patient_lastname")) ? null : reader.GetString(reader.GetOrdinal("patient_lastname"))
                                });

                            }
                        }
                        return appointments;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting appointments for doctor {DoctorId}", doctorId);
                        throw;
                    }
                }
            }
        }

        public async Task<List<Appointment>> GetPatientAppointments(int patientId)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.get_patient_appointments";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_patient_id", OracleDbType.Int32).Value = patientId;

                    cmd.Parameters.Add("p_result", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    try
                    {
                        var appointments = new List<Appointment>();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                appointments.Add(new Appointment
                                {
                                    AppointmentId = reader.GetInt32(reader.GetOrdinal("appointment_id")),
                                    DoctorId = reader.GetInt32(reader.GetOrdinal("doctor_id")),
                                    PatientId = reader.GetInt32(reader.GetOrdinal("patient_id")),
                                    AppointmentDate = reader.GetDateTime(reader.GetOrdinal("appointment_date")),
                                    TimeSlot = reader.GetString(reader.GetOrdinal("time_slot")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null :
                                        reader.GetString(reader.GetOrdinal("description")),
                                    DoctorFirstName = reader.GetString(reader.GetOrdinal("doctor_firstname")),
                                    DoctorLastName = reader.GetString(reader.GetOrdinal("doctor_lastname")),
                                    DoctorSpecialty = reader.GetString(reader.GetOrdinal("doctor_specialty"))
                                });
                            }
                        }
                        return appointments;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting appointments for patient {PatientId}", patientId);
                        throw;
                    }
                }
            }
        }

        public async Task<bool> UpdateAppointmentDescription(int appointmentId, string description)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.update_appointment_description";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_appointment_id", OracleDbType.Int32).Value = appointmentId;
                    cmd.Parameters.Add("p_description", OracleDbType.Varchar2).Value = description;

                    var p_success = new OracleParameter("p_success", OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(p_success);

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                        return Convert.ToInt32(p_success.Value.ToString()) == 1;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating appointment {AppointmentId}", appointmentId);
                        return false;
                    }
                }
            }
        }

        public async Task<bool> DeleteAppointment(int appointmentId)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.delete_appointment";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_appointment_id", OracleDbType.Int32).Value = appointmentId;


                    var p_success = new OracleParameter("p_success", OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(p_success);

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                        return Convert.ToInt32(p_success.Value.ToString()) == 1;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting appointment {AppointmentId}", appointmentId);
                        return false;
                    }
                }
            }
        }

        public async Task<bool> CheckSlotAvailability(int doctorId, DateTime date, string timeSlot)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.check_slot_availability";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_doctor_id", OracleDbType.Int32).Value = doctorId;
                    cmd.Parameters.Add("p_appointment_date", OracleDbType.Date).Value = date.Date;
                    cmd.Parameters.Add("p_time_slot", OracleDbType.Varchar2).Value = timeSlot;

                    var p_is_available = new OracleParameter("p_is_available", OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(p_is_available);

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                        return Convert.ToInt32(p_is_available.Value.ToString()) == 1;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking slot availability for doctor {DoctorId}", doctorId);
                        return false;
                    }
                }
            }
        }

        public async Task<List<TimeSlotDto>> GetAvailableSlots(int doctorId, DateTime date)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.get_available_slots";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_doctor_id", OracleDbType.Int32).Value = doctorId;
                    cmd.Parameters.Add("p_date", OracleDbType.Date).Value = date.Date;
                    cmd.Parameters.Add("p_result", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    try
                    {
                        var slots = new List<TimeSlotDto>();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var slot = new TimeSlotDto
                                {
                                    TimeSlot = reader.GetString(reader.GetOrdinal("time_slot")),
                                    IsAvailable = !reader.IsDBNull(reader.GetOrdinal("is_available"))
                                        ? reader.GetInt32(reader.GetOrdinal("is_available")) == 1
                                        : false,
                                    IsBlocked = !reader.IsDBNull(reader.GetOrdinal("is_blocked"))
                                        ? reader.GetInt32(reader.GetOrdinal("is_blocked")) == 1
                                        : false,
                                    PatientId = !reader.IsDBNull(reader.GetOrdinal("patient_id"))
                                        ? reader.GetInt32(reader.GetOrdinal("patient_id"))
                                        : null
                                };
                                slots.Add(slot);
                            }
                        }
                        return slots;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting available slots for doctor {DoctorId}. Details: {Message}",
                            doctorId, ex.Message);
                        throw;
                    }
                }
            }
        }
        public async Task<bool> CheckPatientTimeSlotAvailability(int patientId, DateTime date, string timeSlot)
        {
            using (var conn = new OracleConnection(ConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "olerning.PKG_LSH_APPOINTMENTS.check_patient_time_slot";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_patient_id", OracleDbType.Int32).Value = patientId;
                    cmd.Parameters.Add("p_appointment_date", OracleDbType.Date).Value = date.Date;
                    cmd.Parameters.Add("p_time_slot", OracleDbType.Varchar2).Value = timeSlot;

                    var p_is_available = new OracleParameter
                    {
                        ParameterName = "p_is_available",
                        OracleDbType = OracleDbType.Int32,
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(p_is_available);

                    await cmd.ExecuteNonQueryAsync();
                    return Convert.ToInt32(p_is_available.Value.ToString()) == 1;
                }
            }
        }

    }
}
