using System;
using KinectDemoCommon.Model;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class CalibrationDataMessage : KinectClientMessage
    {
        public SerializableBody CalibrationBody { get; set; }
        public CalibrationDataMessage(SerializableBody body) {
            CalibrationBody = body;
        }
    }
}
