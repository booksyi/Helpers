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
            public string Description { get; set; }
            public IEnumerable<string> CsAttributes { get; set; }
            public string CsType { get; set; }
        }
    }
}
