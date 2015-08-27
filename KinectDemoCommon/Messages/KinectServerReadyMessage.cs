using System;

namespace KinectDemoCommon.Messages.KinectServerMessages
{
    [Serializable]
    public class KinectServerReadyMessage : KinectServerMessage
    {
        public bool Ready { get; set; }
    }
}
