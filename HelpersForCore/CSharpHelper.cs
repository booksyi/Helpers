﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace HelpersForCore
{
    public class CSharpHelper
    {
        /// <summary>
        /// 傳回一個隨機順序的陣列
        /// </summary>
        public static T[] Shuffle<T>(params T[] values)
        {
            RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();
            return values.OrderBy(x =>
            {
                byte[] bytes = new byte[4];
                rand.GetBytes(bytes);
                return Convert.ToInt32(bytes[0]);
            }).ToArray();
        }

        /// <summary>
        /// 隨機傳回一個值
        /// </summary>
        public static T Random<T>(params T[] values)
        {
            if (values.Length > 0)
            {
                Random rand = new Random(Guid.NewGuid().GetHashCode());
                int i = rand.Next(0, values.Length);
                return values[i];
            }
            return default(T);
        }

        /// <summary>
        /// 取最大值
        /// </summary>
        public static T Max<T>(params T[] values) where T : struct
        {
            if (values != null && values.Any())
            {
                return values.Max();
            }
            return default(T);
        }
        /// <summary>
        /// 取最小值
        /// </summary>
        public static T Min<T>(params T[] values) where T : struct
        {
            if (values != null && values.Any())
            {
                return values.Min();
            }
            return default(T);
        }

        /// <summary>
        /// 用 params 來建立 Array
        /// </summary>
        public static T[] NewArray<T>(params T[] list)
        {
            return list;
        }
        /// <summary>
        /// 用 params 來建立 List
        /// </summary>
        public static List<T> NewList<T>(params T[] list)
        {
            return list.ToList();
        }
        /// <summary>
        /// 取得一個從 A 到 B 的整數集合
        /// </summary>
        public static IEnumerable<int> GetFromAToB(int a, int b, int span = 1)
        {
            List<int> list = new List<int>();
            if (span == 0)
            {
                throw new ArgumentException("參數 span 不能為 0");
            }
            if ((a > b && span > 0) || (a < b && span < 0))
            {
                span = span * -1;
            }
            for (int i = a; (i <= b && span > 0) || (i >= b && span < 0); i += span)
            {
                list.Add(i);
            }
            return list;
        }

        /// <summary>
        /// 將字串經由 SHA512 編碼後傳回
        /// </summary>
        public static string GenerateSHA512String(string inputString)
        {
            SHA512 sha512 = SHA512.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(inputString);
            byte[] hash = sha512.ComputeHash(bytes);
            return GetStringFromHash(hash);
        }
        private static string GetStringFromHash(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString();
        }

        /// <summary>
        /// 將參數帶入 function 處理
        /// (解決參數是一個複雜的運算時，在 function 的多次呼叫不用重新運算)
        /// </summary>
        public static void Using<T>(T value, Action<T> func)
        {
            func(value);
        }
        /// <summary>
        /// 將參數帶入 function 處理
        /// (解決參數是一個複雜的運算時，在 function 的多次呼叫不用重新運算)
        /// </summary>
        public static T2 Using<T1, T2>(T1 value, Func<T1, T2> func)
        {
            return func(value);
        }
        /// <summary>
        /// 將參數帶入 condition 執行結果為 true 再將參數帶入 function 處理
        /// </summary>
        public static void Using<T>(T value, Func<T, bool> condition, Action<T> func)
        {
            if (condition == null)
            {
                Using(value, func);
            }
            else if (condition(value))
            {
                func(value);
            }
        }
        /// <summary>
        /// 將參數帶入 condition 執行結果為 true 再將參數帶入 function 處理
        /// </summary>
        public static T2 Using<T1, T2>(T1 value, Func<T1, bool> condition, Func<T1, T2> func)
        {
            if (condition == null)
            {
                return Using(value, func);
            }
            else if (condition(value))
            {
                return func(value);
            }
            return default(T2);
        }

        #region Try-Catch
        /// <summary>
        /// 執行 func 如果正常執行完畢呼叫 todo, 如果發生錯誤則呼叫 exception
        /// </summary>
        public static void Try(Action func, Action todo = null, Action exception = null)
        {
            Try(func, todo, (Exception e) => { if (exception != null) exception(); });
        }
        /// <summary>
        /// 執行 func 如果正常執行完畢呼叫 todo, 如果發生錯誤則呼叫 exception
        /// (exception 的參數為 Exception)
        /// </summary>
        public static void Try<E>(Action func, Action todo, Action<E> exception) where E : Exception
        {
            Try(func, () => { if (todo != null) todo(); return 0; }, (E e) => { exception(e); return 0; });
        }
        /// <summary>
        /// 執行 func 如果正常執行完畢呼叫 todo, 如果發生錯誤則呼叫 exception
        /// (todo 的參數為 func 的回傳值)
        /// </summary>
        public static void Try<T>(Func<T> func, Action<T> todo, Action exception = null)
        {
            Try(func, todo, (Exception e) => { if (exception != null) exception(); });
        }
        /// <summary>
        /// 執行 func 如果正常執行完畢呼叫 todo, 如果發生錯誤則呼叫 exception
        /// (todo 的參數為 func 的回傳值, exception 的參數為 Exception)
        /// </summary>
        public static void Try<T, E>(Func<T> func, Action<T> todo, Action<E> exception) where E : Exception
        {
            Try(func, x => { todo(x); return 0; }, (E e) => { exception(e); return 0; });
        }
        /// <summary>
        /// 執行 func 如果正常執行完畢呼叫 todo, 如果發生錯誤則呼叫 exception
        /// (todo, exception 各自需回傳結果)
        /// </summary>
        public static T Try<T>(Action func, Func<T> todo, Func<T> exception = null)
        {
            return Try(func, todo, (Exception e) => { if (exception != null) return exception(); return default(T); });
        }
        public static T Try<T, E>(Action func, Func<T> todo, Func<E, T> exception) where E : Exception
        {
            if (func == null) throw new ArgumentException("func 不能為 null");
            return Try(() => { func(); return 0; }, x => { if (todo != null) return todo(); return default(T); }, exception);
        }
        public static T2 Try<T1, T2>(Func<T1> func, Func<T1, T2> todo, Func<T2> exception)
        {
            return Try(func, todo, (Exception e) => { if (exception != null) return exception(); return default(T2); });
        }
        public static T2 Try<T1, T2, E>(Func<T1> func, Func<T1, T2> todo, Func<E, T2> exception) where E : Exception
        {
            if (func == null) throw new ArgumentException("func 不能為 null");
            T1 x = default(T1);
            try
            {
                x = func();
            }
            catch (E e)
            {
                return exception(e);
            }
            return todo(x);
        }

        /// <summary>
        /// 執行 func 如果發生錯誤則呼叫 exception
        /// </summary>
        public static void Catch(Action func, Action exception = null)
        {
            Try(func, null, exception);
        }
        public static void Catch<E>(Action func, Action<E> exception) where E : Exception
        {
            Try(func, null, exception);
        }
        public static T Catch<T>(Func<T> func, Func<T> exception = null)
        {
            return Try(func, x => x, exception);
        }
        public static T Catch<T, E>(Func<T> func, Func<E, T> exception) where E : Exception
        {
            return Try(func, x => x, exception);
        }
        #endregion

        /// <summary>
        /// 取得指定 Class 的 List
        /// </summary>
        public static object GetTypeList(Type type, IEnumerable<object> list)
        {
            if (type == typeof(bool))
            {
                return list.Select(x => (bool)x).ToList();
            }
            if (type == typeof(short))
            {
                return list.Select(x => (short)x).ToList();
            }
            if (type == typeof(int))
            {
                return list.Select(x => (int)x).ToList();
            }
            if (type == typeof(long))
            {
                return list.Select(x => (long)x).ToList();
            }
            if (type == typeof(float))
            {
                return list.Select(x => (float)x).ToList();
            }
            if (type == typeof(double))
            {
                return list.Select(x => (double)x).ToList();
            }
            if (type == typeof(decimal))
            {
                return list.Select(x => (decimal)x).ToList();
            }
            if (type == typeof(DateTime))
            {
                return list.Select(x => (DateTime)x).ToList();
            }
            if (type == typeof(string))
            {
                return list.Select(x => (string)x).ToList();
            }
            return list.ToList();
        }

        /// <summary>
        /// 取得指定 Class 的 Array
        /// </summary>
        public static object GetTypeArray(Type type, IEnumerable<object> array)
        {
            if (type == typeof(bool))
            {
                return array.Select(x => (bool)x).ToArray();
            }
            if (type == typeof(short))
            {
                return array.Select(x => (short)x).ToArray();
            }
            if (type == typeof(int))
            {
                return array.Select(x => (int)x).ToArray();
            }
            if (type == typeof(long))
            {
                return array.Select(x => (long)x).ToArray();
            }
            if (type == typeof(float))
            {
                return array.Select(x => (float)x).ToArray();
            }
            if (type == typeof(double))
            {
                return array.Select(x => (double)x).ToArray();
            }
            if (type == typeof(decimal))
            {
                return array.Select(x => (decimal)x).ToArray();
            }
            if (type == typeof(DateTime))
            {
                return array.Select(x => (DateTime)x).ToArray();
            }
            if (type == typeof(string))
            {
                return array.Select(x => (string)x).ToArray();
            }
            return array.ToArray();
        }

        /// <summary>
        /// 將物件轉換成可以設定到指定型別的物件
        /// </summary>
        public static object GetTypeValue<T>(Type type, T value)
        {
            if (type == value.GetType())
            {
                return value;
            }
            if (type.IsGenericType)
            {
                Type elemType = type.GetGenericArguments().Single();
                if ((type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    || type.GetGenericTypeDefinition() == typeof(List<>))
                    && value is string)
                {
                    return GetTypeList(elemType, Convert.ToString(value).Split(',').Select(x => GetTypeValue(elemType, x)));
                }
                return GetTypeValue(elemType, value);
            }
            if (type.IsEnum)
            {
                if (value is bool)
                {
                    return Enum.Parse(type, Convert.ToBoolean(value) ? "1" : "0");
                }
                if (value is string)
                {
                    foreach (var field in type.GetFields())
                    {
                        var attr = field.GetCustomAttribute<EnumMemberAttribute>();
                        if (attr != null)
                        {
                            if (Convert.ToString(value) == attr.Value)
                            {
                                return field.GetValue(null);
                            }
                        }
                    }
                }
                return Enum.Parse(type, Convert.ToString(value));
            }
            if (type == typeof(bool))
            {
                if (value is sbyte || value is short || value is int || value is long
                    || value is byte || value is ushort || value is int || value is ulong)
                {
                    return Convert.ToInt32(value) == 1;
                }
                if (value is string)
                {
                    if (Convert.ToString(value) == "1" || Convert.ToString(value).ToLower() == "true")
                    {
                        return true;
                    }
                    if (Convert.ToString(value) == "0" || Convert.ToString(value).ToLower() == "false")
                    {
                        return false;
                    }
                }
            }
            return Convert.ChangeType(value, type);
        }
        /// <summary>
        /// 設定物件的屬性值
        /// </summary>
        public static void SetValueToProperty<T>(object @object, PropertyInfo prop, T value)
        {
            prop.SetValue(@object, GetTypeValue(prop.PropertyType, value));
        }

        /// <summary>
        /// 取得 Table 標籤的 Name 值 (若無 Table 標籤而回傳類型名稱)
        /// </summary>
        public static string GetTableName<T>()
        {
            var type = typeof(T);
            if (type.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() is TableAttribute attr)
            {
                return attr.Name;
            }
            return type.Name;
        }
        /// <summary>
        /// 取得所有不包含 NotMapped 標籤的屬性
        /// </summary>
        public static PropertyInfo[] GetMappedProperties<T>()
        {
            return typeof(T).GetProperties().Where(x => x.GetCustomAttribute<NotMappedAttribute>() == null).ToArray();
        }
        /// <summary>
        /// 取得 Column 標籤的 Name 值 (若無 Column 標籤而回傳屬性名稱)
        /// </summary>
        public static string GetColumnName(PropertyInfo property)
        {
            var attr = property.GetCustomAttribute<ColumnAttribute>();
            if (attr != null)
            {
                return attr.Name;
            }
            return property.Name;
        }
        /// <summary>
        /// 取得第一個包含 DatabaseGenerated 標籤且 DatabaseGeneratedOption 設為 Identity 的對應欄位名稱
        /// </summary>
        public static string GetIdentityColumnName<T>()
        {
            foreach (var prop in GetMappedProperties<T>())
            {
                var attr = prop.GetCustomAttribute<DatabaseGeneratedAttribute>();
                if (attr != null)
                {
                    if (attr.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                    {
                        return GetColumnName(prop);
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 取得第一個包含 DatabaseGenerated 標籤且 DatabaseGeneratedOption 設為 Identity 的成員
        /// </summary>
        public static PropertyInfo GetIdentityProperty<T>()
        {
            foreach (var prop in GetMappedProperties<T>())
            {
                var attr = prop.GetCustomAttribute<DatabaseGeneratedAttribute>();
                if (attr != null)
                {
                    if (attr.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                    {
                        return prop;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 建立一個 Timer 來執行特定的動作
        /// </summary>
        public static System.Timers.Timer SetInterval(Action function, double milliseconds, int times = 0)
        {
            if (function != null && milliseconds > 0)
            {
                int i = 0;
                System.Timers.Timer timer = new System.Timers.Timer(milliseconds);
                timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
                {
                    i++;
                    bool stop = (times > 0 && i >= times);
                    if (stop)
                    {
                        timer.Stop();
                    }
                    function();
                    if (stop)
                    {
                        timer.Dispose();
                        timer = null;
                    }
                };
                timer.Start();
                return timer;
            }
            return null;
        }
        /// <summary>
        /// 建立一個 Timer 來執行特定的動作 (只執行一次)
        /// </summary>
        public static System.Timers.Timer SetTimeout(Action function, double milliseconds)
        {
            if (milliseconds > 0)
            {
                System.Timers.Timer timer = new System.Timers.Timer(milliseconds);
                timer.Elapsed += delegate (object sender, System.Timers.ElapsedEventArgs e)
                {
                    timer.Stop();
                    function();
                    timer.Dispose();
                    timer = null;
                };
                timer.Start();
                return timer;
            }
            return null;
        }
        /// <summary>
        /// 執行第一個動作如果超過指定時間，則觸發第二個動作
        /// </summary>
        public static void SetTimeout(Action function1, Action function2, double milliseconds)
        {
            if (function1 != null)
            {
                System.Timers.Timer timer = SetTimeout(function2, milliseconds);
                function1();
                if (timer != null && timer.Enabled)
                {
                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                }
            }
        }

        /// <summary>
        /// 執行 function 並回傳執行時間 (毫秒)
        /// </summary>
        public static double RunWithTiming(Action function)
        {
            DateTime start = DateTime.Now;
            function();
            DateTime stop = DateTime.Now;
            return (stop - start).TotalMilliseconds;
        }

        /// <summary>
        /// 取得這週星期日的日期
        /// </summary>
        public static DateTime GetTheSundayDate(DateTime d)
        {
            return d.AddDays((int)d.DayOfWeek * -1);
        }

        /// <summary>
        /// 將多個 Dictionary 合併成一個 Dictionary, 第一個參數可自訂 Key 衝突時的解決方法
        /// </summary>
        public static Dictionary<T1, T2> Merge<T1, T2>(Func<T1, int, T1> keyFommatter, params Dictionary<T1, T2>[] dictionaries)
        {
            Dictionary<T1, T2> result = new Dictionary<T1, T2>();
            Dictionary<T1, int> indexes = new Dictionary<T1, int>();
            foreach (var dictionary in dictionaries)
            {
                foreach (var keyValue in dictionary)
                {
                    T1 key = keyValue.Key;
                    if (!indexes.ContainsKey(key))
                    {
                        indexes.Add(key, 0);
                    }
                    int index = indexes[key]++;
                    if (keyFommatter != null)
                    {
                        key = keyFommatter(key, index);
                    }
                    result.Add(key, keyValue.Value);
                }
            }
            return result;
        }

        public static string ProcessStart(string fileName, string[] arguments)
        {
            string result = "";
            using (Process process = new Process())
            {
                StringBuilder builder = new StringBuilder();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = string.Join(" ", arguments.Select(x => $"\"{x.Replace("\\", "\\\\").Replace("\"", "\\\"")}\""));
                process.StartInfo.RedirectStandardOutput = true;
                process.OutputDataReceived += (sender, e) => builder.Append(e.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                result = builder.ToString();
            }
            return result;
        }

        public static T ProcessStart<T>(string fileName, string[] arguments, Func<string, T> converter = null)
        {
            if (converter == null)
            {
                converter = JsonConvert.DeserializeObject<T>;
            }
            return converter(ProcessStart(fileName, arguments));
        }
    }
}
