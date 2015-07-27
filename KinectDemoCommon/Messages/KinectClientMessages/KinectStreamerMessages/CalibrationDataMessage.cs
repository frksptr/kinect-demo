using KinectDemoCommon.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
