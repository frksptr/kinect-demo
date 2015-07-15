namespace KinectDemoCommon.KinectStreamerMessages
{
    public class ColorStreamMessage : KinectStreamerMessage
    {
        public byte[] ColorPixels { get; set; }

        public ColorStreamMessage(byte[] colorPixels)
        {
            ColorPixels = colorPixels;
        }
    }
}
