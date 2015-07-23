using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
