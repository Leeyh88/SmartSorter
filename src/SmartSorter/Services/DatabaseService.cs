using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using SmartSorter.Models;

namespace SmartSorter.Services
{
    public class DatabaseService
    {
        // Connect Timeout=3 을 추가하여 DB 접속 실패 시 PC가 멈추지 않도록 방지
        private readonly string _connectionString =
            @"Server=localhost\SQLEXPRESS;Database=SmartSorterDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;";
        // 1. 분류 이력 DB 저장
        public bool SaveSortingLog(SortingLog log)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO SortingHistory (Timestamp, Mode, DetectedColor, TargetServoAngle, Message) 
                                     VALUES (@Timestamp, @Mode, @DetectedColor, @TargetServoAngle, @Message)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Timestamp", log.Timestamp);
                        cmd.Parameters.AddWithValue("@Mode", log.Mode.ToString());
                        cmd.Parameters.AddWithValue("@DetectedColor", log.DetectedColor.ToString());
                        cmd.Parameters.AddWithValue("@TargetServoAngle", log.TargetServoAngle);
                        cmd.Parameters.AddWithValue("@Message", log.Message);

                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB 저장 에러: {ex.Message}");
                return false;
            }
        }

        // 2. 전체 이력 조회 (이력 페이지용)
        public List<SortingLog> GetSortingLogs(int limit = 200)
        {
            var list = new List<SortingLog>();
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = $@"SELECT TOP ({limit}) Timestamp, Mode, DetectedColor, TargetServoAngle, Message 
                                     FROM SortingHistory ORDER BY Timestamp DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Enum.TryParse(reader["Mode"].ToString(), out OperationMode mode);
                            Enum.TryParse(reader["DetectedColor"].ToString(), out BoxColor color);

                            list.Add(new SortingLog
                            {
                                Timestamp = Convert.ToDateTime(reader["Timestamp"]),
                                Mode = mode,
                                DetectedColor = color,
                                TargetServoAngle = Convert.ToInt32(reader["TargetServoAngle"]),
                                Message = reader["Message"].ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB 조회 에러: {ex.Message}");
            }
            return list;
        }

        // 3. 전체 설정값 읽어오기 (Dictionary 형태)
        public Dictionary<string, string> GetSettings()
        {
            var settings = new Dictionary<string, string>();
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT SettingKey, SettingValue FROM SystemSettings";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string key = reader["SettingKey"].ToString() ?? "";
                            string val = reader["SettingValue"].ToString() ?? "";
                            settings[key] = val;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 설정 읽기 오류: {ex.Message}");
            }
            return settings;
        }

        // 4. 단일 설정값 저장/업데이트 (UPSERT)
        public bool SaveSetting(string key, string value)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                IF EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = @Key)
                    UPDATE SystemSettings SET SettingValue = @Value, UpdatedTime = GETDATE() WHERE SettingKey = @Key;
                else
                    INSERT INTO SystemSettings (SettingKey, SettingValue) VALUES (@Key, @Value);";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Key", key);
                        cmd.Parameters.AddWithValue("@Value", value ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 설정 저장 오류: {ex.Message}");
                return false;
            }
        }

        // 조건별 이력 필터링 조회 (날짜 범위 + 색상)
        public List<SortingLog> GetFilteredSortingLogs(DateTime? startDate, DateTime? endDate, string colorFilter, string modeFilter)
        {
            var list = new List<SortingLog>();
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT LogID, Timestamp, Mode, DetectedColor, TargetServoAngle, Message 
                             FROM SortingHistory 
                             WHERE 1=1";

                    // 1. 날짜 조건
                    if (startDate.HasValue)
                        query += " AND Timestamp >= @StartDate";
                    if (endDate.HasValue)
                        query += " AND Timestamp <= @EndDate";

                    // 2. 색상 조건
                    if (!string.IsNullOrEmpty(colorFilter) && colorFilter != "전체")
                        query += " AND DetectedColor = @ColorFilter";

                    // 3. ➕ 운전 모드 조건 (Auto / Manual)
                    if (!string.IsNullOrEmpty(modeFilter) && modeFilter != "전체")
                        query += " AND Mode = @ModeFilter";

                    query += " ORDER BY Timestamp DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        if (startDate.HasValue)
                            cmd.Parameters.AddWithValue("@StartDate", startDate.Value.Date);
                        if (endDate.HasValue)
                            // 선택일의 23:59:59까지 포함
                            cmd.Parameters.AddWithValue("@EndDate", endDate.Value.Date.AddDays(1).AddTicks(-1));
                        if (!string.IsNullOrEmpty(colorFilter) && colorFilter != "전체")
                            cmd.Parameters.AddWithValue("@ColorFilter", colorFilter);
                        if (!string.IsNullOrEmpty(modeFilter) && modeFilter != "전체")
                            cmd.Parameters.AddWithValue("@ModeFilter", modeFilter);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Enum.TryParse(reader["Mode"].ToString(), out OperationMode mode);
                                Enum.TryParse(reader["DetectedColor"].ToString(), out BoxColor color);

                                list.Add(new SortingLog
                                {
                                    Timestamp = Convert.ToDateTime(reader["Timestamp"]),
                                    Mode = mode,
                                    DetectedColor = color,
                                    TargetServoAngle = Convert.ToInt32(reader["TargetServoAngle"]),
                                    Message = reader["Message"].ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 조건 조회 오류: {ex.Message}");
            }
            return list;
        }
        /// <summary>
        /// DB에서 PLC 통신 설정 읽기 (READ ONLY - DB 값 변경 절대 안 됨)
        /// </summary>
        public PlcConfigModel GetPlcConfig()
        {
            var config = new PlcConfigModel();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // DB 설정값이 수정되지 않도록 SELECT 쿼리만 실행합니다.
                    string query = @"
                        SELECT 
                            ISNULL((SELECT SettingValue FROM SystemSettings WHERE SettingKey = 'PLC_PortName'), 'COM3') AS PortName,
                            ISNULL((SELECT SettingValue FROM SystemSettings WHERE SettingKey = 'PLC_BaudRate'), '115200') AS BaudRate,
                            ISNULL((SELECT SettingValue FROM SystemSettings WHERE SettingKey = 'PLC_StationId'), '1') AS StationId;";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            config.PortName = reader["PortName"].ToString();

                            if (int.TryParse(reader["BaudRate"].ToString(), out int baudRate))
                                config.BaudRate = baudRate;

                            if (byte.TryParse(reader["StationId"].ToString(), out byte StationId))
                                config.StationId = StationId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB ERROR] PLC 설정 읽기 실패: {ex.Message}");
            }

            return config;
        }
    }
}