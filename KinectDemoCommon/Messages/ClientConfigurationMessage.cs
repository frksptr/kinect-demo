
using System;

namespace KinectDemoCommon.Messages
{
    [Serializable]
    public class ClientConfigurationMessage : KinectDemoMessage
    {
        public KinectStreamerConfig Configuration { get; set; }
    }
}
