using System;
using System.Collections.Generic;
using System.Text;

namespace HelpersForCore
{
    public class DbTableSchema
    {
        public string TableName { get; set; }
        public IEnumerable<Field> Fields { get; set; }

        public class Field
        {
            public string Title { get; set; }
            public string Name { get; set; }
            public string DbType { get; set; }
            public string DbFullType { get; set; }
            public int Nullable { get; set; }
            public int Length { get; set; }
            public int Prec { get; set; }
            public int Scale { get; set; }
            public string CsType { get; set; }
            public string Description { get; set; }
        }
    }

    public enum ApiActionType
    {
        GetModels,
        GetModel,
        CreateModel,
        UpdateModel,
        DeleteModel
    }

    public class GenerateCSharpOption
    {
        public IEnumerable<string> UsingNamespaces { get; set; }
        public string Namespace { get; set; }
        public IEnumerable<GenerateCSharpClassOption> Classes { get; set; }
    }

    public class GenerateCSharpClassOption
    {
        public IEnumerable<string> Attributes { get; set; }
        public string Prefix { get; set; }
        public string Name { get; set; }
        public string Inheritance { get; set; }
        public GenerateCSharpConstructorOption Constructor { get; set; }
        public IEnumerable<GenerateCSharpClassOption> Classes { get; set; }
        public IEnumerable<GenerateCSharpFieldOption> Fields { get; set; }
        public IEnumerable<GenerateCSharpPropertyOption> Properties { get; set; }
        public IEnumerable<GenerateCSharpMethodOption> Methods { get; set; }
    }

    public class GenerateCSharpFieldOption
    {
        public IEnumerable<string> Attributes { get; set; }
        public string Prefix { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
    }

    public class GenerateCSharpPropertyOption
    {
        public IEnumerable<string> Attributes { get; set; }
        public string Prefix { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public bool GenerateGet { get; set; } = true;
        public bool GenerateSet { get; set; } = true;
        public string GetPrefix { get; set; }
        public string SetPrefix { get; set; }
    }

    public class GenerateCSharpMethodOption
    {
        public string Prefix { get; set; }
        public string ReturnType { get; set; }
        public string Name { get; set; }
        public string Parameters { get; set; }
        public string InnerCode { get; set; }
    }

    public class GenerateCSharpConstructorOption
    {
        public string Name { get; set; }
        public string Parameters { get; set; }
        public string Base { get; set; }
        public string InnerCode { get; set; }
    }

}
