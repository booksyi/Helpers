using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace HelpersForCore
{
    public static class CodingExtension
    {
        public static GenerateNode Add(this List<GenerateNode> nodes, string key, string value)
        {
            GenerateNode newNode = new GenerateNode(key, value);
            nodes.Add(newNode);
            return newNode;
        }

        public static void Add(this List<GenerateNode> nodes, string key, IEnumerable<string> values)
        {
            if (values != null)
            {
                foreach (string value in values)
                {
                    nodes.Add(new GenerateNode(key, value));
                }
            }
        }

        public static GenerateNode AppendChild(this GenerateNode node, GenerateNode child)
        {
            node.ApplyParameters.Add(child);
            return child;
        }

        public static GenerateNode AppendChild(this GenerateNode node, string key, string value)
        {
            GenerateNode child = new GenerateNode(key, value);
            node.ApplyParameters.Add(child);
            return child;
        }

        public static void AppendChild(this GenerateNode node, string key, IEnumerable<string> values)
        {
            if (values != null)
            {
                foreach (string value in values)
                {
                    node.ApplyParameters.Add(new GenerateNode(key, value));
                }
            }
        }

        /// <summary>
        /// 取得 ApplyValue 的值或透過 ApplyApi 取得範本
        /// </summary>
        public static async Task<string> ReadAsStringAsync(this GenerateNode node)
        {
            if (string.IsNullOrWhiteSpace(node.ApplyValue) == false)
            {
                return node.ApplyValue;
            }
            if (string.IsNullOrWhiteSpace(node.ApplyApi) == false)
            {
                HttpClient client = new HttpClient();
                var message = await client.GetAsync(node.ApplyApi);
                if (message.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return await message.Content.ReadAsStringAsync();
                }
                throw new GenerateException($"HttpGet {node.ApplyApi} Failed ({message.StatusCode}).");
            }
            return node.ApplyValue;
        }

        public static Dictionary<string, string> GenerateSynonyms = new Dictionary<string, string>
        {
            { "@@Space", " " },
            { "@@FullSpace", "　" },
            { "@@EndLine", "\r\n" },
            { "@@Tab", "\t" },
            { "@@At", "@" }
        };

        /// <summary>
        /// 以 GenerateNode 生成代碼
        /// </summary>
        public static async Task<string> GenerateAsync(this GenerateNode root)
        {
            string result = await root.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(result))
            {
                return null;
            }

            string paramLeft = root.Settings?.ParamLeft ?? "{{#";       // 參數起始的保留字
            string paramRight = root.Settings?.ParamRight ?? "}}";      // 參數結束的保留字
            string eachSeparator = root.Settings?.EachSeparator ?? ","; // 參數設定分隔符的保留字
            string withDefault = root.Settings?.WithDefault ?? "|";     // 參數設定預設值的保留字

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
                        if (root.ApplyParameters.Any(x => x.ApplyKey == key))
                        {
                            value = string.Join(
                                separator,
                                await Task.WhenAll(
                                    root.ApplyParameters
                                        .Where(x => x.ApplyKey == key)
                                        .Select(async node =>
                                        {
                                            if (string.IsNullOrWhiteSpace(node.ApplyApi) == false)
                                            {
                                                return await GenerateAsync(node);
                                            }
                                            return await node.ReadAsStringAsync();
                                        })));
                        }
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            value = @default;
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
        /// 將 GenerateNode 轉換成容易閱讀的樹狀結構
        /// </summary>
        public static async Task<JToken> ToJTokenForReadAsync(this GenerateNode node)
        {
            if (string.IsNullOrWhiteSpace(node.ApplyApi))
            {
                return JToken.FromObject(await node.ReadAsStringAsync());
            }
            else
            {
                JObject jObject = new JObject();
                if (node.ApplyParameters != null && node.ApplyParameters.Any())
                {
                    foreach (var groupParameters in node.ApplyParameters.GroupBy(x => x.ApplyKey))
                    {
                        if (groupParameters != null && groupParameters.Any())
                        {
                            if (groupParameters.Count() == 1)
                            {
                                if (string.IsNullOrWhiteSpace(node.ApplyApi))
                                {
                                    jObject.Add(
                                        groupParameters.Key,
                                        JToken.FromObject(
                                            await groupParameters.First().ReadAsStringAsync()));
                                }
                                else
                                {
                                    jObject.Add(
                                        groupParameters.Key,
                                        await groupParameters.First().ToJTokenForReadAsync());
                                }
                            }
                            else
                            {
                                JArray jArray = new JArray();
                                foreach (var parameter in groupParameters)
                                {
                                    jArray.Add(await parameter.ToJTokenForReadAsync());
                                }
                                jObject.Add(groupParameters.Key, jArray);
                            }
                        }
                    }
                }
                return new JObject { { node.ApplyApi, jObject } };
            }
        }

        /// <summary>
        /// 將 RequestNode 轉成 JToken
        /// </summary>
        private static JToken ToJToken(this CodeTemplate.TransactionRequestNode requestNode, JObject input, JObject adapter)
        {
            if (requestNode.From == CodeTemplate.RequestFrom.Value)
            {
                return requestNode.Value;
            }
            else if (requestNode.From == CodeTemplate.RequestFrom.Input)
            {
                if (string.IsNullOrWhiteSpace(requestNode.InputName))
                {
                    throw new GenerateException("RequestNode.From 為 Input 時必須有 InputName");
                }
                // 處理 InputName 以 | 區隔，來依序尋找不為 null 或 Empty 的值
                string[] inputNames = requestNode.InputName.Split('|').Select(x => x.Trim()).ToArray();
                foreach (string inputName in inputNames)
                {
                    JToken jToken = input;
                    string[] inputPropertyNames = inputName.Split('.');
                    foreach (var inputPropertyName in inputPropertyNames)
                    {
                        jToken = (jToken as JObject ?? input)?.Property(inputPropertyName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                    }
                    if (string.IsNullOrWhiteSpace(Convert.ToString(jToken)) == false)
                    {
                        return jToken;
                    }
                }
                return null;
            }
            else if (requestNode.From == CodeTemplate.RequestFrom.Adapter)
            {
                if (string.IsNullOrWhiteSpace(requestNode.AdapterName))
                {
                    throw new GenerateException("RequestNode.From 為 Adapter 時必須有 AdapterName");
                }
                // 處理 AdapterProperty 以 . 區隔，來向下層尋找成員
                JToken jToken = adapter?.Property(requestNode.AdapterName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                if (!string.IsNullOrWhiteSpace(requestNode.AdapterProperty) && jToken is JObject jObject)
                {
                    string[] propertyNames = requestNode.AdapterProperty.Split('.');
                    foreach (var propertyName in propertyNames)
                    {
                        jToken = (jToken as JObject)?.Property(propertyName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                    }
                }
                return jToken;
            }
            return null;
        }

        /// <summary>
        /// 依照 AdapterNode 的設定透過 Http 取得 Api 的結果並以 JToken 的型態回傳
        /// </summary>
        private static async Task<JToken> ToJTokenAsync(this CodeTemplate.TransactionAdapterNode adapterNode, JObject input, JObject adapter)
        {
            HttpClient client = new HttpClient();
            string json = null;
            if (adapterNode.HttpMethod == CodeTemplate.HttpMethod.Get)
            {
                string url = adapterNode.Url;
                if (adapterNode.RequestNodes.NotNullAny())
                {
                    string queryString = adapterNode.RequestNodes
                        .ToDictionary(x => x.Name, x => x.ToJToken(input, adapter))
                        .ToQueryString();
                    url = url.AppendQueryString(queryString);
                }
                json = await client.GetStringAsync(url);
            }
            else if (adapterNode.HttpMethod == CodeTemplate.HttpMethod.Post)
            {
                JToken postData;
                if (adapterNode.RequestNodes != null
                    && adapterNode.RequestNodes.Count() == 1
                    && string.IsNullOrWhiteSpace(adapterNode.RequestNodes.First().Name))
                {
                    // 當 Adapter 只有一個名稱為空白的 RequestNode 時，將 RequestNode 的結果當作 Post 的資料
                    postData = adapterNode.RequestNodes.First().ToJToken(input, adapter);
                }
                else
                {
                    postData = adapterNode.RequestNodes?
                        .ToDictionary(x => x.Name, x => x.ToJToken(input, adapter))
                        .ToJObject();
                }
                var response = await client.PostAsJsonAsync(
                    adapterNode.Url, postData);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    json = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new GenerateException($"AdapterNode Post 到 {adapterNode.Url} 的回傳結果不成功，Post Data = {postData}");
                }
            }
            JToken jToken;
            try
            {
                jToken = JToken.Parse(json);
            }
            catch (Exception)
            {
                throw new GenerateException($"AdapterNode Post 到 {adapterNode.Url} 的回傳結果 {json} 無法反序列化");
            }
            if (jToken is JObject jObject)
            {
                return jObject.Confine(adapterNode.ResponseConfine);
            }
            return jToken;
        }
        /// <summary>
        /// 將 TemplateNode 轉成 Url
        /// </summary>
        private static string ToUrl(this CodeTemplate.TransactionTemplateNode templateNode, JObject input)
        {
            string url = templateNode.Url;
            if (templateNode.RequestNodes.NotNullAny())
            {
                string queryString = templateNode.RequestNodes
                    .ToDictionary(x => x.Name, x => x.ToJToken(input, templateNode.TransactionAdapter))
                    .ToQueryString();
                url = url.AppendQueryString(queryString);
            }
            return url;
        }

        #region Validate CodeTemplate
        /// <summary>
        /// 檢查 CodeTemplate 是否合法，並回傳簡化後的物件
        /// </summary>
        public static bool Validate(this CodeTemplate template, out string[] errorMessages)
        {
            List<string> errors = new List<string>();
            if (template.Inputs != null)
            {
                if (template.Inputs.Any())
                {
                    if (template.Inputs.Validate(out string[] outErrorMessages) == false)
                    {
                        errors.AddRange(outErrorMessages);
                    }
                }
                else
                {
                    template.Inputs = null;
                }
            }

            if (template.TemplateNodes != null && template.TemplateNodes.Any())
            {
                foreach (var templateNode in template.TemplateNodes)
                {
                    if (templateNode.Validate(out string[] outErrorMessages) == false)
                    {
                        errors.AddRange(outErrorMessages);
                    }
                }
            }
            else
            {
                errors.Add($"至少需要一個 TemplateNode");
            }

            errorMessages = errors.ToArray();
            return errorMessages.Length == 0;
        }

        private static bool Validate(this CodeTemplate.Input[] inputs, out string[] errorMessages)
        {
            List<string> errors = new List<string>();
            List<string> temp = new List<string>();
            foreach (var input in inputs)
            {
                input.Name = input.Name.Trim();
                if (temp.Contains(input.Name))
                {
                    errors.Add($"Input 名稱重複: {input.Name}");
                }
                if (input.InputType == CodeTemplate.InputType.TrueFalse)
                {
                    // TrueFalse 不允許多值
                    input.IsMultiple = false;
                }
                if (input.Children != null)
                {
                    if (input.Children.Any())
                    {
                        if (input.Children.Validate(out string[] outErrorMessages) == false)
                        {
                            errors.AddRange(outErrorMessages);
                        }
                    }
                    else
                    {
                        input.Children = null;
                    }
                }
                temp.Add(input.Name);
            }
            errorMessages = errors.ToArray();
            return errorMessages.Length == 0;
        }
        private static bool Validate(this CodeTemplate.InputChild[] inputs, out string[] errorMessages)
        {
            CodeTemplate.Input[] args = inputs.JsonConvertTo<CodeTemplate.Input[]>();
            return args.Validate(out errorMessages);
        }
        private static bool Validate(this CodeTemplate.TemplateNode templateNode, out string[] errorMessages)
        {
            List<string> errors = new List<string>();
            if (templateNode.RequestNodes.Validate(out string[] outErrorMessages) == false)
            {
                errors.AddRange(outErrorMessages);
            }
            if (templateNode.AdapterNodes.Validate(out outErrorMessages) == false)
            {
                errors.AddRange(outErrorMessages);
            }
            if (templateNode.ParameterNodes.Validate(out outErrorMessages) == false)
            {
                errors.AddRange(outErrorMessages);
            }
            errorMessages = errors.ToArray();
            return errorMessages.Length == 0;
        }
        private static bool Validate(this CodeTemplate.RequestNode[] requestNodes, out string[] errorMessages)
        {
            List<string> errors = new List<string>();
            List<string> temp = new List<string>();
            foreach (var requestNode in requestNodes)
            {
                requestNode.Name = requestNode.Name.Trim();
                if (temp.Contains(requestNode.Name))
                {
                    errors.Add($"Request 名稱重複: {requestNode.Name}");
                }
                temp.Add(requestNode.Name);
            }
            errorMessages = errors.ToArray();
            return errorMessages.Length == 0;
        }
        private static bool Validate(this CodeTemplate.AdapterNode[] adapterNodes, out string[] errorMessages)
        {
            List<string> errors = new List<string>();
            List<string> temp = new List<string>();
            foreach (var adapterNode in adapterNodes)
            {
                adapterNode.Name = adapterNode.Name.Trim();
                if (temp.Contains(adapterNode.Name))
                {
                    errors.Add($"Adapter 名稱重複: {adapterNode.Name}");
                }
                temp.Add(adapterNode.Name);
            }
            errorMessages = errors.ToArray();
            return errorMessages.Length == 0;
        }
        private static bool Validate(this CodeTemplate.ParameterNode[] parameterNodes, out string[] errorMessages)
        {
            List<string> errors = new List<string>();
            List<string> temp = new List<string>();
            foreach (var parameterNode in parameterNodes)
            {
                parameterNode.Name = parameterNode.Name.Trim();
                if (temp.Contains(parameterNode.Name))
                {
                    errors.Add($"Parameter 名稱重複: {parameterNode.Name}");
                }
                if (parameterNode.TemplateNode.Validate(out string[] outErrorMessages) == false)
                {
                    errors.AddRange(outErrorMessages);
                }
                temp.Add(parameterNode.Name);
            }
            errorMessages = errors.ToArray();
            return errorMessages.Length == 0;
        }
        #endregion

        public static async Task<GenerateNode[]> ToGenerateNodesAsync(this CodeTemplate template, JObject request)
        {
            // Split Inputs
            List<JObject> inputObjects = new List<JObject> { new JObject() };
            if (template.Inputs != null && template.Inputs.Any())
            {
                foreach (var input in template.Inputs)
                {
                    JToken jToken = request?.Property(input.Name, StringComparison.CurrentCultureIgnoreCase)?.Value;
                    List<JObject> tempInputObjects = new List<JObject>();
                    foreach (var inputObject in inputObjects)
                    {
                        if (input.IsSplit && jToken is JArray jArray)
                        {
                            foreach (JToken arrayToken in jArray)
                            {
                                var cloneInputObject = JObject.Parse(inputObject.ToString());
                                cloneInputObject.Add(input.Name, arrayToken);
                                tempInputObjects.Add(cloneInputObject);
                            }
                        }
                        else
                        {
                            inputObject.Add(input.Name, jToken);
                            tempInputObjects.Add(inputObject);
                        }
                    }
                    inputObjects = tempInputObjects;
                }
            }

            List<GenerateNode> generateNodes = new List<GenerateNode>();
            foreach (var inputObject in inputObjects)
            {
                // 依照 AdapterNodes 的設定, 生成 TransactionAdapter
                List<CodeTemplate.TransactionTemplateNode> transactionTemplateNodes = new List<CodeTemplate.TransactionTemplateNode>();
                foreach (var templateNode in template.TemplateNodes)
                {
                    CodeTemplate.TransactionTemplateNode transactionTemplateNode = templateNode.JsonConvertTo<CodeTemplate.TransactionTemplateNode>();
                    transactionTemplateNodes.AddRange(await transactionTemplateNode.GenerateTransactionAdapter(inputObject));
                }

                // 依照 ParameterNodes 的設定, 生成可直接串接的 TransactionParameterNodes
                foreach (CodeTemplate.TransactionTemplateNode transactionTemplateNode in transactionTemplateNodes)
                {
                    await transactionTemplateNode.GenerateTransactionParameterNodes(inputObject);
                }

                // 將 TransactionTemplateNode 轉換成 GenerateNode
                foreach (CodeTemplate.TransactionTemplateNode transactionTemplateNode in transactionTemplateNodes)
                {
                    generateNodes.AddRange(await transactionTemplateNode.ToGenerateNodesAsync(inputObject));
                }
            }
            return generateNodes.ToArray();
        }

        private static async Task<GenerateNode[]> ToGenerateNodesAsync(this CodeTemplate.TransactionTemplateNode templateNode, JObject input)
        {
            GenerateNode generateNode = new GenerateNode();
            generateNode.ApplyKey = templateNode.Name;
            generateNode.ApplyApi = templateNode.ToUrl(input);
            if (templateNode.TransactionParameterNodes != null)
            {
                foreach (var templateParameterNode in templateNode.TransactionParameterNodes)
                {
                    var children = await templateParameterNode.ToGenerateNodesAsync(input, templateNode.TransactionAdapter);
                    foreach (var child in children)
                    {
                        generateNode.AppendChild(child).ChangeKey(templateParameterNode.Name);
                    }
                }
            }
            return new GenerateNode[] { generateNode };
        }

        private static async Task<GenerateNode[]> ToGenerateNodesAsync(this CodeTemplate.TransactionParameterNode parameterNode, JObject input, JObject adapter)
        {
            JToken jToken = null;
            if (parameterNode.From == CodeTemplate.ParameterFrom.Value)
            {
                jToken = parameterNode.Value;
            }
            else if (parameterNode.From == CodeTemplate.ParameterFrom.Input)
            {
                // 處理 InputName 以 | 區隔，來依序尋找不為 null 或 Empty 的值
                string[] inputNames = parameterNode.InputName.Split('|').Select(x => x.Trim()).ToArray();
                foreach (string inputName in inputNames)
                {
                    string[] inputPropertyNames = inputName.Split('.');
                    foreach (var inputPropertyName in inputPropertyNames)
                    {
                        jToken = (jToken as JObject ?? input)?.Property(inputPropertyName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                    }
                    if (string.IsNullOrWhiteSpace(Convert.ToString(jToken)) == false)
                    {
                        break;
                    }
                }
            }
            else if (parameterNode.From == CodeTemplate.ParameterFrom.Adapter)
            {
                // 處理 AdapterProperty 以 . 區隔，來向下層尋找成員
                jToken = adapter?.Property(parameterNode.AdapterName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                if (!string.IsNullOrWhiteSpace(parameterNode.AdapterProperty) && jToken is JObject jObject)
                {
                    string[] propertyNames = parameterNode.AdapterProperty.Split('.');
                    foreach (var propertyName in propertyNames)
                    {
                        jToken = (jToken as JObject)?.Property(propertyName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                    }
                }
            }
            else if (parameterNode.From == CodeTemplate.ParameterFrom.Template)
            {
                return await parameterNode.TemplateNode.ToGenerateNodesAsync(input);
            }
            if (jToken != null)
            {
                if (jToken is JArray jArray)
                {
                    List<GenerateNode> generateNodes = new List<GenerateNode>();
                    foreach (JToken arrayToken in jArray)
                    {
                        generateNodes.Add(new GenerateNode()
                        {
                            ApplyValue = Convert.ToString(arrayToken)
                        });
                    }
                    return generateNodes.ToArray();
                }
                else
                {
                    return new GenerateNode[]
                    {
                        new GenerateNode()
                        {
                            ApplyValue = Convert.ToString(jToken)
                        }
                    };
                }
            }
            return new GenerateNode[0];
        }

        /// <summary>
        /// 依照 AdapterNodes 生成 TransactionAdapter
        /// </summary>
        private static async Task<CodeTemplate.TransactionTemplateNode[]> GenerateTransactionAdapter(this CodeTemplate.TransactionTemplateNode templateNode, JObject input)
        {
            if (templateNode.AdapterNodes != null && templateNode.AdapterNodes.Any())
            {
                templateNode.NewPropertyIfNull(x => x.TransactionAdapter);
                // 因為生成 TransactionAdapter 會以遞迴方式處理, 所以可能會已經有部分的 AdapterNode 處理過了
                string[] adapterNames = templateNode.TransactionAdapter.Properties().Select(x => x.Name).ToArray();
                // 只處理未處理過的 AdapterNode
                CodeTemplate.TransactionAdapterNode[] adapterNodes = templateNode.AdapterNodes.Where(x => adapterNames.Contains(x.Name) == false).ToArray();
                foreach (var adapterNode in adapterNodes)
                {
                    JToken jToken = await adapterNode.ToJTokenAsync(input, templateNode.TransactionAdapter);
                    if (adapterNode.IsSplit)
                    {
                        // 如果 IsSplit == true,
                        // 若回傳的結果為 Object 且只有一個成員,
                        // 則向下尋訪該成員的值是否為 Array
                        JObject adapterObject = new JObject();
                        JObject adapterPropertyObject = adapterObject;
                        JProperty adapterProperty = null;
                        while (jToken is JObject jObject && jObject.Properties().Count() == 1)
                        {
                            JProperty jProperty = jObject.Properties().First();
                            adapterPropertyObject.Add(jProperty.Name, new JObject());
                            adapterProperty = adapterPropertyObject.Property(jProperty.Name);
                            adapterPropertyObject = adapterProperty.Value as JObject;
                            jToken = jProperty.Value;
                        }
                        if (jToken is JArray jArray)
                        {
                            List<CodeTemplate.TransactionTemplateNode> transactionTemplateNodes = new List<CodeTemplate.TransactionTemplateNode>();
                            // adapterProperty != null 代表有向下尋訪,
                            // TransactionAdapter 的值要以最上層的 adapterObject 為主
                            if (adapterProperty != null)
                            {
                                foreach (JToken arrayToken in jArray)
                                {
                                    adapterProperty.Value = arrayToken;
                                    var cloneAdapterObject = JObject.Parse(adapterObject.ToString());
                                    var cloneTemplateNode = templateNode.JsonConvertTo<CodeTemplate.TransactionTemplateNode>();
                                    cloneTemplateNode.TransactionAdapter.Add(adapterNode.Name, cloneAdapterObject);
                                    transactionTemplateNodes.AddRange(await cloneTemplateNode.GenerateTransactionAdapter(input));
                                }
                            }
                            else
                            {
                                foreach (JToken arrayToken in jArray)
                                {
                                    var cloneTemplateNode = templateNode.JsonConvertTo<CodeTemplate.TransactionTemplateNode>();
                                    cloneTemplateNode.TransactionAdapter.Add(adapterNode.Name, arrayToken);
                                    transactionTemplateNodes.AddRange(await cloneTemplateNode.GenerateTransactionAdapter(input));
                                }
                            }
                            return transactionTemplateNodes.ToArray();
                        }
                        else if (adapterProperty != null)
                        {
                            // 如果 IsSplit == true, 但是資料並非 Array 則將 jToken 還原
                            adapterProperty.Value = jToken;
                            jToken = adapterObject;
                        }
                    }
                    templateNode.TransactionAdapter.Add(adapterNode.Name, jToken);
                }
            }
            return new CodeTemplate.TransactionTemplateNode[] { templateNode };
        }

        /// <summary>
        /// 依照 ParameterNodes 生成 TransactionParameterNodes
        /// </summary>
        private static async Task GenerateTransactionParameterNodes(this CodeTemplate.TransactionTemplateNode templateNode, JObject input)
        {
            if (templateNode.ParameterNodes != null && templateNode.ParameterNodes.Any())
            {
                var transactionParameterNodes = new List<CodeTemplate.TransactionParameterNode>();
                foreach (var parameterNode in templateNode.ParameterNodes)
                {
                    if (parameterNode != null)
                    {
                        if (parameterNode.From == CodeTemplate.ParameterFrom.Template)
                        {
                            // 把 TransactionAdapter 丟給 ParameterNode.TemplateNode 的 TransactionAdapter
                            parameterNode.TemplateNode.TransactionAdapter = templateNode.TransactionAdapter;
                            var transactionTemplateNodes = await parameterNode.TemplateNode.GenerateTransactionAdapter(input);
                            foreach (var transactionTemplateNode in transactionTemplateNodes)
                            {
                                var cloneParameterNode = parameterNode.JsonConvertTo<CodeTemplate.TransactionParameterNode>();
                                cloneParameterNode.TemplateNode = transactionTemplateNode;
                                await cloneParameterNode.TemplateNode.GenerateTransactionParameterNodes(input);
                                transactionParameterNodes.Add(cloneParameterNode);
                            }
                        }
                        else
                        {
                            transactionParameterNodes.Add(parameterNode);
                        }
                    }
                }
                templateNode.TransactionParameterNodes = transactionParameterNodes.ToArray();
            }
        }

        /// <summary>
        /// 將 JObject 限縮在指定成員
        /// </summary>
        private static JObject Confine(this JObject source, string propertyConfine)
        {
            if (string.IsNullOrWhiteSpace(propertyConfine) == false)
            {
                JToken jToken = source;
                JObject adapterObject = new JObject();
                JObject adapterPropertyObject = adapterObject;
                JProperty adapterProperty = null;
                string[] confines = propertyConfine.Split(".");
                foreach (string confine in confines)
                {
                    if (jToken is JObject jObject)
                    {
                        JProperty jProperty = jObject.Property(confine, StringComparison.CurrentCultureIgnoreCase);
                        if (jProperty == null)
                        {
                            throw new GenerateException($"物件 {source} 限縮到 {propertyConfine} 時失敗，無法找到成員 {confine}");
                        }
                        adapterPropertyObject.Add(jProperty.Name, new JObject());
                        adapterProperty = adapterPropertyObject.Property(jProperty.Name);
                        adapterPropertyObject = adapterProperty.Value as JObject;
                        jToken = jProperty.Value;
                    }
                    else
                    {
                        break;
                    }
                }
                if (adapterProperty != null)
                {
                    adapterProperty.Value = jToken;
                    return adapterObject;
                }
            }
            return source;
        }

        /// <summary>
        /// 取得 CodeTemplate 所有用到的樣板 API URL
        /// </summary>
        public static string[] GetTemplateUris(this CodeTemplate codeTemplate)
        {
            List<string> templates = new List<string>();
            foreach (var templateNode in codeTemplate.TemplateNodes)
            {
                templates.AddRange(templateNode.GetTemplateUris());
            }
            return templates.Distinct().ToArray();
        }
        private static string[] GetTemplateUris(this CodeTemplate.TemplateNode templateNode)
        {
            List<string> templates = new List<string>();
            templates.Add(templateNode.Url);
            foreach (var parameterNode in templateNode.ParameterNodes)
            {
                if (parameterNode.From == CodeTemplate.ParameterFrom.Template)
                {
                    templates.AddRange(parameterNode.TemplateNode.GetTemplateUris());
                }
            }
            return templates.Distinct().ToArray();
        }
    }
}
