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
            string result = await root.GetApplyTextAsync();
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
                                            return await node.GetApplyTextAsync();
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
                return JToken.FromObject(await node.GetApplyTextAsync());
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
                                            await groupParameters.First().GetApplyTextAsync()));
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
        public static JToken ToJToken(this CodeTemplate.TransactionRequestNode requestNode, JObject request, JObject adapter)
        {
            if (requestNode.From == CodeTemplate.RequestFrom.Value)
            {
                return requestNode.Value;
            }
            else if (requestNode.From == CodeTemplate.RequestFrom.Input)
            {
                // 處理 InputName 以 | 區隔，來依序尋找不為 null 或 Empty 的值
                string[] inputNames = requestNode.InputName.Split('|').Select(x => x.Trim()).ToArray();
                foreach (string inputName in inputNames)
                {
                    JToken jToken = request?.Property(inputName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                    if (string.IsNullOrWhiteSpace(Convert.ToString(jToken)) == false)
                    {
                        return jToken;
                    }
                }
                return null;
            }
            else if (requestNode.From == CodeTemplate.RequestFrom.Adapter)
            {
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
        /// 將 TemplateNode 轉成 Url
        /// </summary>
        public static string ToUrl(this CodeTemplate.TransactionTemplateNode templateNode, JObject request)
        {
            string url = templateNode.Url;
            if (templateNode.RequestNodes.NotNullAny())
            {
                string queryString = templateNode.RequestNodes
                    .ToDictionary(x => x.Name, x => x.ToJToken(request, templateNode.TransactionAdapter))
                    .ToQueryString();
                url = url.AppendQueryString(queryString);
            }
            return url;
        }
        /// <summary>
        /// 依照 AdapterNode 的設定透過 Http 取得 Api 的結果並以 JToken 的型態回傳
        /// </summary>
        public static async Task<JToken> ToJTokenAsync(this CodeTemplate.TransactionAdapterNode adapterNode, JObject request, JObject adapter)
        {
            HttpClient client = new HttpClient();
            string json = null;
            if (adapterNode.HttpMethod == CodeTemplate.HttpMethod.Get)
            {
                string url = adapterNode.Url;
                if (adapterNode.RequestNodes.NotNullAny())
                {
                    string queryString = adapterNode.RequestNodes
                        .ToDictionary(x => x.Name, x => x.ToJToken(request, adapter))
                        .ToQueryString();
                    url = url.AppendQueryString(queryString);
                }
                json = await client.GetStringAsync(url);
            }
            else if (adapterNode.HttpMethod == CodeTemplate.HttpMethod.Post)
            {
                var response = await client.PostAsJsonAsync(
                    adapterNode.Url,
                    adapterNode.RequestNodes?
                        .ToDictionary(x => x.Name, x => x.ToJToken(request, adapter))
                        .ToJObject(request));
                json = await response.Content.ReadAsStringAsync();
            }
            JToken jToken = CSharpHelper.Try(() => JToken.Parse(json), x => x, () => json);
            if (jToken is JObject jObject)
            {
                return jObject.Confine(adapterNode.ResponseConfine);
            }
            return jToken;
        }

        public static async Task<IEnumerable<GenerateNode>> ToGenerateNodesAsync(this CodeTemplate template, JObject request)
        {
            // 依照 AdapterNodes 的設定, 生成 TransactionAdapter
            List<CodeTemplate.TransactionTemplateNode> transactionTemplateNodes = new List<CodeTemplate.TransactionTemplateNode>();
            foreach (var templateNode in template.TemplateNodes)
            {
                CodeTemplate.TransactionTemplateNode transactionTemplateNode = templateNode.JsonConvertTo<CodeTemplate.TransactionTemplateNode>();
                transactionTemplateNodes.AddRange(await transactionTemplateNode.GenerateTransactionAdapter(request));
            }

            // 依照 ParameterNodes 的設定, 生成可直接串接的 TransactionParameterNodes
            foreach (CodeTemplate.TransactionTemplateNode transactionTemplateNode in transactionTemplateNodes)
            {
                await transactionTemplateNode.GenerateTransactionParameterNodes(request);
            }

            // 將 TransactionTemplateNode 轉換成 GenerateNode
            List<GenerateNode> generateNodes = new List<GenerateNode>();
            foreach (CodeTemplate.TransactionTemplateNode transactionTemplateNode in transactionTemplateNodes)
            {
                generateNodes.AddRange(await transactionTemplateNode.ToGenerateNodesAsync(request));
            }
            return generateNodes;
        }

        private static async Task<GenerateNode[]> ToGenerateNodesAsync(this CodeTemplate.TransactionTemplateNode templateNode, JObject request)
        {
            GenerateNode generateNode = new GenerateNode();
            generateNode.ApplyApi = templateNode.ToUrl(request);
            if (templateNode.TransactionParameterNodes != null)
            {
                foreach (var templateParameterNode in templateNode.TransactionParameterNodes)
                {
                    var children = await templateParameterNode.ToGenerateNodesAsync(request, templateNode.TransactionAdapter);
                    foreach (var child in children)
                    {
                        generateNode.AppendChild(child).ChangeKey(templateParameterNode.Name);
                    }
                }
            }
            return new GenerateNode[] { generateNode };
        }

        private static async Task<GenerateNode[]> ToGenerateNodesAsync(this CodeTemplate.TransactionParameterNode parameterNode, JObject request, JObject adapter)
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
                    jToken = request.Property(inputName, StringComparison.CurrentCultureIgnoreCase)?.Value;
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
                //parameterNode.TemplateNode.TransactionAdapter = adapter;
                return await parameterNode.TemplateNode.ToGenerateNodesAsync(request);
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
        public static async Task<CodeTemplate.TransactionTemplateNode[]> GenerateTransactionAdapter(this CodeTemplate.TransactionTemplateNode templateNode, JObject request)
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
                    JToken jToken = await adapterNode.ToJTokenAsync(request, templateNode.TransactionAdapter);
                    if (adapterNode.ResponseSplit)
                    {
                        // 如果 ResponseSplit == true,
                        // 若回傳的結果為 Object 且只有一個成員,
                        // 則向下尋訪該成員的值是否為 Array
                        JObject adapterObject = new JObject();
                        JObject adapterPropertyObject = adapterObject;
                        JProperty adapterProperty = null;
                        while (jToken is JObject tokenObject && tokenObject.Properties().Count() == 1)
                        {
                            JProperty jProperty = tokenObject.Properties().First();
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
                                    transactionTemplateNodes.AddRange(await cloneTemplateNode.GenerateTransactionAdapter(request));
                                }
                            }
                            else
                            {
                                foreach (JToken arrayToken in jArray)
                                {
                                    var cloneTemplateNode = templateNode.JsonConvertTo<CodeTemplate.TransactionTemplateNode>();
                                    cloneTemplateNode.TransactionAdapter.Add(adapterNode.Name, arrayToken);
                                    transactionTemplateNodes.AddRange(await cloneTemplateNode.GenerateTransactionAdapter(request));
                                }
                            }
                            return transactionTemplateNodes.ToArray();
                        }
                        else if (adapterProperty != null)
                        {
                            // 如果 ResponseSplit == true, 但是資料並非 Array 則將 jToken 還原
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
        public static async Task GenerateTransactionParameterNodes(this CodeTemplate.TransactionTemplateNode templateNode, JObject request)
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
                            //parameterNode.TemplateNode.TransactionAdapter = templateNode.TransactionAdapter;
                            var transactionTemplateNodes = await parameterNode.TemplateNode.GenerateTransactionAdapter(request);
                            foreach (var transactionTemplateNode in transactionTemplateNodes)
                            {
                                var cloneParameterNode = parameterNode.JsonConvertTo<CodeTemplate.TransactionParameterNode>();
                                cloneParameterNode.TemplateNode = transactionTemplateNode;
                                await cloneParameterNode.TemplateNode.GenerateTransactionParameterNodes(request);
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
        public static JObject Confine(this JObject source, string propertyConfine)
        {
            if (string.IsNullOrWhiteSpace(propertyConfine) == false)
            {
                JObject confineObject = new JObject();
                JProperty confineProperty = null;
                JToken jToken = source;
                string[] confines = propertyConfine.Split(".");
                foreach (string confine in confines)
                {
                    if (jToken is JObject jObject)
                    {
                        var jProperty = jObject.Property(confine, StringComparison.CurrentCultureIgnoreCase);
                        if (jProperty != null)
                        {
                            confineObject.Add(jProperty.Name, null);
                            confineProperty = confineObject.Property(jProperty.Name, StringComparison.CurrentCultureIgnoreCase);
                            jToken = jProperty.Value;
                            continue;
                        }
                    }
                    break;
                }
                confineProperty.Value = jToken;
                return confineObject;
            }
            return source;
        }

        /// <summary>
        /// 取得 RequestNode 所有用到的樣板 API URL
        /// </summary>
        public static string[] GetAllTemplates(this CodeTemplate.TransactionParameterNode requestNode)
        {
            // TODO: Change
            List<string> templates = new List<string>();
            if (requestNode.From == CodeTemplate.ParameterFrom.Template)
            {
                templates.Add(requestNode.TemplateNode.Url);
                if (requestNode.TemplateNode.ParameterNodes.NotNullAny())
                {
                    templates.AddRange(
                        requestNode
                            .TemplateNode
                            .ParameterNodes
                            .SelectMany(x => x.GetAllTemplates()));
                }
            }
            return templates.Distinct().ToArray();
        }

        /// <summary>
        /// 取得 RequestNode 所有用到 Input 的 Key
        /// </summary>
        public static Dictionary<string, string[]> GetAllRequestKeys(this CodeTemplate.TransactionParameterNode requestNode)
        {
            if (requestNode.From == CodeTemplate.ParameterFrom.Input)
            {
                if (string.IsNullOrWhiteSpace(requestNode.HttpRequestDescription))
                {
                    return new Dictionary<string, string[]>() { {
                        requestNode.InputName,
                        new string[0] } };
                }
                return new Dictionary<string, string[]>() { {
                    requestNode.InputName,
                    new string[] { requestNode.HttpRequestDescription } } };
            }
            else if (requestNode.From == CodeTemplate.ParameterFrom.Template)
            {
                List<KeyValuePair<string, string[]>> items = new List<KeyValuePair<string, string[]>>();
                if (requestNode.TemplateNode.AdapterNodes.NotNullAny())
                {
                    items.AddRange(requestNode
                        .TemplateNode
                        .AdapterNodes
                        .Where(x => x.RequestNodes.NotNullAny())
                        .SelectMany(x => x.RequestNodes
                            .Where(y => y.From == CodeTemplate.RequestFrom.Input)
                            .Select(y => (Key: y.InputName, Description: y.HttpRequestDescription)))
                        .GroupBy(x => x.Key)
                        .Select(x => new KeyValuePair<string, string[]>(
                            x.Key,
                            x.Select(y => y.Description)
                                .Where(y => string.IsNullOrWhiteSpace(y) == false)
                                .Distinct()
                                .ToArray())));
                }
                if (requestNode.TemplateNode.ParameterNodes.NotNullAny())
                {
                    items.AddRange(requestNode
                        .TemplateNode
                        .ParameterNodes
                        .SelectMany(x => x.GetAllRequestKeys()));
                }
                return items
                    .GroupBy(x => x.Key)
                    .Select(x => new KeyValuePair<string, string[]>(x.Key, x.SelectMany(y => y.Value).Distinct().ToArray()))
                    .ToDictionary(x => x.Key, x => x.Value);
            }
            return new Dictionary<string, string[]>();
        }
    }
}
