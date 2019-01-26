using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        public IEnumerable<Field> PrimaryKeys { get => Fields.Where(x => x.IsPrimaryKey).ToArray(); }

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
            public string DefaultValue { get; set; }
        }
    }

    public class GenerateNode
    {
        public string ApplyKey { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ApplyValue { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ApplyApi { get; set; }

        public List<GenerateNode> ApplyParameters { get; private set; } = new List<GenerateNode>();

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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

    public class RequestSimpleNode
    {
        public RequestSimpleFrom From { get; set; } = RequestSimpleFrom.HttpRequest;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string HttpRequestKey { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AdapterName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AdapterPropertyName { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> Adapters { get; set; }

        public RequestSimpleNode() { }
        public RequestSimpleNode(string key)
        {
            HttpRequestKey = key;
        }
        public RequestSimpleNode(string adapter, string property)
        {
            From = RequestSimpleFrom.Adapter;
            AdapterName = adapter;
            AdapterPropertyName = property;
        }
    }

    public class RequestNode : RequestSimpleNode
    {
        public new RequestFrom From { get; set; } = RequestFrom.HttpRequest;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ApiNode TemplateNode { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, RequestNode> SimpleTemplateRequestNodes { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, IEnumerable<RequestNode>> ComplexTemplateRequestNodes { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, AdapterNode> AdapterNodes { get; set; }

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

    public class ApiNode
    {
        public string Url { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, RequestSimpleNode> RequestNodes { get; set; }
    }

    public class AdapterNode : ApiNode
    {
        public AdapterHttpMethod HttpMethod { get; set; }
        public AdapterType Type { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ResponseConfine { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RequestSimpleFrom
    {
        /// <summary>
        /// 請求
        /// </summary>
        HttpRequest,
        /// <summary>
        /// 固定值
        /// </summary>
        Value,
        /// <summary>
        /// 中繼資料
        /// </summary>
        Adapter
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RequestFrom
    {
        /// <summary>
        /// 請求
        /// </summary>
        HttpRequest,
        /// <summary>
        /// 固定值
        /// </summary>
        Value,
        /// <summary>
        /// 中繼資料
        /// </summary>
        Adapter,
        /// <summary>
        /// 樣板
        /// </summary>
        Template
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum AdapterType
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

    [JsonConverter(typeof(StringEnumConverter))]
    public enum AdapterHttpMethod
    {
        Get,
        Post
    }
}
