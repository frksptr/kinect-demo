using Microsoft.Kinect;

namespace KinectDemoCommon.KinectStreamerMessages
{
    public class BodyStreamMessage : KinectStreamerMessage
    {
        public Body[] Bodies { get; set; }
        public BodyStreamMessage(Body[] bodies)
        {
            Bodies = bodies;
        }
    }
}
