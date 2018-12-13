using System;
using System.Collections.Generic;
using System.Linq;

namespace HelpersForFramework
{
    public class CodingHelper
    {
        /// <summary>
        /// 從 DB 取得 Table 的欄位資訊並生成 cs 的 Class
        /// </summary>
        public static string GenerateModel(string connectionString, string tableName, bool useSummary = true, bool useColumnAttr = false)
        {
            List<string> props = new List<string>();
            DbTableSchema schema = GetDbTableSchema(connectionString, tableName);
            foreach (var field in schema.Fields)
            {
                string prop = "";
                if (useSummary && !string.IsNullOrWhiteSpace(field.Description))
                {
                    prop = $@"
                        {prop.ReplaceEndline(@"
                        ")}
                        /// <summary>
                        /// {field.Description.ReplaceEndline(@"
                        /// ")}
                        /// </summary>".CutEmptyHead().DecreaseIndentAllLines();
                }
                if (useColumnAttr)
                {
                    if (field.DbType.In(
                        "char", "nchar", "ntext", "nvarchar", "text", "varchar", "xml"))
                    {
                        if (field.Nullable == 0)
                        {
                            prop = $@"
                                {prop.ReplaceEndline(@"
                                ")}
                                [Required]".CutEmptyHead().DecreaseIndentAllLines();
                        }
                        prop = $@"
                            {prop.ReplaceEndline(@"
                            ")}
                            [Column(""{field.Name}"")]".CutEmptyHead().DecreaseIndentAllLines();
                        if (field.Length > 0)
                        {
                            prop = $@"
                                {prop.ReplaceEndline(@"
                                ")}
                                [StringLength({field.Length})]".CutEmptyHead().DecreaseIndentAllLines();
                        }
                    }
                    else
                    {
                        prop = $@"
                            {prop.ReplaceEndline(@"
                            ")}
                            [Column(""{field.Name}"", TypeName = ""{field.DbFullType}"")]".CutEmptyHead().DecreaseIndentAllLines();
                    }
                }
                prop = $@"
                    {prop.ReplaceEndline(@"
                    ")}
                    public {field.CsType} {field.Name} {{ get; set; }}".CutEmptyHead().DecreaseIndentAllLines();
                props.Add(prop);
            }

            return $@"
                [Table(""{tableName}"")]
                public class {tableName}
                {{
                    {string.Join(@"
                    ", props.Select(x => x.ReplaceEndline(@"
                    "))).CutEmptyHead().TrimStart()}
                }}".CutEmptyHead().DecreaseIndentAllLines();
        }

        /// <summary>
        /// 取得資料庫的所有資料表名稱
        /// </summary>
        public static IEnumerable<object> GetDbTableNames(string connectionString)
        {
            SqlHelper sqlHelper = new SqlHelper(connectionString);
            return sqlHelper.ExecuteFirstColumn(@"
                SELECT [name]
                FROM sysobjects
                WHERE [type] = 'U'");
        }

        public static DbTableSchema GetDbTableSchemaByContext(string context)
        {
            DbTableSchema schema = new DbTableSchema();
            IEnumerable<string> rows = context.Replace("\r\n", "\n").Split('\n').Where(x => string.IsNullOrWhiteSpace(x) == false);
            foreach (string row in rows)
            {
                // TODO:
            }
            return schema;
        }

        /// <summary>
        /// 取得 DB Table 的結構資訊
        /// </summary>
        public static DbTableSchema GetDbTableSchema(string connectionString, string tableName)
        {
            SqlHelper sqlHelper = new SqlHelper(connectionString);
            DbTableSchema schema = new DbTableSchema();
            schema.Fields = sqlHelper.ExecuteDataTable(@"
                SELECT
                    c.name as Name,
                    t.name as DbType,
                    c.isnullable as Nullable,
					c.prec as [Length],
					c.xprec as Prec,
					c.xscale as Scale,
                    p.value as Description
                FROM syscolumns c
                    INNER JOIN sysobjects o ON o.id = c.id
                    LEFT JOIN systypes t ON t.xusertype = c.xusertype
                    LEFT JOIN sys.extended_properties p ON p.major_id = c.id
                        AND p.minor_id = c.colid
                        AND p.name = 'MS_Description'
                WHERE o.type = 'U' AND o.name = @TableName
                ORDER BY c.colorder",
                new System.Data.SqlClient.SqlParameter("@TableName", tableName))
                .Rows.ToModels<DbTableSchema.Field>();
            foreach (DbTableSchema.Field field in schema.Fields)
            {
                field.DbFullType = GetDbFullTypeName(field.DbType, field.Length, field.Prec, field.Scale);
                field.CsType = GetCsTypeName(field.DbType, field.Nullable == 1);
            }
            return schema;
        }

        /// <summary>
        /// 取得 DB 欄位包含長度的類型名稱
        /// </summary>
        public static string GetDbFullTypeName(string dbTypeName, int length, int prec, int scale)
        {
            switch (dbTypeName)
            {
                case "binary":
                case "char":
                case "datetime2":
                case "datetimeoffset":
                case "nchar":
                case "nvarchar":
                case "time":
                case "varbinary":
                case "varchar":
                    return $"{dbTypeName}({(length == -1 ? "max" : Convert.ToString(length))})";
                case "decimal":
                case "numeric":
                    return $"{dbTypeName}({prec}, {scale})";
                default:
                    return dbTypeName;
            }
        }

        /// <summary>
        /// 取得對應 DB 類別的 CSharp 類別名稱
        /// </summary>
        public static string GetCsTypeName(string dbTypeName, bool nullable = false)
        {
            switch (dbTypeName)
            {
                case "bigint":
                    return $"long{(nullable ? "?" : "")}";
                case "binary":
                    return "byte[]";
                case "bit":
                    return $"bool{(nullable ? "?" : "")}";
                case "char":
                    return "string";
                case "date":
                case "datetime":
                case "datetime2":
                    return $"DateTime{(nullable ? "?" : "")}";
                case "datetimeoffset":
                    return $"DateTimeOffset{(nullable ? "?" : "")}";
                case "decimal":
                    return $"decimal{(nullable ? "?" : "")}";
                case "float":
                    return $"double{(nullable ? "?" : "")}";
                //case "geography":
                //    return "";
                //case "geometry":
                //    return "";
                //case "hierarchyid":
                //    return "";
                case "image":
                    return "byte[]";
                case "int":
                    return $"int{(nullable ? "?" : "")}";
                case "money":
                    return $"decimal{(nullable ? "?" : "")}";
                case "nchar":
                    return "string";
                case "ntext":
                    return "string";
                case "numeric":
                    return $"decimal{(nullable ? "?" : "")}";
                case "nvarchar":
                    return "string";
                case "real":
                    return $"float{(nullable ? "?" : "")}";
                case "smalldatetime":
                    return $"DateTime{(nullable ? "?" : "")}";
                case "smallint":
                    return $"short{(nullable ? "?" : "")}";
                case "smallmoney":
                    return $"decimal{(nullable ? "?" : "")}";
                case "sql_variant":
                    return "object";
                case "text":
                    return "string";
                case "time":
                    return $"TimeSpan{(nullable ? "?" : "")}";
                case "timestamp":
                    return "byte[]";
                case "tinyint":
                    return $"byte{(nullable ? "?" : "")}";
                case "uniqueidentifier":
                    return $"Guid{(nullable ? "?" : "")}";
                case "varbinary":
                    return "byte[]";
                case "varchar":
                    return "string";
                case "xml":
                    return "string";
                default:
                    return null;
            }
        }

        /// <summary>
        /// 取得要將物件轉換成指定 CSharp 類型的 Convert 方法名稱
        /// </summary>
        public static string GetConvertMethodName(string csTypeName)
        {
            switch (csTypeName)
            {
                case "bool":
                case "bool?":
                    return "ToBoolean";
                case "short":
                case "short?":
                    return "ToInt16";
                case "int":
                case "int?":
                    return "ToInt32";
                case "long":
                case "long?":
                    return "ToInt64";
                case "float":
                case "float?":
                    return "ToSingle";
                case "double":
                case "double?":
                    return "ToDouble";
                case "decimal":
                case "decimal?":
                    return "ToDecimal";
                case "DateTime":
                case "DateTime?":
                    return "ToDateTime";
                case "string":
                    return "ToString";
                default:
                    return null;
            }
        }
    }
}
