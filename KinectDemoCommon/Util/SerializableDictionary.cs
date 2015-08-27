using System;
using System.Collections.Generic;

namespace KinectDemoCommon.Util
{
    [Serializable]
    public class SerializableDictionary<K,V>
    {
        public List<DictionaryItem<K, V>> Items { get; set; }

        public SerializableDictionary() {
            Items = new List<DictionaryItem<K,V>>();
        }

        public void Add(DictionaryItem<K, V> item)
        {
            Items.Add(item);
        }

        public void CopyToDictionary(IDictionary<K,V> dictionary)
        {
            foreach (DictionaryItem<K, V> item in Items)
            {
                dictionary.Add(item.Key, item.Value);
            }
            
        }


    }
}
