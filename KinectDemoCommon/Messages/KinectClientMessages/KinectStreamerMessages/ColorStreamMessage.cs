using System;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class ColorStreamMessage : KinectClientMessage
    {
        public byte[] ColorPixels { get; set; }

        public int[] ColorFrameSize { get; set; }

        public ColorStreamMessage(byte[] colorPixels, int[] colorFrameSize)
        {
            ColorPixels = colorPixels;
            ColorFrameSize = colorFrameSize;
        }
    }
}
