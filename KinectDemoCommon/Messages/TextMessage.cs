using System;

namespace KinectDemoCommon.Messages
{
    [Serializable]
    public class TextMessage : KinectDemoMessage
    {
        public string Text { get; set; }
    }
}
