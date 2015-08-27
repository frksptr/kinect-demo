using System.ComponentModel;

namespace KinectDemoCommon.Util
{
    public class ObservableKeyValuePair<TKey,TValue>
    {
        private TKey key;

        public TKey Key
        {
            get { return key; }
            set
            {
                if (!value.Equals(key))
                {
                    key = value;
                    NotifyPropertyChanged("Key");
                }
            }
        }

        private TValue value;
        public TValue Value
        {
            get { return value; }
            set
            {
                if (!value.Equals(this.value))
                {
                    this.value = value;
                    NotifyPropertyChanged("Value");

                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
