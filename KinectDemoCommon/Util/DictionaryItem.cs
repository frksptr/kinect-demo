using System;

namespace KinectDemoCommon.Util
{
    [Serializable]
    public class DictionaryItem<TKey,TValue> 
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }

        public DictionaryItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
