﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace HelpersForFramework
{
    public static class CSharpExtension
    {
        /// <summary>
        /// 同 SQL 的 between: 判斷一個數是否介於兩個數之間 (包含臨界值)
        /// </summary>
        public static bool Between(this int i, int a, int b)
        {
            return (a <= i && i <= b) || (b <= i && i <= a);
        }
        /// <summary>
        /// 同 SQL 的 between: 判斷一個數是否介於兩個數之間 (包含臨界值)
        /// </summary>
        public static bool Between(this long i, long a, long b)
        {
            return (a <= i && i <= b) || (b <= i && i <= a);
        }
        /// <summary>
        /// 同 SQL 的 between: 判斷一個數是否介於兩個數之間 (包含臨界值)
        /// </summary>
        public static bool Between(this float i, float a, float b)
        {
            return (a <= i && i <= b) || (b <= i && i <= a);
        }
        /// <summary>
        /// 同 SQL 的 between: 判斷一個數是否介於兩個數之間 (包含臨界值)
        /// </summary>
        public static bool Between(this double i, double a, double b)
        {
            return (a <= i && i <= b) || (b <= i && i <= a);
        }
        /// <summary>
        /// 同 SQL 的 between: 判斷一個數是否介於兩個數之間 (包含臨界值)
        /// </summary>
        public static bool Between(this decimal i, decimal a, decimal b)
        {
            return (a <= i && i <= b) || (b <= i && i <= a);
        }
        /// <summary>
        /// 同 SQL 的 between: 判斷一個時間是否介於兩個時間之間 (包含臨界值)
        /// </summary>
        public static bool Between(this DateTime i, DateTime a, DateTime b)
        {
            return (a <= i && i <= b) || (b <= i && i <= a);
        }
        /// <summary>
        /// 同 SQL 的 in: 判斷值是否存在集合中
        /// </summary>
        public static bool In<T>(this T item, params T[] list)
        {
            return list.Contains(item);
        }
        /// <summary>
        /// 判斷實值型別的 Nullable 是否為 null 或預設值
        /// </summary>
        public static bool IsNullOrDefault<T>(this T? value) where T : struct
        {
            return (value.HasValue && (value.Value.Equals(default(T)) == false));
        }
        /// <summary>
        /// 同 SQL 的 isnull: 第一個值不為 null 回傳第一個值, 第一個值為 null 回傳第二個值
        /// </summary>
        public static object IsNull(this object value1, object value2)
        {
            if (value1 == null || value1 == DBNull.Value)
            {
                return value2;
            }
            return value1;
        }
        /// <summary>
        /// 第一個值不為 null or empty 回傳第一個值, 第一個值為 null or empty 回傳第二個值
        /// </summary>
        public static string IsNull(this string value1, string value2)
        {
            if (string.IsNullOrEmpty(value1))
            {
                return value2;
            }
            return value1;
        }
        /// <summary>
        /// 第一個值不為 null 或預設值回傳第一個值, 第一個值為 null 或預設值回傳第二個值
        /// </summary>
        public static T IsNull<T>(this T? value1, T value2) where T : struct
        {
            if (value1.HasValue && (value1.Value.Equals(default(T)) == false))
            {
                return value1.Value;
            }
            return value2;
        }

        /// <summary>
        /// 直接用 params 做 List 的 AddRange
        /// </summary>
        public static void AddRange<T>(this List<T> list, params T[] items)
        {
            list.AddRange(items);
        }

        /// <summary>
        /// 找出字典的 Key 對應的值, 沒有可對應的 Key 傳回指定值
        /// </summary>
        public static T2 CaseValuesOrDefault<T1, T2>(this T1 value, Dictionary<T1, T2> caseValues, T2 defaultValue = default(T2))
        {
            if (caseValues.ContainsKey(value))
            {
                return caseValues[value];
            }
            return defaultValue;
        }

        public static T2 ConvertTo<T1, T2>(this T1 value, Func<T1, T2> converter)
        {
            return converter(value);
        }

        /// <summary>
        /// 將物件透過 Json 的 Serialize 與 Deserialize 轉換成另一個類型的物件
        /// </summary>
        public static T JsonConvertTo<T>(this object value)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        }

        /// <summary>
        /// 將 DataRow 的資料轉換成 Dictionary
        /// </summary>
        public static Dictionary<string, object> ToDictionary(this DataRow dr)
        {
            if (dr != null)
            {
                return dr.Table.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToDictionary(x => x, x => dr[x]);
            }
            return null;
        }
        /// <summary>
        /// 將自訂類型的 Properties 轉換成 Dictionary
        /// </summary>
        public static Dictionary<string, object> ToDictionary<T>(this T model, bool isComponentModel = false)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            var props = isComponentModel ?
                CSharpHelper.GetMappedProperties<T>() :
                model.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {
                properties.Add(
                    isComponentModel ? CSharpHelper.GetColumnName(prop) : prop.Name,
                    prop.GetValue(model));
            }
            return properties;
        }
        /// <summary>
        /// 將自訂類型的 Properties 轉換成 Dictionary,
        /// 轉換可依照 Attribute 來執行自定義的處理邏輯
        /// </summary>
        public static Dictionary<string, object> ToDictionary<T, TAttr>(this T model, Func<object, IEnumerable<TAttr>, object> attrAdapter, bool isComponentModel = false) where TAttr : Attribute
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            var props = isComponentModel ?
                CSharpHelper.GetMappedProperties<T>() :
                model.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {
                string key = isComponentModel ? CSharpHelper.GetColumnName(prop) : prop.Name;
                object value = prop.GetValue(model);
                IEnumerable<TAttr> attrs = prop.GetCustomAttributes<TAttr>();
                if (attrs.Any())
                {
                    value = attrAdapter(value, attrs);
                }
                properties.Add(key, value);
            }
            return properties;
        }

        /// <summary>
        /// 將 DataRow 的資料轉換成自訂的 Class
        /// </summary>
        public static T ToModel<T>(this DataRow dr, bool caseSensitive = false)
        {
            object model = dr.ToTypeObject(typeof(T), caseSensitive);
            if (model != null)
            {
                return (T)model;
            }
            return default(T);
        }
        /// <summary>
        /// 將 DataReader 的資料轉換成自訂的 Class
        /// </summary>
        public static T ToModel<T>(this IDataReader dr, bool caseSensitive = false)
        {
            object model = dr.ToTypeObject(typeof(T), caseSensitive);
            if (model != null)
            {
                return (T)model;
            }
            return default(T);
        }
        /// <summary>
        /// 將 Dictionary 轉換成自訂的 Class
        /// </summary>
        public static T1 ToModel<T1, T2>(this Dictionary<string, T2> dictionary, bool caseSensitive = false)
        {
            object model = dictionary.ToTypeObject(typeof(T1), caseSensitive);
            if (model != null)
            {
                return (T1)model;
            }
            return default(T1);
        }

        /// <summary>
        /// 將 DataRowCollection 的每一個 DataRow 都轉換成自訂的 Class 之後, 傳回自訂 Class 的集合
        /// </summary>
        public static IEnumerable<T> ToModels<T>(this DataRowCollection collection, bool caseSensitive = false)
        {
            List<T> list = new List<T>();
            foreach (DataRow dr in collection)
            {
                list.Add(dr.ToModel<T>(caseSensitive));
            }
            return list;
        }

        /// <summary>
        /// 將 DataRow 的資料轉換成自訂 Class 的 object
        /// </summary>
        public static object ToTypeObject(this DataRow dr, Type type, bool caseSensitive = false)
        {
            if (dr != null)
            {
                object model = type.Assembly.CreateInstance(type.FullName);
                var props = type.GetProperties();
                var names = caseSensitive ?
                    dr.Table.Columns.Cast<DataColumn>().Select(x => x.ColumnName) :
                    dr.Table.Columns.Cast<DataColumn>().Select(x => x.ColumnName.ToLower());
                Dictionary<PropertyInfo, Dictionary<string, object>> innerClassProperties = new Dictionary<PropertyInfo, Dictionary<string, object>>();
                foreach (PropertyInfo prop in props)
                {
                    string columnName = CSharpHelper.GetColumnName(prop);
                    if (names.Contains(caseSensitive ?
                            columnName :
                            columnName.ToLower())
                        && dr[columnName] != DBNull.Value)
                    {
                        CSharpHelper.SetValueToProperty(model, prop, dr[columnName]);
                    }
                    else
                    {
                        Dictionary<string, object> values = dr.ToDictionary()
                            .Where(x => x.Key.StartsWith($"{columnName}."))
                            .ToDictionary(
                                x => x.Key.Substring(columnName.Length + 1),
                                x => x.Value);
                        if (values.Any())
                        {
                            CSharpHelper.SetValueToProperty(model, prop, values.ToTypeObject(prop.PropertyType));
                        }
                    }
                }
                return model;
            }
            return null;
        }
        /// <summary>
        /// 將 DataReader 的資料轉換成自訂 Class 的 object
        /// </summary>
        public static object ToTypeObject(this IDataReader dr, Type type, bool caseSensitive = false)
        {
            if (dr != null)
            {
                object model = type.Assembly.CreateInstance(type.FullName);
                var props = type.GetProperties();
                var names = Enumerable.Range(0, dr.FieldCount)
                    .Select(i => caseSensitive ? dr.GetName(i) : dr.GetName(i).ToLower());
                foreach (PropertyInfo prop in props)
                {
                    string columnName = CSharpHelper.GetColumnName(prop);
                    if (names.Contains(caseSensitive ?
                            columnName :
                            columnName.ToLower())
                        && dr[columnName] != DBNull.Value)
                    {
                        CSharpHelper.SetValueToProperty(model, prop, dr[columnName]);
                    }
                    else
                    {
                        Dictionary<string, object> values = dr.ToDictionary()
                            .Where(x => x.Key.StartsWith($"{columnName}."))
                            .ToDictionary(
                                x => x.Key.Substring(columnName.Length + 1),
                                x => x.Value);
                        if (values.Any())
                        {
                            CSharpHelper.SetValueToProperty(model, prop, values.ToTypeObject(prop.PropertyType));
                        }
                    }
                }
                return model;
            }
            return null;
        }
        /// <summary>
        /// 將 Dictionary 轉換成自訂 Class 的 object
        /// </summary>
        public static object ToTypeObject<T>(this Dictionary<string, T> dictionary, Type type, bool caseSensitive = false)
        {
            if (dictionary != null)
            {
                object model = type.Assembly.CreateInstance(type.FullName);
                var props = model.GetType().GetProperties();
                var names = dictionary.Keys.ToArray();
                foreach (PropertyInfo prop in props)
                {
                    string columnName = CSharpHelper.GetColumnName(prop);
                    int i = caseSensitive ?
                        Array.IndexOf(names, columnName) :
                        Array.IndexOf(
                            names.Select(x => x.ToLower()).ToArray(),
                            columnName.ToLower());

                    if (i >= 0 && dictionary[names[i]] != null
                        && (dictionary[names[i]] as object) != DBNull.Value)
                    {
                        CSharpHelper.SetValueToProperty(model, prop, dictionary[names[i]]);
                    }
                    else
                    {
                        Dictionary<string, T> values = dictionary
                            .Where(x => x.Key.StartsWith($"{columnName}."))
                            .ToDictionary(
                                x => x.Key.Substring(columnName.Length + 1),
                                x => x.Value);
                        if (values.Any())
                        {
                            CSharpHelper.SetValueToProperty(model, prop, values.ToTypeObject(prop.PropertyType));
                        }
                    }
                }
                return model;
            }
            return null;
        }

        /// <summary>
        /// 以 Left Join 的概念以另一個集合來更新集合的資料
        /// </summary>
        public static IEnumerable<T1> UpdateFrom<T1, T2>(this IEnumerable<T1> source, IEnumerable<T2> updateData, Func<T1, T2, bool> updateWhere, Action<T1, T2> updateAction)
        {
            List<T1> result = new List<T1>();
            foreach (T1 sourceItem in source)
            {
                if (updateData.Any(x => updateWhere(sourceItem, x)))
                {
                    foreach (T2 updateItem in updateData.Where(x => updateWhere(sourceItem, x)))
                    {
                        var resultItem = sourceItem.JsonConvertTo<T1>();
                        updateAction(resultItem, updateItem);
                        result.Add(resultItem);
                    }
                }
                else
                {
                    result.Add(sourceItem.JsonConvertTo<T1>());
                }
            }
            return result;
        }

        /// <summary>
        /// 在 DataTable 建立一個資料欄位, 資料內容以來源資料透過自訂方法生成
        /// </summary>
        public static DataColumn ConvertToNewColumn<T>(this DataColumn source, string newColumnName, Func<object, T> converter)
        {
            DataTable dt = source.Table;
            DataColumn dc = dt.Columns.Add(newColumnName, typeof(T));
            foreach (DataRow dr in dt.Rows)
            {
                dr[newColumnName] = converter(dr[source]);
            }
            return dc;
        }
        /// <summary>
        /// 在 DataTable 建立一個資料欄位, 資料內容以來源資料透過自訂方法生成
        /// </summary>
        public static DataColumn ConvertToNewColumn<T>(this DataTable dt, string sourceColumnName, string newColumnName, Func<object, T> converter)
        {
            DataColumn dc = dt.Columns.Add(newColumnName, typeof(T));
            foreach (DataRow dr in dt.Rows)
            {
                dr[newColumnName] = converter(dr[sourceColumnName]);
            }
            return dc;
        }
        /// <summary>
        /// 在 DataTable 建立一個資料欄位, 資料內容以來源資料透過自訂方法生成
        /// </summary>
        public static DataColumn ConvertToNewColumn<T>(this DataTable dt, string newColumnName, Func<DataRow, T> converter)
        {
            DataColumn dc = dt.Columns.Add(newColumnName, typeof(T));
            foreach (DataRow dr in dt.Rows)
            {
                dr[newColumnName] = converter(dr);
            }
            return dc;
        }

        /// <summary>
        /// 加入一個 Key-Value 到 Dictionary，如果已經有存在 Key 則只修改其值
        /// </summary>
        public static void AddOrUpdate<T1, T2>(this Dictionary<T1, T2> dictionary, T1 key, T2 value)
        {
            if (dictionary.ContainsKey(key) == false)
            {
                dictionary.Add(key, value);
            }
            else
            {
                dictionary[key] = value;
            }
        }
        /// <summary>
        /// 從 Dictionary 取出對應 Key 的值，如果沒有 Key 則傳回指定的預設值
        /// </summary>
        public static T2 GetValueOrDefault<T1, T2>(this Dictionary<T1, T2> dictionary, T1 key, T2 defaultValue = default(T2))
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }
            return defaultValue;
        }

        /// <summary>
        /// 從字串第 N 個字開始尋找第一個符合任一查詢字串的起始位置
        /// </summary>
        public static int IndexOfAny(this string sender, params string[] anyOf)
        {
            return IndexOfAny(sender, 0, anyOf);
        }
        /// <summary>
        /// 在字串中尋找第一個符合任一查詢字串的起始位置
        /// </summary>
        public static int IndexOfAny(this string sender, int startIndex, params string[] anyOf)
        {
            var values = anyOf.Select(x => sender.IndexOf(x, startIndex)).Where(x => x != -1);
            if (values != null && values.Any())
            {
                return values.Min();
            }
            return -1;
        }
        /// <summary>
        /// 找出字串中不為重複字詞出現的第一個位置
        /// </summary>
        public static int IndexOfNot(this string sender, string value, int startIndex = 0)
        {
            int i = startIndex;
            while (sender.IndexOf(value, i) == i)
            {
                i = i + value.Length;
                if (i >= sender.Length)
                {
                    return -1;
                }
            }
            return i;
        }
        /// <summary>
        /// 在陣列中找出第一個不為特定值的位置
        /// </summary>
        public static int IndexOfNot<T>(this T[] array, T value, int startIndex = 0) where T : struct
        {
            if (startIndex.Between(0, array.Length - 1))
            {
                for (int i = startIndex; i < array.Length; i++)
                {
                    if (!array[i].Equals(value))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 取代換行字元
        /// </summary>
        public static string ReplaceEndline(this string sender, string endline)
        {
            return sender.Replace("\r\n", "\n").Replace("\n", endline);
        }

        /// <summary>
        /// 從字串中依照指定格式解析出參數的字典
        /// (重複的參數名稱只取第一個參數的值)
        /// </summary>
        public static Dictionary<string, string> DecryptFormat(this string sender, string format)
        {
            return DecryptFormatMulti(sender, format)?.ToDictionary(x => x.Key, x => x.Value.First());
        }
        /// <summary>
        /// 從字串中依照指定格式解析出參數的字典
        /// (所有參數的值以集合的類型傳回)
        /// </summary>
        public static Dictionary<string, IEnumerable<string>> DecryptFormatMulti(this string sender, string format)
        {
            Dictionary<string, List<string>> args = new Dictionary<string, List<string>>();

            int nIndex0 = 0;
            int vIndex0 = 0;
            while (nIndex0 >= 0)
            {
                int nIndex1 = format.IndexOf("{", nIndex0);
                int nIndex2 = format.IndexOf("}", nIndex1 + 1);
                int nIndex3 = format.IndexOf("{", nIndex2 + 1);
                int nIndex4 = format.IndexOf("}", nIndex3 + 1);
                if ((nIndex1 >= 0 && nIndex2 < 0) || (nIndex3 >= 0 && nIndex4 < 0))
                {
                    // { 與 } 必須成對出現。
                    return null;
                }
                if (nIndex2 + 1 == nIndex3)
                {
                    // 參數間必須有常規字串存在。
                    return null;
                }
                string name = format.Substring(nIndex1 + 1, nIndex2 - nIndex1 - 1);
                string before = "", after = "";
                if (nIndex1 > nIndex0)
                {
                    before = format.Substring(nIndex0, nIndex1 - nIndex0);
                }
                if (nIndex2 < format.Length - 1)
                {
                    if (nIndex3 < 0)
                    {
                        after = format.Substring(nIndex2 + 1);
                    }
                    else
                    {
                        after = format.Substring(nIndex2 + 1, nIndex3 - nIndex2 - 1);
                    }
                }
                nIndex0 = Math.Min(nIndex2 + 1, nIndex3);

                string value = "";
                int vIndex1 = before.Length == 0 ? vIndex0 : sender.IndexOf(before, vIndex0);
                int vIndex2 = after.Length == 0 ? sender.Length : sender.IndexOf(after, vIndex1 + before.Length);
                if (vIndex1 < 0 || vIndex2 < 0)
                {
                    // 找不到常規字串
                    return null;
                }
                if (vIndex2 > vIndex1 + before.Length)
                {
                    value = sender.Substring(vIndex1 + before.Length, vIndex2 - vIndex1 - before.Length);
                }
                vIndex0 = vIndex2;

                if (name.Length > 0)
                {
                    if (args.ContainsKey(name) == false)
                    {
                        args.Add(name, new List<string>());
                    }
                    args[name].Add(value);
                }
            }
            return args.ToDictionary(x => x.Key, x => x.Value.Select(y => y));
        }

        /// <summary>
        /// 取得字串從左邊開始的最多幾個字元
        /// </summary>
        public static string Left(this string sender, int length, string append = null)
        {
            if (sender.Length > length)
            {
                if (append == null)
                {
                    return sender.Substring(0, length);
                }
                else
                {
                    return string.Format("{0}{1}", sender.Substring(0, length), append);
                }
            }
            return sender;
        }
        /// <summary>
        /// 取得字串從右邊開始的最多幾個字元
        /// </summary>
        public static string Right(this string sender, int length, string append = null)
        {
            if (sender.Length > length)
            {
                if (append == null)
                {
                    return sender.Substring(sender.Length - length, length);
                }
                else
                {
                    return string.Format("{0}{1}", sender.Substring(sender.Length - length, length), append);
                }
            }
            return sender;
        }

        /// <summary>
        /// 如果字串第一行是空白的則移除
        /// </summary>
        public static string CutEmptyHead(this string sender)
        {
            if (string.IsNullOrWhiteSpace(sender))
            {
                return string.Empty;
            }
            int index = sender.IndexOf("\n");
            if (index >= 0)
            {
                string head = sender.Left(index);
                if (string.IsNullOrWhiteSpace(head))
                {
                    return sender.Substring(index + 1);
                }
            }
            if (string.IsNullOrWhiteSpace(sender))
            {
                return string.Empty;
            }
            return sender;
        }

        /// <summary>
        /// 去除多行字串左邊起始共同長度的空白字元
        /// </summary>
        public static string DecreaseIndentAllLines(this string sender, int max = 0)
        {
            IEnumerable<string> lines = sender.Replace("\t", "    ").Split('\n');
            int min = lines.Where(x => string.IsNullOrWhiteSpace(x) == false)
                .Min(x => x.IndexOfNot(" "));
            if (max > 0)
            {
                min = Math.Min(min, max);
            }
            IEnumerable<string> newLines = lines.Select(x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                {
                    return string.Empty;
                }
                return x.Substring(min);
            });
            return string.Join("\n", newLines);
        }

        /// <summary>
        /// 將一維陣列轉換成二維陣列
        /// </summary>
        public static T[][] To2D<T>(this T[] arr, int size)
        {
            int len = arr.Length / size + 1;
            T[][] result = new T[len][];

            for (int i = 0; i < len; i++)
            {
                int copySize = Math.Min(size, arr.Length - i * size);
                result[i] = new T[copySize];
                Array.Copy(arr, i * size, result[i], 0, copySize);
            }

            return result;
        }
    }
}
