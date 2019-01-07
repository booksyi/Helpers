using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpersForCore
{
    public class DbTableSchema
    {
        public string TableName { get; set; }
        public IEnumerable<Field> Fields { get; set; }
        public TableForCsharp ForCs { get; set; } = new TableForCsharp();

        public Field Identity { get => Fields.FirstOrDefault(x => x.IsIdentity); }
        public IEnumerable<Field> PrimaryKeys { get => Fields.Where(x => x.IsPrimaryKey); }

        public class Field
        {
            public string Title { get; set; }
            public string Name { get; set; }
            public string TypeName { get; set; }
            public string TypeFullName { get; set; }
            public bool IsNullable { get; set; }
            public bool IsIdentity { get; set; }
            public bool IsUnique { get; set; }
            public bool IsPrimaryKey { get; set; }
            public bool IsForeignKey { get; set; }
            public bool IsReferencedForeignKey { get; set; }
            public int Length { get; set; }
            public int Prec { get; set; }
            public int Scale { get; set; }
            public string Description { get; set; }
            public FieldForCsharp ForCs { get; set; } = new FieldForCsharp();
            public FieldForTypeScript ForTs { get; set; } = new FieldForTypeScript();
        }

        public class TableForCsharp
        {
            public string ModelName { get; set; }
        }

        public class FieldForCsharp
        {
            public IEnumerable<string> EFAttributes { get; set; }
            public string TypeName { get; set; }
        }
        public class FieldForTypeScript
        {
            public string TypeName { get; set; }
        }
    }

    public class GenerateNode
    {
        public string ApplyKey { get; private set; }
        public string ApplyValue { private get; set; }
        public string ApplyFilePath { get; set; }
        public List<GenerateNode> ApplyParameters { get; private set; } = new List<GenerateNode>();

        /// <summary>
        /// 取得 ApplyValue 的值或讀取 ApplyFilePath 路徑的檔案
        /// </summary>
        public async Task<string> GetApplyValueAsync()
        {
            if (string.IsNullOrWhiteSpace(ApplyValue))
            {
                if ((string.IsNullOrWhiteSpace(ApplyFilePath) == false)
                    && System.IO.File.Exists(ApplyFilePath))
                {
                    return await System.IO.File.ReadAllTextAsync(ApplyFilePath);
                }
            }
            return ApplyValue;
        }

        public GenerateNode() { }

        public GenerateNode(string key, string value)
        {
            ApplyKey = key;
            ApplyValue = value;
        }

        public GenerateNode ChangeKey(string newKey)
        {
            ApplyKey = newKey;
            return this;
        }
    }
}
