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
        public GenerateNodeSettings Settings { get; set; }

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
                    ApplyExceptionMessage = $"HttpGet {ApplyApi} Failed ({message.StatusCode}).";
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

    public class GenerateNodeSettings
    {
        /// <summary>
        /// 參數起始的保留字
        /// </summary>
        public string ParamLeft { get; set; }

        /// <summary>
        /// 參數結束的保留字
        /// </summary>
        public string ParamRight { get; set; }

        /// <summary>
        /// 參數設定分隔符的保留字
        /// </summary>
        public string EachSeparator { get; set; }

        /// <summary>
        /// 參數設定預設值的保留字
        /// </summary>
        public string WithDefault { get; set; }
    }

    public class CodeTemplate
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Input[] Inputs { get; set; }

        public TemplateNode[] TemplateNodes { get; set; }

        public class Input
        {
            public string Name { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string DefaultValue { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Description { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool IsJson { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool Split { get; set; }

            public Input() { }
            public Input(string name)
            {
                Name = name;
            }
        }

        public class RequestNode
        {
            public string Name { get; set; }
            public RequestFrom From { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string InputName { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string InputProperty { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string HttpRequestDescription { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Value { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AdapterName { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AdapterProperty { get; set; }

            public RequestNode() { }
            public RequestNode(string name)
            {
                Name = name;
            }

            public RequestNode FromValue(string value)
            {
                From = RequestFrom.Value;
                Value = value;
                return this;
            }

            public RequestNode FromInput(string inputName, string inputProperty = null)
            {
                From = RequestFrom.Input;
                InputName = inputName;
                InputProperty = inputProperty;
                return this;
            }

            public RequestNode FromAdapter(string adapterName, string adapterProperty = null)
            {
                From = RequestFrom.Adapter;
                AdapterName = adapterName;
                AdapterProperty = adapterProperty;
                return this;
            }
        }

        public class ParameterNode
        {
            public string Name { get; set; }
            public ParameterFrom From { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Value { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string InputName { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string InputProperty { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string HttpRequestDescription { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AdapterName { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AdapterProperty { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TemplateNode TemplateNode { get; set; }

            public ParameterNode() { }
            public ParameterNode(string name)
            {
                Name = name;
            }

            public ParameterNode FromValue(string value)
            {
                From = ParameterFrom.Value;
                Value = value;
                return this;
            }

            public ParameterNode FromInput(string inputName, string inputProperty = null)
            {
                From = ParameterFrom.Input;
                InputName = inputName;
                InputProperty = inputProperty;
                return this;
            }

            public ParameterNode FromAdapter(string adapterName, string adapterProperty = null)
            {
                From = ParameterFrom.Adapter;
                AdapterName = adapterName;
                AdapterProperty = adapterProperty;
                return this;
            }
            
            public ParameterNode FromTemplate(TemplateNode template)
            {
                From = ParameterFrom.Template;
                TemplateNode = template;
                return this;
            }
        }

        public class AdapterNode
        {
            public string Name { get; set; }

            public HttpMethod HttpMethod { get; set; }
            public string Url { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public RequestNode[] RequestNodes { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string ResponseConfine { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool ResponseSplit { get; set; }

            public AdapterNode() { }
            public AdapterNode(string name, string url, HttpMethod httpMethod = HttpMethod.Get)
            {
                Name = name;
                Url = url;
                HttpMethod = httpMethod;
            }
        }

        public class TemplateNode
        {
            public string Url { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public RequestNode[] RequestNodes { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public AdapterNode[] AdapterNodes { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public ParameterNode[] ParameterNodes { get; set; }

            public TemplateNode() { }
            public TemplateNode(string url)
            {
                Url = url;
            }
        }

        #region Transaction Classes
        public class TransactionRequestNode
        {
            public string Name { get; set; }
            public RequestFrom From { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string InputName { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string InputProperty { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string HttpRequestDescription { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Value { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AdapterName { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AdapterProperty { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, JToken> Adapters { get; set; }
        }

        public class TransactionParameterNode
        {
            public string Name { get; set; }
            public ParameterFrom From { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Value { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string InputName { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string InputProperty { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string HttpRequestDescription { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AdapterName { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AdapterProperty { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TransactionTemplateNode TemplateNode { get; set; }
        }

        public class TransactionAdapterNode
        {
            public string Name { get; set; }

            public HttpMethod HttpMethod { get; set; }
            public string Url { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TransactionRequestNode[] RequestNodes { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string ResponseConfine { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool ResponseSplit { get; set; }
        }

        public class TransactionTemplateNode
        {
            public string Url { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TransactionRequestNode[] RequestNodes { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TransactionAdapterNode[] AdapterNodes { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TransactionParameterNode[] ParameterNodes { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, JToken> Adapters { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TransactionParameterNode[] TransactionParameterNodes { get; set; }
        }
        #endregion

        [JsonConverter(typeof(StringEnumConverter))]
        public enum RequestFrom
        {
            /// <summary>
            /// 固定值
            /// </summary>
            Value,
            /// <summary>
            /// 輸入參數
            /// </summary>
            Input,
            /// <summary>
            /// 中繼資料
            /// </summary>
            Adapter
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum ParameterFrom
        {
            /// <summary>
            /// 固定值
            /// </summary>
            Value,
            /// <summary>
            /// 輸入參數
            /// </summary>
            Input,
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
        public enum HttpMethod
        {
            Get,
            Post
        }
    }
}
