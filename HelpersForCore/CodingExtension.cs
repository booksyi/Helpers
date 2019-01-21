using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
            { "@@Endline", "\r\n" },
            { "@@Tab", "\t" }
        };

        /// <summary>
        /// 以 GenerateNode 生成代碼
        /// </summary>
        public static async Task<string> GenerateAsync(this GenerateNode root)
        {
            const string paramLeft = "{{#";
            const string paramRight = "}}";
            const string eachSeparator = ",";
            const string withDefault = "|";

            string result = await root.GetApplyTextAsync();
            if (string.IsNullOrWhiteSpace(result))
            {
                return null;
            }

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
                                            if (node.ApplyParameters.Any())
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
        public static async Task<JToken> ToJTokenAsync(this GenerateNode node)
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
                                        await groupParameters.First().ToJTokenAsync());
                                }
                            }
                            else
                            {
                                JArray jArray = new JArray();
                                foreach (var parameter in groupParameters)
                                {
                                    jArray.Add(await parameter.ToJTokenAsync());
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
        /// 將 RequestNode 的 HttpRequest 及 Adapters 丟給所有子節點
        /// </summary>
        public static void Deep(this RequestNode node)
        {
            if (node.AdapterNodes != null)
            {
                foreach (var adapterNode in node.AdapterNodes.Values)
                {
                    if (adapterNode.RequestNodes != null)
                    {
                        foreach (var adapterRequestNode in adapterNode.RequestNodes.Values)
                        {
                            adapterRequestNode.HttpRequest = node.HttpRequest;
                            if (node.Complex?.Adapters != null)
                            {
                                adapterRequestNode
                                    .NewPropertyIfNull(x => x.Complex).Complex
                                    .NewPropertyIfNull(x => x.Adapters).Adapters =
                                        node.Complex.Adapters;
                            }
                        }
                    }
                }
            }
            if (node.TemplateRequestNodes != null)
            {
                foreach (var requestNode in node.TemplateRequestNodes.Values)
                {
                    requestNode.HttpRequest = node.HttpRequest;
                    if (node.Complex?.Adapters != null)
                    {
                        requestNode
                            .NewPropertyIfNull(x => x.Complex).Complex
                            .NewPropertyIfNull(x => x.Adapters).Adapters =
                                node.Complex.Adapters;
                    }
                    Deep(requestNode);
                }
            }
        }

        public static async Task<JObject> ToJObjectAsync(this Dictionary<string, RequestNode> nodes)
        {
            JObject jObject = new JObject();
            foreach (var key in nodes.Keys)
            {
                var node = nodes[key];
                if (node.From == RequestFrom.HttpRequest)
                {
                    if (node.HttpRequest != null
                        && node.HttpRequest.ContainsKey(node.HttpRequestKey))
                    {
                        jObject.Add(key, node.HttpRequest[node.HttpRequestKey]);
                    }
                }
                else if (node.From == RequestFrom.Adapter)
                {
                    if (node.Complex?.Adapters != null
                        && node.Complex.Adapters.ContainsKey(node.AdapterName))
                    {
                        if (string.IsNullOrWhiteSpace(node.AdapterPropertyName)
                            && node.Complex.Adapters[node.AdapterName] is JValue value)
                        {
                            jObject.Add(key, value);
                        }
                        else if (node.Complex.Adapters[node.AdapterName] is JObject @object)
                        {
                            string[] propertyNames = node.AdapterPropertyName.Split('.');
                            JToken jToken = @object;
                            foreach (var propertyName in propertyNames)
                            {
                                jToken = (jToken as JObject)?.Property(propertyName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                            }
                            jObject.Add(key, jToken);
                            //jObject.Add(key, @object.Property(node.AdapterPropertyName, StringComparison.CurrentCultureIgnoreCase)?.Value);
                        }
                    }
                }
            }
            return jObject;
        }

        public static async Task<JToken> ToJTokenAsync(this RequestAdapterNode node)
        {
            HttpClient client = new HttpClient();
            var response = await client.PostAsJsonAsync(node.Url, await node.RequestNodes.ToJObjectAsync());
            var json = await response.Content.ReadAsStringAsync();
            return JToken.FromObject(JsonConvert.DeserializeObject(json));
        }

        public static async Task<IEnumerable<RequestNode>> BuildComplex(this RequestNode requestNode)
        {
            #region requestNode.AdapterNodes to Adapters
            if (requestNode.AdapterNodes != null && requestNode.AdapterNodes.Any())
            {
                string adapterNodeKey = requestNode.AdapterNodes.First().Key;
                var adapterNode = requestNode.AdapterNodes[adapterNodeKey];
                requestNode.AdapterNodes.Remove(adapterNodeKey);
                JToken adapterValue = await adapterNode.ToJTokenAsync();
                if (adapterValue is JObject jObject)
                {
                    if (adapterNode.Type == RequestAdapterType.Unification)
                    {
                        requestNode
                            .NewPropertyIfNull(x => x.Complex).Complex
                            .NewPropertyIfNull(x => x.Adapters).Adapters
                            .Add(adapterNodeKey, jObject);
                        requestNode.Deep();
                        return await BuildComplex(requestNode);
                    }
                    else if (adapterNode.Type == RequestAdapterType.Separation)
                    {
                        if (jObject.Properties().Count() == 1 && jObject.Properties().First().Value is JArray jArray)
                        {
                            List<RequestNode> nodes = new List<RequestNode>();
                            foreach (var value in jArray)
                            {
                                JObject jValue = new JObject();
                                jValue.Add(jObject.Properties().First().Name, value);
                                RequestNode clone = requestNode.JsonConvertTo<RequestNode>();
                                clone
                                    .NewPropertyIfNull(x => x.Complex).Complex
                                    .NewPropertyIfNull(x => x.Adapters).Adapters
                                    .Add(adapterNodeKey, jValue);
                                clone.Deep();
                                nodes.AddRange(await BuildComplex(clone));
                            }
                            return nodes;
                        }
                    }
                }
                else if (adapterValue is JArray values)
                {
                    if (adapterNode.Type == RequestAdapterType.Unification)
                    {
                        // TODO: FIX ??
                        JArray jArray = new JArray();
                        foreach (JToken value in values)
                        {
                            jArray.Add(value);
                        }
                        requestNode
                            .NewPropertyIfNull(x => x.Complex).Complex
                            .NewPropertyIfNull(x => x.Adapters).Adapters
                            .Add(adapterNodeKey, jArray);
                        requestNode.Deep();
                        return await BuildComplex(requestNode);
                    }
                    else if (adapterNode.Type == RequestAdapterType.Separation)
                    {
                        List<RequestNode> nodes = new List<RequestNode>();
                        foreach (var value in values)
                        {
                            RequestNode clone = requestNode.JsonConvertTo<RequestNode>();
                            clone
                                .NewPropertyIfNull(x => x.Complex).Complex
                                .NewPropertyIfNull(x => x.Adapters).Adapters
                                .Add(adapterNodeKey, value);
                            clone.Deep();
                            nodes.AddRange(await BuildComplex(clone));
                        }
                        return nodes;
                    }
                }
            }
            #endregion

            #region Deep
            if (requestNode.TemplateRequestNodes.Any())
            {
                string[] keys = requestNode.TemplateRequestNodes.Keys.ToArray();
                foreach (var key in keys)
                {
                    if (requestNode.TemplateRequestNodes[key].From == RequestFrom.Template)
                    {
                        var complex = await BuildComplex(requestNode.TemplateRequestNodes[key]);
                        requestNode
                            .NewPropertyIfNull(x => x.Complex).Complex
                            .NewPropertyIfNull(x => x.TemplateRequestNodes).TemplateRequestNodes
                            .Add(key, complex);
                    }
                    else
                    {
                        requestNode
                            .NewPropertyIfNull(x => x.Complex).Complex
                            .NewPropertyIfNull(x => x.TemplateRequestNodes).TemplateRequestNodes
                            .Add(key, new RequestNode[] { requestNode.TemplateRequestNodes[key] });
                    }
                }
            }
            #endregion
            return new RequestNode[] { requestNode };
        }

        public static async Task<IEnumerable<GenerateNode>> ToGenerateNode(this RequestNode requestNode)
        {
            if (requestNode.From == RequestFrom.HttpRequest)
            {
                List<GenerateNode> generateNodes = new List<GenerateNode>();
                var values = requestNode.HttpRequest
                    .Where(x => x.Key == requestNode.HttpRequestKey)
                    .Select(x => Convert.ToString(x.Value)).ToArray();
                foreach (var value in values)
                {
                    generateNodes.Add(new GenerateNode(requestNode.HttpRequestKey, value));
                }
                return generateNodes;
            }
            else if (requestNode.From == RequestFrom.Adapter)
            {
                string[] propertyNames = requestNode.AdapterPropertyName.Split('.');
                JToken jToken = requestNode.Complex.Adapters[requestNode.AdapterName];
                foreach (var propertyName in propertyNames)
                {
                    jToken = (jToken as JObject)?.Property(propertyName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                }
                List<GenerateNode> generateNodes = new List<GenerateNode>();
                /*
                var jToken =
                    (requestNode.Complex.Adapters[requestNode.AdapterName] as JObject)?
                    .Property(requestNode.AdapterPropertyName, StringComparison.CurrentCultureIgnoreCase)?
                    .Value;*/
                if (jToken != null)
                {
                    if (jToken is JValue jValue)
                    {
                        generateNodes.Add(new GenerateNode(
                            requestNode.AdapterPropertyName,
                            Convert.ToString(jValue)));
                    }
                    else if (jToken is JObject jObject)
                    {
                        generateNodes.Add(new GenerateNode(
                            requestNode.AdapterPropertyName,
                            Convert.ToString(jObject)));
                    }
                    else if (jToken is JArray jArray)
                    {
                        foreach (var value in jArray)
                        {
                            generateNodes.Add(new GenerateNode(
                                requestNode.AdapterPropertyName,
                                Convert.ToString(value)));
                        }
                    }
                }
                return generateNodes;
            }
            else if(requestNode.From == RequestFrom.Template)
            {
                GenerateNode generateNode = new GenerateNode();
                generateNode.ApplyApi = requestNode.TemplateUrl;
                if (requestNode.Complex?.TemplateRequestNodes != null)
                {
                    foreach (var key in requestNode.Complex.TemplateRequestNodes.Keys)
                    {
                        var parameterNodes = requestNode.Complex.TemplateRequestNodes[key];
                        foreach (var node in parameterNodes)
                        {
                            var children = await ToGenerateNode(node);
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
    }
}
