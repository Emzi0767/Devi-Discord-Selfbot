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
    }
}