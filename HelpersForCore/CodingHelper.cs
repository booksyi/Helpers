using AutoMapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HelpersForCore
{
    public class CodingHelper
    {
        private readonly IMapper mapper;
        public CodingHelper(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public string GenerateCode(CsSchema.Unit unit)
        {
            var syntax = mapper.Map<CompilationUnitSyntax>(unit);
            return syntax.NormalizeWhitespace().ToFullString();
        }

        #region Static Methods
        /// <summary>
        /// 從程式碼讀出 class 的結構
        /// </summary>
        public static CsSchema.Class AnalysisClass(string programText)
        {
            CsSchema.Class csClass = new CsSchema.Class();
            List<CsSchema.Property> csProperties = new List<CsSchema.Property>();
            SyntaxTree tree = CSharpSyntaxTree.ParseText(programText);
            CompilationUnitSyntax root = tree.GetRoot() as CompilationUnitSyntax;
            ClassDeclarationSyntax classSyntax =
                root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
            IEnumerable<PropertyDeclarationSyntax> propertiesSyntax =
                classSyntax.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var propertySyntax in propertiesSyntax)
            {
                PredefinedTypeSyntax typeSyntax =
                    propertySyntax.DescendantNodes().OfType<PredefinedTypeSyntax>().Single();
                SyntaxToken type = typeSyntax.Keyword;
                SyntaxToken property = propertySyntax.Identifier;
                csProperties.Add(new CsSchema.Property()
                {
                    Name = property.Text,
                    TypeName = type.Text
                });
            }
            csClass.Name = classSyntax.Identifier.Text;
            csClass.Properties = csProperties.ToArray();
            return csClass;
        }

        /// <summary>
        /// 取得資料庫的所有資料表名稱
        /// </summary>
        public static IEnumerable<string> GetDbTableNames(string connectionString)
        {
            SqlHelper sqlHelper = new SqlHelper(connectionString);
            return sqlHelper.ExecuteFirstColumn(@"
                SELECT [name]
                FROM sysobjects
                WHERE [type] = 'U'").Select(x => Convert.ToString(x));
        }

        /// <summary>
        /// 取得 DB Table 的結構資訊
        /// </summary>
        public static DbSchema.Table GetDbTableSchema(string connectionString, string tableName)
        {
            SqlHelper sqlHelper = new SqlHelper(connectionString);
            DbSchema.Table schema = new DbSchema.Table();
            schema.Name = tableName;
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
                .Rows.ToObjects<DbSchema.Field>().ToArray();
            foreach (DbSchema.Field field in schema.Fields)
            {
                field.TypeFullName = GetDbTypeFullName(field);
            }
            return schema;
        }

        /// <summary>
        /// 取得 DB 多個 Table 的結構資訊
        /// </summary>
        public static IEnumerable<DbSchema.Table> GetDbTableSchema(string connectionString, IEnumerable<string> tableNames)
        {
            return tableNames.Select(x => GetDbTableSchema(connectionString, x));
        }

        /// <summary>
        /// 取得 DB 欄位包含長度的類型名稱
        /// </summary>
        public static string GetDbTypeFullName(DbSchema.Field field)
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
        #endregion
    }
}
