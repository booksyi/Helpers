using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        public string ApplyKey { get; set; }
        public string ApplyValue { get; set; }
        public string ApplyApi { get; set; }
        public List<GenerateNode> ApplyParameters { get; private set; } = new List<GenerateNode>();

        public string ApplyExceptionMessage { get; set; }

        /// <summary>
        /// 取得 ApplyValue 的值或透過 ApplyApi 取得範本
        /// </summary>
        public async Task<string> GetApplyTextAsync()
        {
            if (string.IsNullOrWhiteSpace(ApplyValue) == false)
            {
                return ApplyValue;
            }
            if (string.IsNullOrWhiteSpace(ApplyApi) == false)
            {
                HttpClient client = new HttpClient();
                var message = await client.GetAsync(ApplyApi);
                if (message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return await message.Content.ReadAsStringAsync();
                }
                else
                {
                    ApplyExceptionMessage = $"HttpGet {ApplyApi} Failed.";
                    return null;
                }
            }
            return ApplyValue;
        }

        public GenerateNode() { }
        public GenerateNode(string key)
        {
            ApplyKey = key;
        }
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

    public class RequestNode
    {
        public RequestFrom From { get; set; } = RequestFrom.HttpRequest;
        public string HttpRequestKey { get; set; }
        public string AdapterName { get; set; }
        public string AdapterPropertyName { get; set; }
        public string TemplateUrl { get; set; }

        public Dictionary<string, JToken> HttpRequest { get; set; }
        public Dictionary<string, RequestAdapterNode> AdapterNodes { get; set; }
        public Dictionary<string, RequestNode> TemplateRequestNodes { get; set; }
        public RequestComplex Complex { get; set; }

        public RequestNode() { }
        public RequestNode(string key)
        {
            HttpRequestKey = key;
        }
        public RequestNode(string adapter, string property)
        {
            From = RequestFrom.Adapter;
            AdapterName = adapter;
            AdapterPropertyName = property;
        }
    }

    public class RequestComplex
    {
        public Dictionary<string, JToken> Adapters { get; set; }
        public Dictionary<string, IEnumerable<RequestNode>> TemplateRequestNodes { get; set; }
    }

    public class RequestAdapterNode
    {
        public string Url { get; set; }
        public Dictionary<string, RequestNode> RequestNodes { get; set; }
        public RequestAdapterType Type { get; set; }
    }

    public enum RequestFrom
    {
        /// <summary>
        /// 請求
        /// </summary>
        HttpRequest,
        /// <summary>
        /// 中繼資料
        /// </summary>
        Adapter,
        /// <summary>
        /// 樣板
        /// </summary>
        Template
    }

    public enum RequestAdapterType
    {
        /// <summary>
        /// 統一
        /// </summary>
        Unification,
        /// <summary>
        /// 分離
        /// </summary>
        Separation
    }
}
