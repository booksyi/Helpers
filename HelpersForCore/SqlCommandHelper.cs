using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace HelpersForCore
{
    public class SqlCommandHelper
    {
        #region COUNT
        /// <summary>
        /// 生成 SELECT COUNT(*) 指令
        /// </summary>
        public static SqlCommand GenerateCount(string tableName, Dictionary<string, object> equals)
        {
            string commandText = $@"
                SELECT COUNT(*)
                FROM [{tableName}]
                WHERE {string.Join(" AND ", equals.Select(x => $"[{x.Key}] = @{x.Key}"))}";
            return new SqlCommand(commandText).AddParameters(equals
                .Select(x => new SqlParameter($"@{x.Key}", x.Value ?? DBNull.Value)).ToArray());
        }
        /// <summary>
        /// 生成 SELECT COUNT(*) 指令
        /// </summary>
        public static SqlCommand GenerateCount(string tableName, string where = null, IEnumerable<SqlParameter> parameters = null)
        {
            string commandText = $@"
                SELECT COUNT(*)
                FROM [{tableName}]{(string.IsNullOrWhiteSpace(where) ? "" : $@"
                WHERE {where}")}";
            return new SqlCommand(commandText).AddParameters(parameters?.ToArray());
        }
        /// <summary>
        /// 生成 SELECT COUNT(*) 指令
        /// </summary>
        public static SqlCommand GenerateCount<T>(Dictionary<string, object> equals)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            return GenerateCount(tableName, equals);
        }
        /// <summary>
        /// 生成 SELECT COUNT(*) 指令
        /// </summary>
        public static SqlCommand GenerateCount<T>(string where = null, IEnumerable<SqlParameter> parameters = null)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            return GenerateCount(tableName, where, parameters);
        }
        #endregion
        #region SELECT
        /// <summary>
        /// 生成 SELECT 指令
        /// </summary>
        public static SqlCommand GenerateSelect(string tableName, IEnumerable<string> columns, Dictionary<string, object> equals)
        {
            string selectColumns = "*";
            if (columns != null && columns.Any())
            {
                selectColumns = string.Join(", ", columns.Select(x => $"[{x}]"));
            }
            string commandText = $@"
                SELECT {selectColumns}
                FROM [{tableName}]
                WHERE {string.Join(" AND ", equals.Select(x => $"[{x.Key}] = @{x.Key}"))}";
            return new SqlCommand(commandText).AddParameters(equals
                .Select(x => new SqlParameter($"@{x.Key}", x.Value ?? DBNull.Value)).ToArray());
        }
        /// <summary>
        /// 生成 SELECT 指令
        /// </summary>
        public static SqlCommand GenerateSelect(string tableName, IEnumerable<string> columns, string where = null, IEnumerable<SqlParameter> parameters = null)
        {
            string selectColumns = "*";
            if (columns != null && columns.Any())
            {
                selectColumns = string.Join(", ", columns.Select(x => $"[{x}]"));
            }
            string commandText = $@"
                SELECT {selectColumns}
                FROM [{tableName}]{(string.IsNullOrWhiteSpace(where) ? "" : $@"
                WHERE {where}")}";
            return new SqlCommand(commandText).AddParameters(parameters?.ToArray());
        }
        /// <summary>
        /// 生成 SELECT 指令
        /// </summary>
        public static SqlCommand GenerateSelect(string tableName, Dictionary<string, object> equals)
        {
            return GenerateSelect(tableName, null, equals);
        }
        /// <summary>
        /// 生成 SELECT 指令
        /// </summary>
        public static SqlCommand GenerateSelect(string tableName, string where = null, IEnumerable<SqlParameter> parameters = null)
        {
            return GenerateSelect(tableName, null, where, parameters);
        }
        /// <summary>
        /// 生成 SELECT 指令
        /// </summary>
        public static SqlCommand GenerateSelect<T>(IEnumerable<string> columns, Dictionary<string, object> equals)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            return GenerateSelect(tableName, columns, equals);
        }
        /// <summary>
        /// 生成 SELECT 指令
        /// </summary>
        public static SqlCommand GenerateSelect<T>(IEnumerable<string> columns, string where = null, IEnumerable<SqlParameter> parameters = null)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            return GenerateSelect(tableName, columns, where, parameters);
        }
        /// <summary>
        /// 生成 SELECT 指令
        /// </summary>
        public static SqlCommand GenerateSelect<T>(Dictionary<string, object> equals)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            IEnumerable<string> columns = CSharpHelper.GetMappedProperties<T>().Select(x => CSharpHelper.GetColumnName(x));
            return GenerateSelect(tableName, columns, equals);
        }
        /// <summary>
        /// 生成 SELECT 指令
        /// </summary>
        public static SqlCommand GenerateSelect<T>(string where = null, IEnumerable<SqlParameter> parameters = null)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            IEnumerable<string> columns = CSharpHelper.GetMappedProperties<T>().Select(x => CSharpHelper.GetColumnName(x));
            return GenerateSelect(tableName, columns, where, parameters);
        }
        #endregion
        #region INSERT
        /// <summary>
        /// 生成 INSERT 指令
        /// </summary>
        public static SqlCommand GenerateInsert(string tableName, string identityColumn, Dictionary<string, object> columns)
        {
            string commandText = $@"
                INSERT INTO [{tableName}]
                ({string.Join(", ", columns.Select(x => $"[{x.Key}]"))}){(string.IsNullOrWhiteSpace(identityColumn) ? "" : $@"
                OUTPUT INSERTED.[{identityColumn}]")}
                VALUES
                ({string.Join(", ", columns.Select(x => $"@{x.Key}"))})";
            SqlParameter[] parameters = columns.Where(x => x.Key != identityColumn).Select(x => new SqlParameter($"@{x.Key}", x.Value ?? DBNull.Value)).ToArray();
            return new SqlCommand(commandText).AddParameters(parameters);
        }
        /// <summary>
        /// 生成 INSERT 指令
        /// </summary>
        public static SqlCommand GenerateInsert(string tableName, Dictionary<string, object> columns)
        {
            return GenerateInsert(tableName, null, columns);
        }
        /// <summary>
        /// 生成 INSERT 指令
        /// </summary>
        public static SqlCommand GenerateInsert<T>(T model, IEnumerable<string> containsColumns = null)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            string identityColumn = CSharpHelper.GetIdentityColumnName<T>();
            Dictionary<string, object> columns = CSharpHelper.GetMappedProperties<T>().ToDictionary(x => CSharpHelper.GetColumnName(x), x => x.GetValue(model));
            if (containsColumns != null && containsColumns.Any())
            {
                columns = columns.Where(x => containsColumns.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            }
            return GenerateInsert(tableName, identityColumn, columns);
        }
        /// <summary>
        /// 生成 INSERT 指令
        /// </summary>
        public static SqlCommand GenerateInsertSkip<T>(T model, IEnumerable<string> skipColumns = null)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            string identityColumn = CSharpHelper.GetIdentityColumnName<T>();
            Dictionary<string, object> columns = CSharpHelper.GetMappedProperties<T>().ToDictionary(x => CSharpHelper.GetColumnName(x), x => x.GetValue(model));
            if (skipColumns != null && skipColumns.Any())
            {
                columns = columns.Where(x => skipColumns.Contains(x.Key) == false).ToDictionary(x => x.Key, x => x.Value);
            }
            return GenerateInsert(tableName, identityColumn, columns);
        }
        #endregion
        #region INSERT (Multi-Rows)
        /// <summary>
        /// 生成 INSERT 指令 (多筆資料)
        /// </summary>
        public static IEnumerable<SqlCommand> GenerateInsert(string tableName, IEnumerable<Dictionary<string, object>> rows)
        {
            List<SqlCommand> commands = new List<SqlCommand>();

            rows = rows.Where(row => row != null);
            int commandIndex = 0;
            int commandRows = 500;
            int count = rows.Count();

            string commandTextHead = $@"
                INSERT INTO [{tableName}]
                ({string.Join(", ", rows.First().Select(x => $"[{x.Key}]"))})
                VALUES";

            while (count > (commandIndex * commandRows))
            {
                List<SqlParameter> parameters = new List<SqlParameter>();

                int i = 0;
                List<string> commandValues = new List<string>();
                foreach (Dictionary<string, object> row in rows.Skip(commandIndex * commandRows).Take(commandRows))
                {
                    commandValues.Add($"({string.Join(", ", row.Select(x => $"@{x.Key}_{i}"))})");
                    parameters.AddRange(row.Select(x => new SqlParameter($"@{x.Key}_{i}", x.Value ?? DBNull.Value)));
                    i++;
                }

                string commandText = $"{commandTextHead} {string.Join(", ", commandValues)}";
                commands.Add(new SqlCommand(commandText).AddParameters(parameters.ToArray()));
                commandIndex++;
            }

            return commands;
        }
        /// <summary>
        /// 生成 INSERT 指令 (多筆資料)
        /// </summary>
        public static IEnumerable<SqlCommand> GenerateInsert(DataTable dt)
        {
            string tableName = dt.TableName;
            var columns = dt.Columns.Cast<DataColumn>().Select(x => x.ColumnName);
            var rows = dt.Rows.Cast<DataRow>().Select(dr => columns.ToDictionary(x => x, x => dr[x]));
            return GenerateInsert(tableName, rows);
        }
        /// <summary>
        /// 生成 INSERT 指令 (多筆資料)
        /// </summary>
        public static IEnumerable<SqlCommand> GenerateInsert<T>(IEnumerable<T> models)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            var rows = models
                .Where(model => model != null)
                .Select(model => model.ToDictionary(true));
            return GenerateInsert(tableName, rows);
        }
        #endregion
        #region UPDATE
        /// <summary>
        /// 生成 UPDATE 指令
        /// </summary>
        public static SqlCommand GenerateUpdate(string tableName, Dictionary<string, object> columns, Dictionary<string, object> equals)
        {
            return new SqlCommand($@"
                UPDATE [{tableName}]
                SET {string.Join(", ", columns.Select(x => $"[{x.Key}] = @{x.Key}"))}{(equals == null || !equals.Any() ? "" : $@"
                WHERE {string.Join(" AND ", equals.Select(x => $"[{x.Key}] = @{x.Key}"))}")}")
                .AddParameters(columns.Select(x => new SqlParameter($"@{x.Key}", x.Value ?? DBNull.Value)).ToArray())
                .AddParameters(equals?.Select(x => new SqlParameter($"@{x.Key}", x.Value ?? DBNull.Value)).ToArray());
        }
        /// <summary>
        /// 生成 UPDATE 指令
        /// </summary>
        public static SqlCommand GenerateUpdate(string tableName, Dictionary<string, object> columns, string where, IEnumerable<SqlParameter> parameters = null)
        {
            return new SqlCommand($@"
                UPDATE [{tableName}]
                SET {string.Join(", ", columns.Select(x => $"[{x.Key}] = @{x.Key}"))}{(string.IsNullOrWhiteSpace(where) ? "" : $@"
                WHERE {where}")}")
                .AddParameters(columns.Select(x => new SqlParameter($"@{x.Key}", x.Value ?? DBNull.Value)).ToArray())
                .AddParameters(parameters?.ToArray());
        }
        /// <summary>
        /// 生成 UPDATE 指令
        /// </summary>
        public static SqlCommand GenerateUpdate<T>(T model, IEnumerable<string> containsColumns, IEnumerable<string> whereColumns)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            Dictionary<string, object> columns = CSharpHelper.GetMappedProperties<T>().ToDictionary(x => CSharpHelper.GetColumnName(x), x => x.GetValue(model));
            Dictionary<string, object> where = null;
            if (whereColumns != null && whereColumns.Any())
            {
                where = columns.Where(x => whereColumns.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            }
            if (containsColumns != null && containsColumns.Any())
            {
                columns = columns.Where(x => containsColumns.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            }
            return GenerateUpdate(tableName, columns, where);
        }
        /// <summary>
        /// 生成 UPDATE 指令
        /// </summary>
        public static SqlCommand GenerateUpdate<T>(T model, IEnumerable<string> containsColumns, string where, IEnumerable<SqlParameter> parameters = null)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            Dictionary<string, object> columns = CSharpHelper.GetMappedProperties<T>().ToDictionary(x => CSharpHelper.GetColumnName(x), x => x.GetValue(model));
            if (containsColumns != null && containsColumns.Any())
            {
                columns = columns.Where(x => containsColumns.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            }
            return GenerateUpdate(tableName, columns, where, parameters);
        }
        /// <summary>
        /// 生成 UPDATE 指令
        /// </summary>
        public static SqlCommand GenerateUpdate<T>(T model, IEnumerable<string> whereColumns)
        {
            return GenerateUpdate(model, null, whereColumns);
        }
        /// <summary>
        /// 生成 UPDATE 指令
        /// </summary>
        public static SqlCommand GenerateUpdate<T>(T model, string where, IEnumerable<SqlParameter> parameters = null)
        {
            return GenerateUpdate(model, null, where, parameters);
        }

        /// <summary>
        /// 生成 UPDATE 指令
        /// </summary>
        public static SqlCommand GenerateUpdateSkip<T>(T model, IEnumerable<string> skipColumns, IEnumerable<string> whereColumns)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            Dictionary<string, object> columns = CSharpHelper.GetMappedProperties<T>().ToDictionary(x => CSharpHelper.GetColumnName(x), x => x.GetValue(model));
            Dictionary<string, object> where = null;
            if (whereColumns != null && whereColumns.Any())
            {
                where = columns.Where(x => whereColumns.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            }
            if (skipColumns != null && skipColumns.Any())
            {
                columns = columns.Where(x => skipColumns.Contains(x.Key) == false).ToDictionary(x => x.Key, x => x.Value);
            }
            return GenerateUpdate(tableName, columns, where);
        }
        /// <summary>
        /// 生成 UPDATE 指令
        /// </summary>
        public static SqlCommand GenerateUpdateSkip<T>(T model, IEnumerable<string> skipColumns, string where, IEnumerable<SqlParameter> parameters = null)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            Dictionary<string, object> columns = CSharpHelper.GetMappedProperties<T>().ToDictionary(x => CSharpHelper.GetColumnName(x), x => x.GetValue(model));
            if (skipColumns != null && skipColumns.Any())
            {
                columns = columns.Where(x => skipColumns.Contains(x.Key) == false).ToDictionary(x => x.Key, x => x.Value);
            }
            return GenerateUpdate(tableName, columns, where, parameters);
        }
        #endregion
        #region DELETE
        /// <summary>
        /// 生成 DELETE 指令
        /// </summary>
        public static SqlCommand GenerateDelete(string tableName, string where)
        {
            string commandText = $@"
                DELETE FROM [{tableName}]{(string.IsNullOrWhiteSpace(where) ? "" : $@"
                WHERE {where}")}";
            return new SqlCommand(commandText);
        }
        /// <summary>
        /// 生成 DELETE 指令
        /// </summary>
        public static SqlCommand GenerateDelete(string tableName, Dictionary<string, object> equals)
        {
            string commandText = $@"
                DELETE FROM [{tableName}]{(equals == null || !equals.Any() ? "" : $@"
                WHERE WHERE {string.Join(" AND ", equals.Select(x => $"[{x.Key}] = @{x.Key}"))}")}";
            return new SqlCommand(commandText)
                .AddParameters(equals.Select(x => new SqlParameter($"@{x.Key}", x.Value ?? DBNull.Value)).ToArray());
        }
        /// <summary>
        /// 生成 DELETE 指令
        /// </summary>
        public static SqlCommand GenerateDelete<T>(T model, IEnumerable<string> whereColumns)
        {
            string tableName = CSharpHelper.GetTableName<T>();
            Dictionary<string, object> columns = CSharpHelper.GetMappedProperties<T>().ToDictionary(x => CSharpHelper.GetColumnName(x), x => x.GetValue(model));
            Dictionary<string, object> where = null;
            if (whereColumns != null && whereColumns.Any())
            {
                where = columns.Where(x => whereColumns.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            }
            return GenerateDelete(tableName, where);
        }
        #endregion

        /// <summary>
        /// 生成條件子句 (A = @A AND B = @B AND ...)
        /// </summary>
        public static (string Sql, SqlParameter[] Parameters) GenerateFilterEquals(Dictionary<string, object> equals)
        {
            string sql = string.Join(" AND ", equals.Select(x => $"[{x.Key}] = @{x.Key}"));
            SqlParameter[] parameters = equals.Select(x => new SqlParameter($"@{x.Key}", x.Value ?? DBNull.Value)).ToArray();
            return (sql, parameters);
        }
    }
}
