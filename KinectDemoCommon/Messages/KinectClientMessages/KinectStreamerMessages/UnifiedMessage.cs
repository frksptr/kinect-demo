using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class UnifiedMessage : KinectClientMessage
    {
        BodyStreamMessage BodyStreamMessage { get; set; }
        ColorStreamMessage ColorStreamMessage { get; set; }
        DepthStreamMessage DepthStreamMessage { get; set; }
        PointCloudStreamMessage PointCloudStreamMessage { get; set; }

        public UnifiedMessage()
        {

        }
    }
}
