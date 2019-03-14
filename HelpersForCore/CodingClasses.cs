using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HelpersForCore
{
    public class DbSchemaTable
    {
        public string Name { get; set; }
        public DbSchemaField[] Fields { get; set; }

        public DbSchemaField Identity { get => Fields.FirstOrDefault(x => x.IsIdentity); }
        public DbSchemaField[] PrimaryKeys { get => Fields.Where(x => x.IsPrimaryKey).ToArray(); }
    }

    public class DbSchemaField
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
    }

    public class CsSchemaValue
    {
        public string Value { get; set; }
        public bool IsString { get; set; }
        public CsSchemaValue(string value, bool isString = true)
        {
            Value = value;
            IsString = isString;
        }
        public CsSchemaValue(object value, bool isString = false)
        {
            Value = Convert.ToString(value);
            IsString = isString;
        }
    }

    public class CsSchemaAttribute
    {
        public string Name { get; set; }
        public Dictionary<string, CsSchemaValue> ConstructorParameters { get; set; }
        public Dictionary<string, CsSchemaValue> Properties { get; set; }
        public CsSchemaAttribute(string name)
        {
            Name = name;
        }
    }

    public class CsSchemaClass
    {
        public string Name { get; set; }
        public CsSchemaAttribute[] Attributes { get; set; }
        public CsSchemaProperty[] Properties { get; set; }
    }

    public class CsSchemaProperty
    {
        public CsSchemaAttribute[] Attributes { get; set; }
        public string TypeName { get; set; }
        public string Name { get; set; }
    }

    public class TsSchemaClass
    {
        public string Name { get; set; }
        public TsSchemaProperty[] Properties { get; set; }
    }

    public class TsSchemaProperty
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
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
            public string DisplayName { get; set; }

            public InputType InputType { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool IsRequired { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool IsMultiple { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool IsSplit { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string[] DefaultValues { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public InputChild[] Children { get; set; }

            public Input() { }
            public Input(string name)
            {
                Name = name;
            }

            public Input HasDisplayName(string displayName)
            {
                DisplayName = displayName;
                return this;
            }

            public Input HasDefaultValues(params string[] defaultValues)
            {
                DefaultValues = defaultValues;
                return this;
            }

            public Input Required(bool isRequired = true)
            {
                IsRequired = isRequired;
                return this;
            }

            public Input Multiple(bool isMultiple = true)
            {
                IsMultiple = isMultiple;
                return this;
            }

            public Input Split(bool isSplit = true)
            {
                IsSplit = isSplit;
                return this;
            }
        }

        public class InputChild
        {
            public string Name { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string DisplayName { get; set; }

            public InputType InputType { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool IsRequired { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool IsMultiple { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string[] DefaultValues { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public InputChild[] Children { get; set; }

            public InputChild() { }
            public InputChild(string name)
            {
                Name = name;
            }

            public InputChild HasDisplayName(string displayName)
            {
                DisplayName = displayName;
                return this;
            }

            public InputChild HasDefaultValues(params string[] defaultValues)
            {
                DefaultValues = defaultValues;
                return this;
            }

            public InputChild Required(bool isRequired = true)
            {
                IsRequired = isRequired;
                return this;
            }

            public InputChild Multiple(bool isMultiple = true)
            {
                IsMultiple = isMultiple;
                return this;
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
            public bool IsSplit { get; set; }

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
            public string Name { get; set; }

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
            public bool IsSplit { get; set; }
        }

        public class TransactionTemplateNode
        {
            public string Name { get; set; }

            public string Url { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TransactionRequestNode[] RequestNodes { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TransactionAdapterNode[] AdapterNodes { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TransactionParameterNode[] ParameterNodes { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public JObject TransactionAdapter { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TransactionParameterNode[] TransactionParameterNodes { get; set; }
        }
        #endregion

        [JsonConverter(typeof(StringEnumConverter))]
        public enum InputType
        {
            [EnumMember(Value = "textbox")]
            TextBox,

            [EnumMember(Value = "textarea")]
            TextArea,

            [EnumMember(Value = "truefalse")]
            TrueFalse,


        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum RequestFrom
        {
            /// <summary>
            /// 固定值
            /// </summary>
            [EnumMember(Value = "value")]
            Value,
            /// <summary>
            /// 輸入參數
            /// </summary>
            [EnumMember(Value = "input")]
            Input,
            /// <summary>
            /// 中繼資料
            /// </summary>
            [EnumMember(Value = "adapter")]
            Adapter
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum ParameterFrom
        {
            /// <summary>
            /// 固定值
            /// </summary>
            [EnumMember(Value = "value")]
            Value,
            /// <summary>
            /// 輸入參數
            /// </summary>
            [EnumMember(Value = "input")]
            Input,
            /// <summary>
            /// 中繼資料
            /// </summary>
            [EnumMember(Value = "adapter")]
            Adapter,
            /// <summary>
            /// 樣板
            /// </summary>
            [EnumMember(Value = "template")]
            Template
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum HttpMethod
        {
            [EnumMember(Value = "get")]
            Get,

            [EnumMember(Value = "post")]
            Post
        }
    }
}
