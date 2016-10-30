using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster
{
    public static class DictionaryExtensionMethods
    {
        /// <summary>
        /// Pythonic "Dictionary.get()" method.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="def"></param> default value. Optional parameter, defaults to the default type of TValue
        /// <returns></returns>
        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue def = default(TValue))
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            } else
            {
                return def;
            }
        }
    }

    public static class ListExtensionMethods
    {
        /// <summary>
        /// A Pythonic "Add" method.
        /// </summary>
        /// <typeparam name="Ttype"></typeparam>
        /// <param name="list"></param>
        /// <param name="parameters"></param>
        public static void Add<Ttype>(this List<Ttype> list, params Ttype[] parameters)
        {
            foreach(var p in parameters)
            {
                list.Add(p);
            }
        }
    }
}
