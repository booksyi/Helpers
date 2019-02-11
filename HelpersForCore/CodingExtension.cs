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
        /// 將 RequestSimpleNode 轉成 JToken
        /// </summary>
        public static JToken ToJToken(this RequestSimpleNode node, JObject request)
        {
            if (node.From == RequestSimpleFrom.HttpRequest)
            {
                string[] keys = node.HttpRequestKey.Split('|').Select(x => x.Trim()).ToArray();
                foreach (string key in keys)
                {
                    JToken jToken = request?.Property(key, StringComparison.CurrentCultureIgnoreCase)?.Value;
                    if (string.IsNullOrWhiteSpace(Convert.ToString(jToken)) == false)
                    {
                        return jToken;
                    }
                }
                return null;
            }
            else if (node.From == RequestSimpleFrom.Value)
            {
                return node.Value;
            }
            else if (node.From == RequestSimpleFrom.Adapter)
            {
                if (node.Adapters != null
                    && node.Adapters.ContainsKey(node.AdapterName))
                {
                    if (string.IsNullOrWhiteSpace(node.AdapterPropertyName)
                        && node.Adapters[node.AdapterName] is JValue jValue)
                    {
                        return jValue;
                    }
                    else if (node.Adapters[node.AdapterName] is JObject jObject)
                    {
                        string[] propertyNames = node.AdapterPropertyName.Split('.');
                        JToken jToken = jObject;
                        foreach (var propertyName in propertyNames)
                        {
                            jToken = (jToken as JObject)?.Property(propertyName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                        }
                        return jToken;
                    }
                    else if (node.Adapters[node.AdapterName] is JArray jArray)
                    {
                        return jArray;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 將 ApiNode 轉成 Url
        /// </summary>
        public static string ToUrl(this ApiNode node, JObject request)
        {
            string url = node.Url;
            if (node.RequestNodes.NotNullAny())
            {
                string queryString = node.RequestNodes
                    .ToDictionary(x => x.Key, x => x.Value.ToJToken(request))
                    .ToQueryString();
                url = url.AppendQueryString(queryString);
            }
            return url;
        }
        /// <summary>
        /// 將 AdapterNode 轉成 JToken
        /// </summary>
        public static async Task<JToken> ToJTokenAsync(this AdapterNode node, JObject request)
        {
            HttpClient client = new HttpClient();
            string json = null;
            if (node.HttpMethod == AdapterHttpMethod.Get)
            {
                string url = node.Url;
                if (node.RequestNodes.NotNullAny())
                {
                    string queryString = node.RequestNodes
                        .ToDictionary(x => x.Key, x => x.Value.ToJToken(request))
                        .ToQueryString();
                    url = url.AppendQueryString(queryString);
                }
                json = await client.GetStringAsync(url);
            }
            else if (node.HttpMethod == AdapterHttpMethod.Post)
            {
                var response = await client.PostAsJsonAsync(
                    node.Url,
                    node.RequestNodes?
                        .ToDictionary(x => x.Key, x => x.Value.ToJToken(request))
                        .ToJObject(request));
                json = await response.Content.ReadAsStringAsync();
            }
            JToken jToken = CSharpHelper.Try(() => JToken.Parse(json), x => x, () => json);
            if (jToken is JObject jObject)
            {
                return jObject.Confine(node.ResponseConfine);
            }
            return jToken;
        }
        /// <summary>
        /// 將 RequestNode 轉成 IEnumerable&lt;GenerateNode&gt;
        /// </summary>
        public static async Task<IEnumerable<GenerateNode>> ToGenerateNodesAsync(this RequestNode requestNode, JObject request)
        {
            if (requestNode.AdapterNodes.NotNullAny())
            {
                List<GenerateNode> generateNodes = new List<GenerateNode>();
                var nodes = await requestNode.GenerateAdapters(request);
                foreach (var node in nodes)
                {
                    generateNodes.AddRange(await node.ToGenerateNodesAsync(request));
                }
                return generateNodes;
            }
            if (requestNode.SimpleTemplateRequestNodes.NotNullAny())
            {
                await GenerateComplex(requestNode, request);
            }
            if (requestNode.From == RequestFrom.HttpRequest)
            {
                List<GenerateNode> generateNodes = new List<GenerateNode>();
                JToken jToken = null;
                string[] keys = requestNode.HttpRequestKey.Split('|').Select(x => x.Trim()).ToArray();
                foreach (string key in keys)
                {
                    JToken propertyToken = request.Property(key, StringComparison.CurrentCultureIgnoreCase)?.Value;
                    if (string.IsNullOrWhiteSpace(Convert.ToString(propertyToken)) == false)
                    {
                        jToken = propertyToken;
                        break;
                    }
                }
                if (jToken is JValue jValue)
                {
                    generateNodes.Add(new GenerateNode()
                    {
                        ApplyValue = Convert.ToString(jValue)
                    });
                }
                else if (jToken is JObject jObject)
                {
                    generateNodes.Add(new GenerateNode()
                    {
                        ApplyValue = Convert.ToString(jObject)
                    });
                }
                else if (jToken is JArray jArray)
                {
                    foreach (JToken arrayToken in jArray)
                    {
                        generateNodes.Add(new GenerateNode()
                        {
                            ApplyValue = Convert.ToString(arrayToken)
                        });
                    }
                }
                return generateNodes;
            }
            else if (requestNode.From == RequestFrom.Value)
            {
                GenerateNode generateNode = new GenerateNode
                {
                    ApplyValue = requestNode.Value
                };
                return new GenerateNode[] { generateNode };
            }
            else if (requestNode.From == RequestFrom.Adapter)
            {
                List<GenerateNode> generateNodes = new List<GenerateNode>();
                if (requestNode.Adapters != null
                    && requestNode.Adapters.Any()
                    && requestNode.Adapters.ContainsKey(requestNode.AdapterName))
                {
                    string[] propertyNames = requestNode.AdapterPropertyName?.Split('.');
                    JToken jToken = requestNode.Adapters[requestNode.AdapterName];
                    if (jToken is JObject && propertyNames != null && propertyNames.Any())
                    {
                        foreach (var propertyName in propertyNames)
                        {
                            jToken = (jToken as JObject)?.Property(propertyName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                        }
                    }
                    if (jToken != null)
                    {
                        if (jToken is JValue jValue)
                        {
                            generateNodes.Add(new GenerateNode()
                            {
                                ApplyValue = Convert.ToString(jValue)
                            });
                        }
                        else if (jToken is JObject jObject)
                        {
                            generateNodes.Add(new GenerateNode()
                            {
                                ApplyValue = Convert.ToString(jObject)
                            });
                        }
                        else if (jToken is JArray jArray)
                        {
                            foreach (JToken arrayToken in jArray)
                            {
                                generateNodes.Add(new GenerateNode()
                                {
                                    ApplyValue = Convert.ToString(arrayToken)
                                });
                            }
                        }
                    }
                }
                return generateNodes;
            }
            else if (requestNode.From == RequestFrom.Template)
            {
                GenerateNode generateNode = new GenerateNode();
                if (requestNode.TemplateNode.RequestNodes?.Values != null)
                {
                    foreach (var adapterRequestNode in requestNode.TemplateNode.RequestNodes?.Values)
                    {
                        adapterRequestNode.Adapters = requestNode.Adapters;
                    }
                }
                generateNode.ApplyApi = requestNode.TemplateNode.ToUrl(request);
                if (requestNode.ComplexTemplateRequestNodes != null)
                {
                    foreach (var key in requestNode.ComplexTemplateRequestNodes.Keys)
                    {
                        var parameterNodes = requestNode.ComplexTemplateRequestNodes[key];
                        foreach (var node in parameterNodes)
                        {
                            var children = await ToGenerateNodesAsync(node, request);
                            foreach (var child in children)
                            {
                                generateNode.AppendChild(child).ChangeKey(key);
                            }
                        }
                    }
                }
                return new GenerateNode[] { generateNode };
            }
            return new GenerateNode[0];
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
        /// 依照 RequestNode.AdapterNodes 生成 RequestNode.Adapters
        /// </summary>
        public static async Task<IEnumerable<RequestNode>> GenerateAdapters(this RequestNode requestNode, JObject request)
        {
            if (requestNode.AdapterNodes != null
                && requestNode.AdapterNodes.Any())
            {
                if (requestNode.Adapters == null)
                {
                    requestNode.Adapters = new Dictionary<string, JToken>();
                }
                string adapterNodeKey = requestNode.AdapterNodes.First().Key;
                var adapterNode = requestNode.AdapterNodes[adapterNodeKey];
                requestNode.AdapterNodes.Remove(adapterNodeKey);
                if (adapterNode.RequestNodes?.Values != null)
                {
                    foreach (var adapterRequestNode in adapterNode.RequestNodes.Values)
                    {
                        adapterRequestNode.Adapters = requestNode.Adapters;
                    }
                }
                JToken adapterNodeValue = await adapterNode.ToJTokenAsync(request);
                if (adapterNodeValue is JObject jObject)
                {
                    if (adapterNode.Type == AdapterType.Unification)
                    {
                        requestNode.Adapters.Add(adapterNodeKey, jObject);
                        return await GenerateAdapters(requestNode, request);
                    }
                    else if (adapterNode.Type == AdapterType.Separation)
                    {
                        JObject adapterObject = new JObject();
                        JProperty adapterProperty = null;
                        JToken jToken = jObject;
                        while (jToken is JObject tokenObject && tokenObject.Properties().Count() == 1)
                        {
                            JProperty jProperty = tokenObject.Properties().First();
                            adapterObject.Add(jProperty.Name, null);
                            adapterProperty = adapterObject.Property(jProperty.Name, StringComparison.CurrentCultureIgnoreCase);
                            jToken = jProperty.Value;
                        }
                        if (jToken is JArray jArray)
                        {
                            List<RequestNode> nodes = new List<RequestNode>();
                            foreach (JToken arrayToken in jArray)
                            {
                                adapterProperty.Value = arrayToken;
                                var cloneAdapterObject = JObject.Parse(adapterObject.ToString());
                                var cloneRequestNode = requestNode.JsonConvertTo<RequestNode>();
                                cloneRequestNode.Adapters.Add(adapterNodeKey, cloneAdapterObject);
                                nodes.AddRange(await GenerateAdapters(cloneRequestNode, request));
                            }
                            return nodes;
                        }
                    }
                }
                else if (adapterNodeValue is JArray jArray)
                {
                    if (adapterNode.Type == AdapterType.Unification)
                    {
                        JArray adapterArray = new JArray();
                        foreach (JToken arrayToken in jArray)
                        {
                            adapterArray.Add(arrayToken);
                        }
                        requestNode.Adapters.Add(adapterNodeKey, adapterArray);
                        return await GenerateAdapters(requestNode, request);
                    }
                    else if (adapterNode.Type == AdapterType.Separation)
                    {
                        List<RequestNode> nodes = new List<RequestNode>();
                        foreach (JToken arrayToken in jArray)
                        {
                            var cloneRequestNode = requestNode.JsonConvertTo<RequestNode>();
                            cloneRequestNode.Adapters.Add(adapterNodeKey, arrayToken);
                            nodes.AddRange(await GenerateAdapters(cloneRequestNode, request));
                        }
                        return nodes;
                    }
                }
            }
            return new RequestNode[] { requestNode };
        }
        /// <summary>
        /// 依照 RequestNode.SimpleTemplateRequestNodes 生成 RequestNode.ComplexTemplateRequestNodes
        /// </summary>
        public static async Task GenerateComplex(this RequestNode requestNode, JObject request)
        {
            if (requestNode.SimpleTemplateRequestNodes != null
                && requestNode.SimpleTemplateRequestNodes.Any())
            {
                string[] keys = requestNode.SimpleTemplateRequestNodes.Keys.ToArray();
                requestNode.ComplexTemplateRequestNodes = new Dictionary<string, IEnumerable<RequestNode>>();
                foreach (var key in keys)
                {
                    RequestNode node = requestNode.SimpleTemplateRequestNodes[key];
                    if (node != null)
                    {
                        node.Adapters = requestNode.Adapters;
                        if (node.From == RequestFrom.Template)
                        {
                            var complex = await GenerateAdapters(node, request);
                            requestNode.ComplexTemplateRequestNodes.Add(key, complex);
                        }
                        else
                        {
                            requestNode.ComplexTemplateRequestNodes.Add(key, new RequestNode[] { node });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 取得 RequestNode 所有用到的樣板 API URL
        /// </summary>
        public static string[] GetAllTemplates(this RequestNode requestNode)
        {
            List<string> templates = new List<string>();
            if (requestNode.From == RequestFrom.Template)
            {
                templates.Add(requestNode.TemplateNode.Url);
                if (requestNode.SimpleTemplateRequestNodes.NotNullAny())
                {
                    templates.AddRange(
                        requestNode
                            .SimpleTemplateRequestNodes
                            .Values
                            .SelectMany(x => x.GetAllTemplates()));
                }
            }
            return templates.Distinct().ToArray();
        }

        /// <summary>
        /// 取得 RequestNode 所有用到 Input 的 Key
        /// </summary>
        public static Dictionary<string, string[]> GetAllRequestKeys(this RequestNode requestNode)
        {
            if (requestNode.From == RequestFrom.HttpRequest)
            {
                if (string.IsNullOrWhiteSpace(requestNode.HttpRequestDescription))
                {
                    return new Dictionary<string, string[]>() { {
                        requestNode.HttpRequestKey,
                        new string[0] } };
                }
                return new Dictionary<string, string[]>() { {
                    requestNode.HttpRequestKey,
                    new string[] { requestNode.HttpRequestDescription } } };
            }
            else if (requestNode.From == RequestFrom.Template)
            {
                List<KeyValuePair<string, string[]>> items = new List<KeyValuePair<string, string[]>>();
                if (requestNode.AdapterNodes.NotNullAny())
                {
                    items.AddRange(requestNode
                        .AdapterNodes
                        .Values
                        .Where(x => x.RequestNodes.NotNullAny())
                        .SelectMany(x => x.RequestNodes
                            .Values
                            .Where(y => y.From == RequestSimpleFrom.HttpRequest)
                            .Select(y => (Key: y.HttpRequestKey, Description: y.HttpRequestDescription)))
                        .GroupBy(x => x.Key)
                        .Select(x => new KeyValuePair<string, string[]>(
                            x.Key,
                            x.Select(y => y.Description)
                                .Where(y => string.IsNullOrWhiteSpace(y) == false)
                                .Distinct()
                                .ToArray())));
                }
                if (requestNode.SimpleTemplateRequestNodes.NotNullAny())
                {
                    items.AddRange(requestNode
                        .SimpleTemplateRequestNodes
                        .Values
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
