using System;

namespace KinectDemoCommon.Messages
{
    [Serializable]
    public class CalibrationMessage : KinectDemoMessage
    {
        public enum CalibrationMessageEnum
        {
            Start, Stop
        }
        public CalibrationMessageEnum Message { get; set; }
    }


}
