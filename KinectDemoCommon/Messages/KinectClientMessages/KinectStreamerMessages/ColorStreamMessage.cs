using System;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class ColorStreamMessage : KinectClientMessage
    {
        public byte[] ColorPixels { get; set; }

        public FrameSize ColorFrameSize { get; set; }

        public ColorStreamMessage(byte[] colorPixels, FrameSize colorFrameSize)
        {
            ColorPixels = colorPixels;
            ColorFrameSize = colorFrameSize;
        }
    }
}
