using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace HelpersForCore
{
    public static class SqlCommandExtension
    {
        /// <summary>
        /// 執行 SqlCommand 並將完整的結果以 DataSet 類型傳回
        /// </summary>
        public static DataSet ExecuteDataSet(this SqlCommand cmd)
        {
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                DataSet ds = new DataSet();
                da.Fill(ds);
                return ds;
            }
        }

        /// <summary>
        /// 執行 SqlCommand 並將完整的結果以 DataTable 類型傳回
        /// </summary>
        public static DataTable ExecuteDataTable(this SqlCommand cmd)
        {
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
        }

        /// <summary>
        /// 執行 SqlCommand 並將完整的結果以 DataTable 類型傳回
        /// </summary>
        public static DataTable ExecuteDataTable(this SqlCommand cmd, string tableName)
        {
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                DataTable dt = new DataTable(tableName);
                dt.Load(reader);
                return dt;
            }
        }

        /// <summary>
        /// 執行 SqlCommand 並將所有結果的第一個欄位以集合傳回
        /// </summary>
        public static IEnumerable<object> ExecuteFirstColumn(this SqlCommand cmd)
        {
            DataTable dt = cmd.ExecuteDataTable();
            if (dt.Columns.Count > 0)
            {
                return dt.Rows.Cast<DataRow>().Select(dr => dr.ItemArray[0]);
            }
            return null;
        }

        /// <summary>
        /// 執行 SqlCommand 並將第一筆結果以 DataRow 類型傳回
        /// </summary>
        public static DataRow ExecuteFirstRow(this SqlCommand cmd)
        {
            DataTable dt = cmd.ExecuteDataTable();
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0];
            }
            return null;
        }

        /// <summary>
        /// 執行 SQL 語法並以 SqlDataReader 處理每一筆資料
        /// </summary>
        public static void ExecuteReaderEach(this SqlCommand cmd, Action<SqlDataReader> func)
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    func(dr);
                }
            }
        }
        /// <summary>
        /// 執行 SQL 語法並以自訂 Class 處理每一筆資料
        /// </summary>
        public static void ExecuteReaderEach<T>(this SqlCommand cmd, Action<T> func)
        {
            cmd.ExecuteReaderEach(dr => {
                T model = dr.ToModel<T>();
                func(model);
            });
        }

        /// <summary>
        /// 執行 SqlCommand 並將完整的結果以 DataSet 類型傳回
        /// </summary>
        public static async Task<DataSet> ExecuteDataSetAsync(this SqlCommand cmd)
        {
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                DataSet ds = new DataSet();
                await Task.Run(() => da.Fill(ds));
                return ds;
            }
        }

        /// <summary>
        /// 執行 SqlCommand 並將完整的結果以 DataTable 類型傳回
        /// </summary>
        public static async Task<DataTable> ExecuteDataTableAsync(this SqlCommand cmd)
        {
            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
        }

        /// <summary>
        /// 執行 SqlCommand 並將完整的結果以 DataTable 類型傳回
        /// </summary>
        public static async Task<DataTable> ExecuteDataTableAsync(this SqlCommand cmd, string tableName)
        {
            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                DataTable dt = new DataTable(tableName);
                dt.Load(reader);
                return dt;
            }
        }

        /// <summary>
        /// 執行 SqlCommand 並將所有結果的第一個欄位以集合傳回
        /// </summary>
        public static async Task<IEnumerable<object>> ExecuteFirstColumnAsync(this SqlCommand cmd)
        {
            DataTable dt = await cmd.ExecuteDataTableAsync();
            if (dt.Columns.Count > 0)
            {
                return dt.Rows.Cast<DataRow>().Select(dr => dr.ItemArray[0]);
            }
            return null;
        }

        /// <summary>
        /// 執行 SqlCommand 並將第一筆結果以 DataRow 類型傳回
        /// </summary>
        public static async Task<DataRow> ExecuteFirstRowAsync(this SqlCommand cmd)
        {
            DataTable dt = await cmd.ExecuteDataTableAsync();
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0];
            }
            return null;
        }

        /// <summary>
        /// 執行 SQL 語法並以 SqlDataReader 處理每一筆資料
        /// </summary>
        public static async Task ExecuteReaderEachAsync(this SqlCommand cmd, Action<SqlDataReader> func)
        {
            using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
            {
                while (await dr.ReadAsync())
                {
                    func(dr);
                }
            }
        }
        /// <summary>
        /// 執行 SQL 語法並以自訂 Class 處理每一筆資料
        /// </summary>
        public static async Task ExecuteReaderEachAsync<T>(this SqlCommand cmd, Action<T> func)
        {
            await cmd.ExecuteReaderEachAsync(dr => {
                T model = dr.ToModel<T>();
                func(model);
            });
        }

        /// <summary>
        /// 直接以 SqlCommand 增加 Parameters
        /// </summary>
        public static SqlCommand AddParameters(this SqlCommand cmd, params SqlParameter[] parameters)
        {
            if (parameters != null && parameters.Any())
            {
                foreach (SqlParameter parameter in parameters)
                {
                    if (parameter.Value == null)
                    {
                        parameter.Value = DBNull.Value;
                    }
                }
                cmd.Parameters.AddRange(parameters);
            }
            return cmd;
        }
    }
}
