using System;
using System.Collections.Generic;

namespace KinectDemoCommon.Util
{
    [Serializable]
    public class SerializableDictionary<K,V>
    {
        public List<DictionaryItem<K, V>> items { get; set; }

        public SerializableDictionary() {
            items = new List<DictionaryItem<K,V>>();
        }

        public void Add(DictionaryItem<K, V> item)
        {
            items.Add(item);
        }

        public void CopyToDictionary(IDictionary<K,V> dictionary)
        {
            foreach (DictionaryItem<K, V> item in items)
            {
                dictionary.Add(item.Key, item.Value);
            }
            
        }


    }
}
