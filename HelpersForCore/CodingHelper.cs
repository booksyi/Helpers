using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpersForCore
{
    public class CodingHelper
    {
        public static Dictionary<string, string> GenerateSynonyms = new Dictionary<string, string>
        {
            { "@@Space", " " },
            { "@@Endline", "\r\n" },
            { "@@Tab", "\t" }
        };

        public static string Generate(GenerateNode root)
        {
            const string paramLeft = "{{#";
            const string paramRight = "}}";
            const string eachSeparator = ",";
            const string withDefault = "|";

            if (string.IsNullOrWhiteSpace(root.Text))
            {
                return null;
            }

            string result = root.Text;

            int searchIndex = 0;
            int paramStartIndex = result.IndexOf(paramLeft);
            while (paramStartIndex >= 0)
            {
                paramStartIndex = paramStartIndex + paramLeft.Length;
                int paramEndIndex = result.IndexOf(paramRight, paramStartIndex) - 1;
                int lineStartIndex = result.Substring(0, paramStartIndex).LastIndexOf("\n") + 1;
                int lineEndIndex = CSharpHelper.Using(result.IndexOf("\n", lineStartIndex + 1), x => x >= 0 ? x : result.Length) - 1;
                int separatorStartIndex = CSharpHelper.Using(result.Substring(0, paramEndIndex).IndexOf(eachSeparator, paramStartIndex), x => x >= 0 ? x + eachSeparator.Length : -1);
                int defaultStartIndex = CSharpHelper.Using(result.Substring(0, paramEndIndex).IndexOf(withDefault, Math.Max(paramStartIndex, separatorStartIndex)), x => x >= 0 ? x + withDefault.Length : -1);
                int separatorEndIndex = defaultStartIndex >= 0 ? defaultStartIndex - withDefault.Length - 1 : paramEndIndex;
                int defaultEndIndex = paramEndIndex;
                int keyStartIndex = paramStartIndex;
                int keyEndIndex = separatorStartIndex >= 0 ? separatorStartIndex - eachSeparator.Length - 1 : defaultStartIndex >= 0 ? defaultStartIndex - withDefault.Length - 1 : paramEndIndex;
                if (paramStartIndex < paramEndIndex
                    && paramEndIndex < lineEndIndex
                    && separatorStartIndex < paramEndIndex
                    && defaultStartIndex < paramEndIndex)
                {
                    string prefix = result.Substring(lineStartIndex, paramStartIndex - paramLeft.Length - lineStartIndex);
                    string suffix = result.Substring(paramEndIndex + paramRight.Length + 1, lineEndIndex - (paramEndIndex + paramRight.Length));
                    string key = result.Substring(keyStartIndex, keyEndIndex - keyStartIndex + 1).Trim();
                    string value = "";
                    string param = result.Substring(paramStartIndex, paramEndIndex - paramStartIndex + 1);
                    string separator = separatorStartIndex >= 0 ? result.Substring(separatorStartIndex, separatorEndIndex - separatorStartIndex + 1).Trim() : "";
                    string @default = defaultStartIndex >= 0 ? result.Substring(defaultStartIndex, defaultEndIndex - defaultStartIndex + 1).Trim() : "";
                    foreach (var synonym in GenerateSynonyms)
                    {
                        separator = separator.Replace(synonym.Key, synonym.Value);
                        @default = @default.Replace(synonym.Key, synonym.Value);
                    }
                    #region Apply
                    if (string.IsNullOrWhiteSpace(key) == false)
                    {
                        if (root.Children.Any(x => x.Name == key))
                        {
                            value = string.Join(
                                separator,
                                root.Children
                                    .Where(x => x.Name == key)
                                    .Select(node =>
                                    {
                                        string nodeValue = null;
                                        if (node.Children.Any())
                                        {
                                            nodeValue = Generate(node);
                                        }
                                        else
                                        {
                                            nodeValue = node.Text;
                                        }
                                        if (string.IsNullOrWhiteSpace(nodeValue))
                                        {
                                            nodeValue = @default;
                                        }
                                        return nodeValue;
                                    }));
                        }
                        if (string.IsNullOrWhiteSpace(prefix))
                        {
                            value = value.Replace("\n", $"\n{prefix}");
                        }
                        if (string.IsNullOrWhiteSpace(value)
                            && string.IsNullOrWhiteSpace(prefix)
                            && string.IsNullOrWhiteSpace(suffix))
                        {
                            if (lineEndIndex + 2 < result.Length)
                            {
                                result = $"{result.Substring(0, Math.Max(lineStartIndex - 1, 0))}{result.Substring(lineEndIndex + 2)}";
                                searchIndex = lineStartIndex;
                            }
                            else
                            {
                                result = $"{result.Substring(0, Math.Max(lineStartIndex - 1, 0))}";
                                searchIndex = -1;
                            }
                        }
                        else
                        {
                            result = $"{result.Substring(0, paramStartIndex - paramLeft.Length)}{value}{result.Substring(paramEndIndex + paramRight.Length + 1)}";
                            searchIndex = paramStartIndex - paramLeft.Length + value.Length;
                        }
                    }
                    else
                    {
                        searchIndex = paramStartIndex + paramRight.Length;
                    }
                    #endregion
                }
                else
                {
                    searchIndex = paramStartIndex + paramRight.Length;
                }
                paramStartIndex = searchIndex >= 0 ? result.IndexOf(paramLeft, searchIndex) : -1;
            }
            return result;
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
            schema.TableName = tableName;
            schema.Fields = sqlHelper.ExecuteDataTable(@"
                SELECT c.NAME                      AS NAME, 
                       t.NAME                      AS TypeName, 
                       c.is_nullable               AS IsNullable, 
                       c.is_identity               AS IsIdentity, 
                       Isnull(i.is_unique, 0)      AS IsUnique, 
                       Isnull(i.is_primary_key, 0) AS IsPrimaryKey, 
                       CASE 
                         WHEN (SELECT Count(1) 
                               FROM   sys.foreign_key_columns 
                               WHERE  parent_object_id = c.object_id 
                                      AND parent_column_id = c.column_id) > 0 THEN 1 
                         ELSE 0 
                       END                         AS IsForeignKey, 
                       CASE 
                         WHEN (SELECT Count(1) 
                               FROM   sys.foreign_key_columns 
                               WHERE  referenced_object_id = c.object_id 
                                      AND referenced_column_id = c.column_id) > 0 THEN 1 
                         ELSE 0 
                       END                         AS IsReferencedForeignKey, 
                       sc.prec                     AS [Length], 
                       c.PRECISION                 AS Prec, 
                       c.scale                     AS Scale, 
                       p_des.value                 AS Description 
                FROM   sys.columns c 
                       INNER JOIN syscolumns sc 
                               ON c.object_id = sc.id 
                                  AND c.column_id = sc.colid 
                       INNER JOIN sys.objects o 
                               ON c.object_id = o.object_id 
                       LEFT JOIN sys.types t 
                              ON t.user_type_id = c.user_type_id 
                       LEFT JOIN sys.extended_properties p_des 
                              ON c.object_id = p_des.major_id 
                                 AND c.column_id = p_des.minor_id 
                                 AND p_des.NAME = 'MS_Description' 
                       LEFT JOIN sys.index_columns AS ic 
                              ON c.object_id = ic.object_id 
                                 AND c.column_id = ic.column_id 
                       LEFT JOIN sys.indexes i 
                              ON c.object_id = i.object_id 
                                 AND ic.index_id = i.index_id 
                WHERE  o.type = 'U' 
                       AND o.NAME = @TableName 
                ORDER  BY sc.colorder ",
                new System.Data.SqlClient.SqlParameter("@TableName", tableName))
                .Rows.ToModels<DbTableSchema.Field>();
            foreach (DbTableSchema.Field field in schema.Fields)
            {
                field.TypeFullName = GetDbTypeFullName(field);
                field.ForCs.EFAttributes = GetCsEFAttributes(field);
                field.ForCs.TypeName = GetCsTypeName(field);
            }
            return schema;
        }

        /// <summary>
        /// 取得 DB 欄位包含長度的類型名稱
        /// </summary>
        public static string GetDbTypeFullName(DbTableSchema.Field field)
        {
            switch (field.TypeName)
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
                    return $"{field.TypeName}({(field.Length == -1 ? "max" : Convert.ToString(field.Length))})";
                case "decimal":
                case "numeric":
                    return $"{field.TypeName}({field.Prec}, {field.Scale})";
                default:
                    return field.TypeName;
            }
        }

        public static IEnumerable<string> GetCsEFAttributes(DbTableSchema.Field field)
        {
            List<string> attributes = new List<string>();
            if (field.TypeName.In("char", "nchar", "ntext", "nvarchar", "text", "varchar", "xml"))
            {
                if (field.IsNullable == false)
                {
                    attributes.Add("[Required]");
                }
                attributes.Add($@"[Column(""{field.Name}"")]");
                if (field.Length > 0)
                {
                    attributes.Add($"[StringLength({field.Length})]");
                }
            }
            else
            {
                attributes.Add($@"[Column(""{field.Name}"", TypeName = ""{field.TypeFullName}"")]");
            }
            return attributes;
        }

        /// <summary>
        /// 取得對應 DB 類別的 CSharp 類別名稱
        /// </summary>
        public static string GetCsTypeName(DbTableSchema.Field field)
        {
            switch (field.TypeName)
            {
                case "bigint":
                    return $"long{(field.IsNullable ? "?" : "")}";
                case "binary":
                    return "byte[]";
                case "bit":
                    return $"bool{(field.IsNullable ? "?" : "")}";
                case "char":
                    return "string";
                case "date":
                case "datetime":
                case "datetime2":
                    return $"DateTime{(field.IsNullable ? "?" : "")}";
                case "datetimeoffset":
                    return $"DateTimeOffset{(field.IsNullable ? "?" : "")}";
                case "decimal":
                    return $"decimal{(field.IsNullable ? "?" : "")}";
                case "float":
                    return $"double{(field.IsNullable ? "?" : "")}";
                //case "geography":
                //    return "";
                //case "geometry":
                //    return "";
                //case "hierarchyid":
                //    return "";
                case "image":
                    return "byte[]";
                case "int":
                    return $"int{(field.IsNullable ? "?" : "")}";
                case "money":
                    return $"decimal{(field.IsNullable ? "?" : "")}";
                case "nchar":
                    return "string";
                case "ntext":
                    return "string";
                case "numeric":
                    return $"decimal{(field.IsNullable ? "?" : "")}";
                case "nvarchar":
                    return "string";
                case "real":
                    return $"float{(field.IsNullable ? "?" : "")}";
                case "smalldatetime":
                    return $"DateTime{(field.IsNullable ? "?" : "")}";
                case "smallint":
                    return $"short{(field.IsNullable ? "?" : "")}";
                case "smallmoney":
                    return $"decimal{(field.IsNullable ? "?" : "")}";
                case "sql_variant":
                    return "object";
                case "text":
                    return "string";
                case "time":
                    return $"TimeSpan{(field.IsNullable ? "?" : "")}";
                case "timestamp":
                    return "byte[]";
                case "tinyint":
                    return $"byte{(field.IsNullable ? "?" : "")}";
                case "uniqueidentifier":
                    return $"Guid{(field.IsNullable ? "?" : "")}";
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

        public static string GetTsTypeName(DbTableSchema.Field field)
        {
            switch (field.TypeName)
            {
                case "bigint":
                    return "number";
                case "binary":
                    return "any";
                case "bit":
                    return "boolean";
                case "char":
                    return "string";
                case "date":
                case "datetime":
                case "datetime2":
                    return "Date";
                case "datetimeoffset":
                    return "any";
                case "decimal":
                case "float":
                    return "number";
                case "geography":
                    return "any";
                case "geometry":
                    return "any";
                case "hierarchyid":
                    return "any";
                case "image":
                    return "any";
                case "int":
                case "money":
                    return "number";
                case "nchar":
                case "ntext":
                    return "string";
                case "numeric":
                    return $"number";
                case "nvarchar":
                    return "string";
                case "real":
                    return "number";
                case "smalldatetime":
                    return "Date";
                case "smallint":
                    return "number";
                case "smallmoney":
                    return "number";
                case "sql_variant":
                    return "any";
                case "text":
                    return "string";
                case "time":
                    return "any";
                case "timestamp":
                    return "any";
                case "tinyint":
                    return "any";
                case "uniqueidentifier":
                    return "any";
                case "varbinary":
                    return "any";
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
        public static string GetCsConvertMethodName(string csTypeName)
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
