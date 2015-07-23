using KinectDemoCommon.Model;
using Microsoft.Kinect;
using System;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class BodyStreamMessage : KinectClientMessage
    {
        public SerializableBody[] Bodies { get; set; }
        public BodyStreamMessage(SerializableBody[] bodies)
        {
            Bodies = bodies;
            
        }
    }
}
