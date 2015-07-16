using Microsoft.Kinect;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    public class BodyStreamMessage : KinectClientMessage
    {
        public Body[] Bodies { get; set; }
        public BodyStreamMessage(Body[] bodies)
        {
            Bodies = bodies;
        }
    }
}
