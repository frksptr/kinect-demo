using System;

namespace KinectDemoCommon.KinectStreamerMessages
{
    [Serializable]
    public abstract class KinectStreamerMessage
    {
        public enum KinectStreamerMessageType
        {
            DepthStreamMessage,
            ColorStreamMessage,
            BodyStreamMessage
        }

        public KinectStreamerMessageType Type { get; set; }

        protected KinectStreamerMessage()
        {
            try
            {
                Type = (KinectStreamerMessageType)Enum.Parse(typeof(KinectStreamerMessageType), GetType().Name, false);
            }
            catch (Exception e)
            {
                
                throw e;
            }
            
        }

    }


}
