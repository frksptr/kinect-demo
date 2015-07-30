using System;

namespace KinectDemoCommon.Util
{
    [Serializable]
    public class DictionaryItem<K,V>
    {
        public K Key { get; set; }
        public V Value { get; set; }

        public DictionaryItem(K key, V value)
        {
            Key = key;
            Value = value;
        }
    }
}
