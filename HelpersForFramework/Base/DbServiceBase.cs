using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace HelpersForFramework.Base
{
    /// <summary>
    /// 與 DB 對應的 Service
    /// </summary>
    /// <typeparam name="T">對應 Table 的 Model</typeparam>
    public abstract class DbServiceBase<T>
    {
        public SqlHelper SqlHelper { get; }

        public DbServiceBase(string connectionString, string tableName = null)
        {
            SqlHelper = new SqlHelper(connectionString);
        }

        /// <summary>
        /// 取得所有資料
        /// </summary>
        public virtual IEnumerable<T> SelectAll()
        {
            var cmd = SqlCommandHelper.GenerateSelect<T>();
            return SqlHelper.ExecuteModels<T>(cmd);
        }

        /// <summary>
        /// 取得一筆資料
        /// </summary>
        public virtual T SelectTop1(Dictionary<string, object> equals)
        {
            var cmd = SqlCommandHelper.GenerateSelect<T>(equals);
            return SqlHelper.ExecuteFirstRow(cmd).ToModel<T>();
        }
        /// <summary>
        /// 取得一筆資料
        /// </summary>
        public virtual T SelectTop1(string where, params SqlParameter[] parameters)
        {
            var cmd = SqlCommandHelper.GenerateSelect<T>(where).AddParameters(parameters);
            return SqlHelper.ExecuteFirstRow(cmd).ToModel<T>();
        }

        /// <summary>
        /// 取得多筆資料
        /// </summary>
        public virtual IEnumerable<T> Select(Dictionary<string, object> equals)
        {
            var cmd = SqlCommandHelper.GenerateSelect<T>(equals);
            return SqlHelper.ExecuteModels<T>(cmd);
        }
        /// <summary>
        /// 取得多筆資料
        /// </summary>
        public virtual IEnumerable<T> Select(string where, params SqlParameter[] parameters)
        {
            var cmd = SqlCommandHelper.GenerateSelect<T>(where).AddParameters(parameters);
            return SqlHelper.ExecuteModels<T>(cmd);
        }

        /// <summary>
        /// 新增一筆資料並更新 Identity
        /// </summary>
        public virtual bool Insert(T model, IEnumerable<string> columns)
        {
            var cmd = SqlCommandHelper.GenerateInsert(model, columns);
            object identity = SqlHelper.ExecuteScalar(cmd);
            if (identity != null && identity != DBNull.Value)
            {
                var prop = CSharpHelper.GetIdentityProperty<T>();
                prop.SetValue(model, identity);
            }
            return true;
        }

        /// <summary>
        /// 新增多筆資料
        /// </summary>
        public virtual int Insert(IEnumerable<T> models, IEnumerable<string> columns)
        {
            int count = 0;
            foreach (T model in models)
            {
                if (Insert(model, columns))
                {
                    count = count + 1;
                }
            }
            return count;
        }

        /// <summary>
        /// 更新一筆資料
        /// </summary>
        public virtual bool Update(T model, IEnumerable<string> columns, IEnumerable<string> whereColumns)
        {
            var cmd = SqlCommandHelper.GenerateUpdate(model, columns, whereColumns);
            return SqlHelper.ExecuteNonQuery(cmd) > 0;
        }

        /// <summary>
        /// 分次更新多筆資料
        /// </summary>
        public virtual int Update(IEnumerable<T> models, IEnumerable<string> columns, IEnumerable<string> whereColumns)
        {
            int count = 0;
            foreach (T model in models)
            {
                var cmd = SqlCommandHelper.GenerateUpdate(model, columns, whereColumns);
                count += SqlHelper.ExecuteNonQuery(cmd);
            }
            return count;
        }

        /// <summary>
        /// 更新多筆資料
        /// </summary>
        public virtual int Update(Dictionary<string, object> update, Dictionary<string, object> equals)
        {
            var cmd = SqlCommandHelper.GenerateUpdate(CSharpHelper.GetTableName<T>(), update, equals);
            return SqlHelper.ExecuteNonQuery(cmd);
        }
        /// <summary>
        /// 更新多筆資料
        /// </summary>
        public virtual int Update(Dictionary<string, object> update, string where, params SqlParameter[] parameters)
        {
            var cmd = SqlCommandHelper.GenerateUpdate(CSharpHelper.GetTableName<T>(), update, where).AddParameters(parameters);
            return SqlHelper.ExecuteNonQuery(cmd);
        }

        /// <summary>
        /// 刪除一筆資料
        /// </summary>
        public virtual bool Delete(T model, IEnumerable<string> whereColumns)
        {
            var cmd = SqlCommandHelper.GenerateDelete(model, whereColumns);
            return SqlHelper.ExecuteNonQuery(cmd) > 0;
        }

        /// <summary>
        /// 刪除多筆資料
        /// </summary>
        public virtual int Delete(Dictionary<string, object> equals)
        {
            var cmd = SqlCommandHelper.GenerateDelete(CSharpHelper.GetTableName<T>(), equals);
            return SqlHelper.ExecuteNonQuery(cmd);
        }
        /// <summary>
        /// 刪除多筆資料
        /// </summary>
        public virtual int Delete(string where, params SqlParameter[] parameters)
        {
            var cmd = SqlCommandHelper.GenerateDelete(CSharpHelper.GetTableName<T>(), where).AddParameters(parameters);
            return SqlHelper.ExecuteNonQuery(cmd);
        }

        /// <summary>
        /// 傳回符合條件的資料筆數
        /// </summary>
        public virtual int Count(Dictionary<string, object> equals)
        {
            var cmd = SqlCommandHelper.GenerateCount<T>(equals);
            return Convert.ToInt32(SqlHelper.ExecuteScalar(cmd));
        }
        /// <summary>
        /// 傳回符合條件的資料筆數
        /// </summary>
        public virtual int Count(string where, params SqlParameter[] parameters)
        {
            var cmd = SqlCommandHelper.GenerateCount<T>(where).AddParameters(parameters);
            return Convert.ToInt32(SqlHelper.ExecuteScalar(cmd));
        }

        /// <summary>
        /// 傳回是否有符合條件的資料
        /// </summary>
        public virtual bool Any(Dictionary<string, object> equals)
        {
            return Count(equals) > 0;
        }
        /// <summary>
        /// 傳回是否有符合條件的資料
        /// </summary>
        public virtual bool Any(string where, params SqlParameter[] parameters)
        {
            return Count(where, parameters) > 0;
        }
    }
}
