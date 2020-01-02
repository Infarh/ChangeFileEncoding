using System;
using System.Collections.Generic;
using System.Text;

namespace ChangeFileEncoding
{
    internal static class StringExtensions
    {
        public static string TrimInternal(this string Str, int MaxLength = 60, string Placeholder = "...")
        {
            if (Str.Length <= MaxLength) return Str;

            var len = MaxLength - Placeholder.Length;
            var half_len = len / 2;

            var left_str_length = half_len;
            var right_str_length = MaxLength - left_str_length - Placeholder.Length;

            var left_str = Str.Substring(0, left_str_length);
            var right_str = Str.Substring(Str.Length - right_str_length, right_str_length);
            return $"{left_str}{Placeholder}{right_str}";
        }
    }
}
