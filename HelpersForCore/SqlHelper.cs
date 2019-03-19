using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace HelpersForCore
{
    public class SqlHelper
    {
        /// <summary>
        /// 連接字串
        /// </summary>
        readonly string connectionString;

        /// <summary>
        /// 逾時時間
        /// </summary>
        readonly int commandTimeout;

        public SqlHelper(string connectionString, int commandTimeout = 30)
        {
            this.connectionString = connectionString;
            this.commandTimeout = commandTimeout;
        }

        /// <summary>
        /// 建立連接物件
        /// (需自行用 using 來控制連接的開關)
        /// </summary>
        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(connectionString);
            }
        }

        /// <summary>
        /// 將 SqlParameter 轉換成 Declare 語法顯示在輸出視窗
        /// </summary>
        private void DebugWrite(SqlParameter parameter)
        {
            string name = parameter.ParameterName;
            if (name.StartsWith("@"))
            {
                name = name.Substring(1);
            }
            object value = parameter.Value;

            Type type = value.GetType();
            if (type == typeof(bool))
                System.Diagnostics.Debug.WriteLine($"DECLARE @{name} bit = {(Convert.ToBoolean(value) ? 1 : 0)}");
            else if (type == typeof(int))
                System.Diagnostics.Debug.WriteLine($"DECLARE @{name} int = {value}");
            else if (type == typeof(float) || type == typeof(double))
                System.Diagnostics.Debug.WriteLine($"DECLARE @{name} real = {value}");
            else if (type == typeof(string))
            {
                int length = Convert.ToString(value).Length;
                if (value == null)
                    System.Diagnostics.Debug.WriteLine($"DECLARE @{name} nvarchar = null");
                else if (length <= 4000)
                    System.Diagnostics.Debug.WriteLine($"DECLARE @{name} nvarchar({length}) = '{value}'");
                else
                    System.Diagnostics.Debug.WriteLine($"DECLARE @{name} nvarchar(max) = '{value}'");
            }
            else if (type == typeof(DateTime))
                System.Diagnostics.Debug.WriteLine($"DECLARE @{name} datetime = '{Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss")}'");
            else if (value == null)
                System.Diagnostics.Debug.WriteLine($"DECLARE @{name} nvarchar = null");
            else
                System.Diagnostics.Debug.WriteLine($"DECLARE @{name} nvarchar(max) = '{value}'");
        }

        /// <summary>
        /// 將 SqlCommand 的參數宣告與 SQL 語法顯示在輸出視窗
        /// </summary>
        public void DebugWrite(SqlCommand cmd)
        {
            System.Diagnostics.Debug.WriteLine($@"
                ----Connection String-----------------------------
                -- {connectionString.Replace("\r\n", "\n").Replace("\n", " ")}
                ----Parameters------------------------------------".RemoveHeadEmptyLines().DecreaseIndent());
            foreach (SqlParameter parameter in cmd.Parameters)
            {
                DebugWrite(parameter);
            }
            System.Diagnostics.Debug.WriteLine($@"
                ----Command Text----------------------------------".RemoveHeadEmptyLines().DecreaseIndent());
            System.Diagnostics.Debug.WriteLine(cmd.CommandText.RemoveHeadEmptyLines().DecreaseIndent());
            System.Diagnostics.Debug.WriteLine($@"
                ----End-------------------------------------------".RemoveHeadEmptyLines().DecreaseIndent());
        }

        /// <summary>
        /// 執行 SQL 語法並傳回受影響的資料筆數
        /// </summary>
        public int ExecuteNonQuery(string commandText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandTimeout = commandTimeout;
                cmd.AddParameters(parameters);
                DebugWrite(cmd);
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並傳回第一筆結果第一個欄位的值
        /// </summary>
        public object ExecuteScalar(string commandText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandTimeout = commandTimeout;
                cmd.AddParameters(parameters);
                DebugWrite(cmd);
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並將第一筆結果以 DataRow 類型傳回
        /// </summary>
        public DataRow ExecuteFirstRow(string commandText, params SqlParameter[] parameters)
        {
            DataTable dt = ExecuteDataTable(commandText, parameters);
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0];
            }
            return null;
        }

        /// <summary>
        /// 執行 SQL 語法並將所有結果的第一個欄位以集合傳回
        /// </summary>
        public IEnumerable<object> ExecuteFirstColumn(string commandText, params SqlParameter[] parameters)
        {
            DataTable dt = ExecuteDataTable(commandText, parameters);
            if (dt.Columns.Count > 0)
            {
                return dt.Rows.Cast<DataRow>().Select(dr => dr.ItemArray[0]);
            }
            return null;
        }

        /// <summary>
        /// 執行 SQL 語法並將完整的結果以 DataTable 類型傳回
        /// </summary>
        public DataTable ExecuteDataTable(string commandText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandTimeout = commandTimeout;
                cmd.AddParameters(parameters);
                DebugWrite(cmd);
                return cmd.ExecuteDataTable();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並將完整的結果以 DataSet 類型傳回
        /// </summary>
        public DataSet ExecuteDataSet(string commandText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandTimeout = commandTimeout;
                cmd.AddParameters(parameters);
                DebugWrite(cmd);
                return cmd.ExecuteDataSet();
            }
        }

        /// <summary>
        /// 執行 SQL 將結果以自訂的  Class 集合傳回
        /// </summary>
        public IEnumerable<T> ExecuteModels<T>(string commandText, params SqlParameter[] parameters)
        {
            return ExecuteDataTable(commandText, parameters).Rows.ToObjects<T>();
        }

        /// <summary>
        /// 執行 SQL 語法並以 SqlDataReader 處理每一筆資料
        /// </summary>
        public void ExecuteReaderEach(string commandText, IEnumerable<SqlParameter> parameters, Action<SqlDataReader> func)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandTimeout = commandTimeout;
                cmd.AddParameters(parameters?.ToArray());
                DebugWrite(cmd);
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        func(dr);
                    }
                }
            }
        }
        /// <summary>
        /// 執行 SQL 語法並以自訂 Class 處理每一筆資料
        /// </summary>
        public void ExecuteReaderEach<T>(string commandText, IEnumerable<SqlParameter> parameters, Action<T> func)
        {
            ExecuteReaderEach(commandText, parameters, dr =>
            {
                T model = dr.ToObject<T>();
                func(model);
            });
        }

        /// <summary>
        /// 執行 SQL 語法並傳回受影響的資料筆數
        /// </summary>
        public int ExecuteNonQuery(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並傳回第一筆結果第一個欄位的值
        /// </summary>
        public object ExecuteScalar(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並將第一筆結果以 DataRow 類型傳回
        /// </summary>
        public DataRow ExecuteFirstRow(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return cmd.ExecuteFirstRow();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並將所有結果的第一個欄位以集合傳回
        /// </summary>
        public IEnumerable<object> ExecuteFirstColumn(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return cmd.ExecuteFirstColumn();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並將完整的結果以 DataTable 類型傳回
        /// </summary>
        public DataTable ExecuteDataTable(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return cmd.ExecuteDataTable();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並將完整的結果以 DataSet 類型傳回
        /// </summary>
        public DataSet ExecuteDataSet(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return cmd.ExecuteDataSet();
            }
        }

        /// <summary>
        /// 執行 SQL 將結果以自訂的  Class 集合傳回
        /// </summary>
        public IEnumerable<T> ExecuteModels<T>(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return cmd.ExecuteDataTable().Rows.ToObjects<T>();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並以 SqlDataReader 處理每一筆資料
        /// </summary>
        public void ExecuteReaderEach(SqlCommand cmd, Action<SqlDataReader> func)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                cmd.Connection = conn;
                DebugWrite(cmd);
                cmd.ExecuteReaderEach(func);
            }
        }
        /// <summary>
        /// 執行 SQL 語法並以自訂 Class 處理每一筆資料
        /// </summary>
        public void ExecuteReaderEach<T>(SqlCommand cmd, Action<T> func)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                cmd.Connection = conn;
                DebugWrite(cmd);
                cmd.ExecuteReaderEach(func);
            }
        }

        /// <summary>
        /// 用交易式 Command 來執行 SQL 語法，全部成功才會更新，傳回是否更新成功與受影響資料筆數的總和
        /// </summary>
        public (bool IsCommit, int QueryCount) ExecuteSqlTransaction(params SqlCommand[] commands)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        int count = 0;
                        foreach (SqlCommand cmd in commands)
                        {
                            cmd.Connection = conn;
                            cmd.Transaction = tran;
                            DebugWrite(cmd);
                            count += cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                        return (true, count);
                    }
                    catch
                    {
                        tran.Rollback();
                        return (false, 0);
                    }
                }
            }
        }

        /// <summary>
        /// 執行 SQL 語法並傳回受影響的資料筆數
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string commandText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandTimeout = commandTimeout;
                cmd.AddParameters(parameters);
                DebugWrite(cmd);
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並傳回第一筆結果第一個欄位的值
        /// </summary>
        public async Task<object> ExecuteScalarAsync(string commandText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = Connection)
            {
                await conn.OpenAsync();
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandTimeout = commandTimeout;
                cmd.AddParameters(parameters);
                DebugWrite(cmd);
                return await cmd.ExecuteScalarAsync();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並將第一筆結果以 DataRow 類型傳回
        /// </summary>
        public async Task<DataRow> ExecuteFirstRowAsync(string commandText, params SqlParameter[] parameters)
        {
            DataTable dt = await ExecuteDataTableAsync(commandText, parameters);
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0];
            }
            return null;
        }

        /// <summary>
        /// 執行 SQL 語法並將所有結果的第一個欄位以集合傳回
        /// </summary>
        public async Task<IEnumerable<object>> ExecuteFirstColumnAsync(string commandText, params SqlParameter[] parameters)
        {
            DataTable dt = await ExecuteDataTableAsync(commandText, parameters);
            if (dt.Columns.Count > 0)
            {
                return dt.Rows.Cast<DataRow>().Select(dr => dr.ItemArray[0]);
            }
            return null;
        }

        /// <summary>
        /// 執行 SQL 語法並將完整的結果以 DataTable 類型傳回
        /// </summary>
        public async Task<DataTable> ExecuteDataTableAsync(string commandText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandTimeout = commandTimeout;
                cmd.AddParameters(parameters);
                DebugWrite(cmd);
                return await cmd.ExecuteDataTableAsync();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並將完整的結果以 DataSet 類型傳回
        /// </summary>
        public async Task<DataSet> ExecuteDataSetAsync(string commandText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandTimeout = commandTimeout;
                cmd.AddParameters(parameters);
                DebugWrite(cmd);
                return await cmd.ExecuteDataSetAsync();
            }
        }

        /// <summary>
        /// 執行 SQL 將結果以自訂的  Class 集合傳回
        /// </summary>
        public async Task<IEnumerable<T>> ExecuteModelsAsync<T>(string commandText, params SqlParameter[] parameters)
        {
            return (await ExecuteDataTableAsync(commandText, parameters)).Rows.ToObjects<T>();
        }

        /// <summary>
        /// 執行 SQL 語法並以 SqlDataReader 處理每一筆資料
        /// </summary>
        public async Task ExecuteReaderEachAsync(string commandText, IEnumerable<SqlParameter> parameters, Action<SqlDataReader> func)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandTimeout = commandTimeout;
                cmd.AddParameters(parameters?.ToArray());
                DebugWrite(cmd);
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        func(dr);
                    }
                }
            }
        }
        /// <summary>
        /// 執行 SQL 語法並以自訂 Class 處理每一筆資料
        /// </summary>
        public async Task ExecuteReaderEachAsync<T>(string commandText, IEnumerable<SqlParameter> parameters, Action<T> func)
        {
            await ExecuteReaderEachAsync(commandText, parameters, dr =>
            {
                T model = dr.ToObject<T>();
                func(model);
            });
        }

        /// <summary>
        /// 執行 SQL 語法並傳回受影響的資料筆數
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並傳回第一筆結果第一個欄位的值
        /// </summary>
        public async Task<object> ExecuteScalarAsync(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return await cmd.ExecuteScalarAsync();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並將第一筆結果以 DataRow 類型傳回
        /// </summary>
        public async Task<DataRow> ExecuteFirstRowAsync(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return await cmd.ExecuteFirstRowAsync();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並將所有結果的第一個欄位以集合傳回
        /// </summary>
        public async Task<IEnumerable<object>> ExecuteFirstColumnAsync(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return await cmd.ExecuteFirstColumnAsync();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並將完整的結果以 DataTable 類型傳回
        /// </summary>
        public async Task<DataTable> ExecuteDataTableAsync(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return await cmd.ExecuteDataTableAsync();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並將完整的結果以 DataSet 類型傳回
        /// </summary>
        public async Task<DataSet> ExecuteDataSetAsync(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return await cmd.ExecuteDataSetAsync();
            }
        }

        /// <summary>
        /// 執行 SQL 將結果以自訂的  Class 集合傳回
        /// </summary>
        public async Task<IEnumerable<T>> ExecuteModelsAsync<T>(SqlCommand cmd)
        {
            using (SqlConnection conn = Connection)
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                DebugWrite(cmd);
                return (await cmd.ExecuteDataTableAsync()).Rows.ToObjects<T>();
            }
        }

        /// <summary>
        /// 執行 SQL 語法並以 SqlDataReader 處理每一筆資料
        /// </summary>
        public async Task ExecuteReaderEachAsync(SqlCommand cmd, Action<SqlDataReader> func)
        {
            using (SqlConnection conn = Connection)
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                DebugWrite(cmd);
                await cmd.ExecuteReaderEachAsync(func);
            }
        }
        /// <summary>
        /// 執行 SQL 語法並以自訂 Class 處理每一筆資料
        /// </summary>
        public async Task ExecuteReaderEachAsync<T>(SqlCommand cmd, Action<T> func)
        {
            using (SqlConnection conn = Connection)
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                DebugWrite(cmd);
                await cmd.ExecuteReaderEachAsync(func);
            }
        }

        /// <summary>
        /// 用交易式 Command 來執行 SQL 語法，全部成功才會更新，傳回是否更新成功與受影響資料筆數的總和
        /// </summary>
        public async Task<(bool IsCommit, int QueryCount)> ExecuteSqlTransactionAsync(params SqlCommand[] commands)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        int count = 0;
                        foreach (SqlCommand cmd in commands)
                        {
                            cmd.Connection = conn;
                            cmd.Transaction = tran;
                            DebugWrite(cmd);
                            count += await cmd.ExecuteNonQueryAsync();
                        }
                        tran.Commit();
                        return (true, count);
                    }
                    catch
                    {
                        tran.Rollback();
                        return (false, 0);
                    }
                }
            }
        }
    }
}
