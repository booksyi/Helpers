using System;
using System.Text.RegularExpressions;

namespace HelpersForFramework
{
    public class ValidateMethod
    {
        /// <summary>
        /// 必填 (不為空字串)
        /// </summary>
        public static Func<string, bool> Required
        {
            get { return x => !string.IsNullOrWhiteSpace(x); }
        }

        /// <summary>
        /// 可以轉換成布林的字串
        /// </summary>
        public static Func<string, bool> IsBoolean
        {
            get { return x => string.IsNullOrWhiteSpace(x) || x.Trim().In("0", "1") || bool.TryParse(x, out bool y); }
        }

        /// <summary>
        /// 可以轉換成整數的字串
        /// </summary>
        public static Func<string, bool> IsInt32
        {
            get { return x => string.IsNullOrWhiteSpace(x) || int.TryParse(x, out int y); }
        }

        /// <summary>
        /// 可以轉換成整數的字串
        /// </summary>
        public static Func<string, bool> IsInt64
        {
            get { return x => string.IsNullOrWhiteSpace(x) || long.TryParse(x, out long y); }
        }

        /// <summary>
        /// 可以轉換成小數的字串
        /// </summary>
        public static Func<string, bool> IsDouble
        {
            get { return x => string.IsNullOrWhiteSpace(x) || double.TryParse(x, out double y); }
        }

        /// <summary>
        /// 可以轉換成日期的字串
        /// </summary>
        public static Func<string, bool> IsDateTime
        {
            get { return x => string.IsNullOrWhiteSpace(x) || DateTime.TryParse(x, out DateTime y); }
        }

        /// <summary>
        /// 指定整數的最小值
        /// </summary>
        public static Func<string, bool> Min(int min)
        {
            return x => string.IsNullOrWhiteSpace(x) || (int.TryParse(x, out int y) && min <= y);
        }
        /// <summary>
        /// 指定整數的最小值
        /// </summary>
        public static Func<string, bool> Min(long min)
        {
            return x => string.IsNullOrWhiteSpace(x) || (long.TryParse(x, out long y) && min <= y);
        }
        /// <summary>
        /// 指定小數的最小值
        /// </summary>
        public static Func<string, bool> Min(double min)
        {
            return x => string.IsNullOrWhiteSpace(x) || (double.TryParse(x, out double y) && min <= y);
        }
        /// <summary>
        /// 指定時間的最小值
        /// </summary>
        public static Func<string, bool> Min(DateTime min)
        {
            return x => string.IsNullOrWhiteSpace(x) || (DateTime.TryParse(x, out DateTime y) && min <= y);
        }

        /// <summary>
        /// 指定整數的最大值
        /// </summary>
        public static Func<string, bool> Max(int max)
        {
            return x => string.IsNullOrWhiteSpace(x) || (int.TryParse(x, out int y) && y <= max);
        }
        /// <summary>
        /// 指定整數的最大值
        /// </summary>
        public static Func<string, bool> Max(long max)
        {
            return x => string.IsNullOrWhiteSpace(x) || (long.TryParse(x, out long y) && y <= max);
        }
        /// <summary>
        /// 指定小數的最大值
        /// </summary>
        public static Func<string, bool> Max(double max)
        {
            return x => string.IsNullOrWhiteSpace(x) || (double.TryParse(x, out double y) && y <= max);
        }
        /// <summary>
        /// 指定時間的最大值
        /// </summary>
        public static Func<string, bool> Max(DateTime max)
        {
            return x => string.IsNullOrWhiteSpace(x) || (DateTime.TryParse(x, out DateTime y) && y <= max);
        }

        /// <summary>
        /// 指定整數的範圍
        /// </summary>
        public static Func<string, bool> Between(int min, int max)
        {
            return x => string.IsNullOrWhiteSpace(x) || (int.TryParse(x, out int y) && min <= y && y <= max);
        }
        /// <summary>
        /// 指定整數的範圍
        /// </summary>
        public static Func<string, bool> Between(long min, long max)
        {
            return x => string.IsNullOrWhiteSpace(x) || (long.TryParse(x, out long y) && min <= y && y <= max);
        }
        /// <summary>
        /// 指定小數的範圍
        /// </summary>
        public static Func<string, bool> Between(double min, double max)
        {
            return x => string.IsNullOrWhiteSpace(x) || (double.TryParse(x, out double y) && min <= y && y <= max);
        }
        /// <summary>
        /// 指定時間的範圍
        /// </summary>
        public static Func<string, bool> Between(DateTime min, DateTime max)
        {
            return x => string.IsNullOrWhiteSpace(x) || (DateTime.TryParse(x, out DateTime y) && min <= y && y <= max);
        }

        /// <summary>
        /// 指定字串的最小長度
        /// </summary>
        public static Func<string, bool> LengthMin(int min)
        {
            return x => string.IsNullOrWhiteSpace(x) || min <= x.Length;
        }
        /// <summary>
        /// 指定字串的最大長度
        /// </summary>
        public static Func<string, bool> LengthMax(int max)
        {
            return x => string.IsNullOrWhiteSpace(x) || x.Length <= max;
        }
        /// <summary>
        /// 指定字串長度的範圍
        /// </summary>
        public static Func<string, bool> LengthBetween(int min, int max)
        {
            return x => string.IsNullOrWhiteSpace(x) || (min <= x.Length && x.Length <= max);
        }

        /// <summary>
        /// 是否符合正規表示法
        /// </summary>
        public static Func<string, bool> Regular(string pattern)
        {
            return x => string.IsNullOrWhiteSpace(x) || new Regex(pattern).IsMatch(x);
        }

        /// <summary>
        /// 是否符合 Email 格式
        /// </summary>
        public static Func<string, bool> IsMail
        {
            get { return Regular(@"^\w+((-\w+)|(\.\w+))*\@[A-Za-z0-9]+((\.|-)[A-Za-z0-9]+)*\.[A-Za-z]+$"); }
        }
    }
}
