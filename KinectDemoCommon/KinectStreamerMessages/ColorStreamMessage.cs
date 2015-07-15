using System;
namespace KinectDemoCommon.KinectStreamerMessages
{
    [Serializable]
    public class ColorStreamMessage : KinectStreamerMessage
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
