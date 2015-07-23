using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectDemoCommon.Util
{
    public class SerializableDictionaryBuilder<K, V>
    {
        public SerializableDictionary<K, V> Build(IReadOnlyDictionary<K, V> dictionary)
        {
            SerializableDictionary<K, V> serializableDictionary = new SerializableDictionary<K, V>();
            foreach (var pair in dictionary)
            {
                serializableDictionary.Add(new DictionaryItem<K, V>(pair.Key, pair.Value));
            }
            return serializableDictionary;
        }
    }
}
