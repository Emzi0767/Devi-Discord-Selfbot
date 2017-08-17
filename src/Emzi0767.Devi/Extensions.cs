using System;
using System.Collections.Generic;

namespace Emzi0767.Devi
{
    public static class Extensions
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }

        public static string ToBase64(this byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        public static byte[] FromBase64(this string data)
        {
            return Convert.FromBase64String(data);
        }
    }
}