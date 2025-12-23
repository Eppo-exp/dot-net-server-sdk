using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace eppo_sdk.helpers
{
#if !NET7_0_OR_GREATER
    public static class DictionaryExtensions
    {
        public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary)
        {
            return new ReadOnlyDictionary<TKey, TValue>(dictionary);
        }
    }
#endif


    public static class ConvertExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToHexString(byte[] bytes)
        {
#if NET5_0_OR_GREATER
            return Convert.ToHexString(bytes);
#else
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
#endif
        }
    }
}

#if NETSTANDARD2_0 || NETSTANDARD2_1
namespace System.Collections.Generic
{
    internal static class KeyValuePairExtensions
    {
        public static void Deconstruct<TKey, TValue>(
            this KeyValuePair<TKey, TValue> kvp,
            out TKey key,
            out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}
#endif
