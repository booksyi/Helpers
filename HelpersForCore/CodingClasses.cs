using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HelpersForCore
{
    public class DbTableSchema
    {
        public string TableName { get; set; }
        public IEnumerable<Field> Fields { get; set; }

        public Field Identity { get => Fields.FirstOrDefault(x => x.IsIdentity); }

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
            public ForCsharp ForCs { get; set; } = new ForCsharp();
            public ForTypeScript ForTs { get; set; } = new ForTypeScript();
        }

        public class ForCsharp
        {
            public IEnumerable<string> EFAttributes { get; set; }
            public string TypeName { get; set; }
        }
        public class ForTypeScript
        {
            public string TypeName { get; set; }
        }
    }

    public class GenerateNode
    {
        public string Name { get; private set; }
        public string Text { get; set; }
        public List<GenerateNode> Children { get; private set; }

        public GenerateNode(string name, string text)
        {
            Name = name;
            Text = text;
            Children = new List<GenerateNode>();
        }

        public void Rename(string newName)
        {
            Name = newName;
        }
    }
}
