﻿using Newtonsoft.Json;
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

        public static async Task<string> ToQueryString(this Dictionary<string, RequestSimpleNode> nodes, JObject request)
        {
            List<string> queries = new List<string>();
            foreach (var key in nodes.Keys)
            {
                var node = nodes[key];
                if (node.From == RequestSimpleFrom.HttpRequest)
                {
                    string queryValue = Convert.ToString(request?.Property(node.HttpRequestKey, StringComparison.CurrentCultureIgnoreCase)?.Value);
                    queries.Add($"{key}={HttpUtility.UrlEncode(queryValue)}");
                }
                else if (node.From == RequestSimpleFrom.Adapter)
                {
                    if (node.Adapters != null
                        && node.Adapters.ContainsKey(node.AdapterName))
                    {
                        if (string.IsNullOrWhiteSpace(node.AdapterPropertyName)
                            && node.Adapters[node.AdapterName] is JValue jValue)
                        {
                            queries.Add($"{key}={HttpUtility.UrlEncode(Convert.ToString(jValue))}");
                        }
                        else if (node.Adapters[node.AdapterName] is JObject jObject)
                        {
                            string[] propertyNames = node.AdapterPropertyName.Split('.');
                            JToken jToken = jObject;
                            foreach (var propertyName in propertyNames)
                            {
                                jToken = (jToken as JObject)?.Property(propertyName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                            }
                            queries.Add($"{key}={HttpUtility.UrlEncode(Convert.ToString(jToken))}");
                        }
                        else if (node.Adapters[node.AdapterName] is JArray jArray)
                        {
                            foreach (var jToken in jArray)
                            {
                                queries.Add($"{key}={HttpUtility.UrlEncode(Convert.ToString(jToken))}");
                            }
                        }
                    }
                }
            }
            return string.Join("&", queries);
        }

        public static async Task<JObject> ToJObjectAsync(this Dictionary<string, RequestSimpleNode> nodes, JObject request)
        {
            JObject result = new JObject();
            foreach (var key in nodes.Keys)
            {
                var node = nodes[key];
                if (node.From == RequestSimpleFrom.HttpRequest)
                {
                    var jToken = request?.Property(node.HttpRequestKey, StringComparison.CurrentCultureIgnoreCase)?.Value;
                    result.Add(key, jToken);
                }
                else if (node.From == RequestSimpleFrom.Adapter)
                {
                    if (node.Adapters != null
                        && node.Adapters.ContainsKey(node.AdapterName))
                    {
                        if (string.IsNullOrWhiteSpace(node.AdapterPropertyName)
                            && node.Adapters[node.AdapterName] is JValue jValue)
                        {
                            result.Add(key, jValue);
                        }
                        else if (node.Adapters[node.AdapterName] is JObject jObject)
                        {
                            string[] propertyNames = node.AdapterPropertyName.Split('.');
                            JToken jToken = jObject;
                            foreach (var propertyName in propertyNames)
                            {
                                jToken = (jToken as JObject)?.Property(propertyName, StringComparison.CurrentCultureIgnoreCase)?.Value;
                            }
                            result.Add(key, jToken);
                        }
                        else if (node.Adapters[node.AdapterName] is JArray jArray)
                        {
                            result.Add(key, jArray);
                        }
                    }
                }
            }
            return result;
        }

        public static async Task<JToken> ToJTokenAsync(this AdapterNode node, JObject request)
        {
            HttpClient client = new HttpClient();
            string json = null;
            if (node.HttpMethod == AdapterHttpMethod.Get)
            {
                json = await client.GetStringAsync(node.Url.AppendQueryString(await node.RequestNodes.ToQueryString(request)));
            }
            else if (node.HttpMethod == AdapterHttpMethod.Post)
            {
                var response = await client.PostAsJsonAsync(node.Url, await node.RequestNodes.ToJObjectAsync(request));
                json = await response.Content.ReadAsStringAsync();
            }
            return JToken.FromObject(JsonConvert.DeserializeObject(json));
        }

        /// <summary>
        /// AdapterNodes to Adapters
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
                JToken adapterValue = await adapterNode.ToJTokenAsync(request);
                if (adapterValue is JObject jObject)
                {
                    #region ResponseConfines
                    if (adapterNode.ResponseConfines != null && adapterNode.ResponseConfines.Any())
                    {
                        string[] confines = adapterNode.ResponseConfines.Select(x => x.ToLower()).ToArray();
                        string[] propertyNames = jObject
                            .Properties()
                            .Select(x => x.Name.ToLower())
                            .Where(x => confines.Contains(x))
                            .ToArray();
                        foreach (string propertyName in propertyNames)
                        {
                            jObject
                                .Property(propertyName, StringComparison.CurrentCultureIgnoreCase)
                                .Remove();
                        }
                    }
                    #endregion
                    if (adapterNode.Type == AdapterType.Unification)
                    {
                        requestNode.Adapters.Add(adapterNodeKey, jObject);
                        return await GenerateAdapters(requestNode, request);
                    }
                    else if (adapterNode.Type == AdapterType.Separation)
                    {
                        if (jObject.Properties().Count() == 1 && jObject.Properties().First().Value is JArray jArray)
                        {
                            List<RequestNode> nodes = new List<RequestNode>();
                            foreach (var value in jArray)
                            {
                                JObject jValue = new JObject();
                                jValue.Add(jObject.Properties().First().Name, value);
                                var cloneRequestNode = requestNode.JsonConvertTo<RequestNode>();
                                cloneRequestNode.Adapters.Add(adapterNodeKey, jValue);
                                nodes.AddRange(await GenerateAdapters(cloneRequestNode, request));
                            }
                            return nodes;
                        }
                    }
                }
                else if (adapterValue is JArray values)
                {
                    if (adapterNode.Type == AdapterType.Unification)
                    {
                        JArray jArray = new JArray();
                        foreach (JToken value in values)
                        {
                            jArray.Add(value);
                        }
                        requestNode.Adapters.Add(adapterNodeKey, jArray);
                        return await GenerateAdapters(requestNode, request);
                    }
                    else if (adapterNode.Type == AdapterType.Separation)
                    {
                        List<RequestNode> nodes = new List<RequestNode>();
                        foreach (var value in values)
                        {
                            var cloneRequestNode = requestNode.JsonConvertTo<RequestNode>();
                            cloneRequestNode.Adapters.Add(adapterNodeKey, value);
                            nodes.AddRange(await GenerateAdapters(cloneRequestNode, request));
                        }
                        return nodes;
                    }
                }
            }
            return new RequestNode[] { requestNode };
        }
        /// <summary>
        /// SimpleTemplateRequestNodes to ComplexTemplateRequestNodes
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

        public static async Task<IEnumerable<GenerateNode>> ToGenerateNode(this RequestNode requestNode, JObject request)
        {
            if (requestNode.AdapterNodes.NotNullAndAny())
            {
                List<GenerateNode> generateNodes = new List<GenerateNode>();
                var nodes = await requestNode.GenerateAdapters(request);
                foreach (var node in nodes)
                {
                    generateNodes.AddRange(await node.ToGenerateNode(request));
                }
                return generateNodes;
            }
            if (requestNode.SimpleTemplateRequestNodes.NotNullAndAny())
            {
                await GenerateComplex(requestNode, request);
            }
            if (requestNode.From == RequestFrom.HttpRequest)
            {
                List<GenerateNode> generateNodes = new List<GenerateNode>();
                var jToken = request.Property(requestNode.HttpRequestKey, StringComparison.CurrentCultureIgnoreCase)?.Value;
                if (jToken is JValue jValue)
                {
                    generateNodes.Add(new GenerateNode(
                        requestNode.HttpRequestKey,
                        Convert.ToString(jValue)));
                }
                else if (jToken is JObject jObject)
                {
                    generateNodes.Add(new GenerateNode(
                        requestNode.HttpRequestKey,
                        Convert.ToString(jObject)));
                }
                else if (jToken is JArray jArray)
                {
                    foreach (var value in jArray)
                    {
                        generateNodes.Add(new GenerateNode(
                            requestNode.HttpRequestKey,
                            Convert.ToString(value)));
                    }
                }
                return generateNodes;
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
                }
                return generateNodes;
            }
            else if(requestNode.From == RequestFrom.Template)
            {
                GenerateNode generateNode = new GenerateNode();
                generateNode.ApplyApi = requestNode.TemplateUrl;
                if (requestNode.ComplexTemplateRequestNodes != null)
                {
                    foreach (var key in requestNode.ComplexTemplateRequestNodes.Keys)
                    {
                        var parameterNodes = requestNode.ComplexTemplateRequestNodes[key];
                        foreach (var node in parameterNodes)
                        {
                            var children = await ToGenerateNode(node, request);
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
