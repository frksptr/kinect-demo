using System;
using KinectDemoCommon.Model;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class ColoredPointCloudStreamMessage : PointCloudStreamMessage
    {
        public byte[] ColorPixels { get; set; }

        public ColoredPointCloudStreamMessage(NullablePoint3D[] points, byte[] colors) : base(points)
        {
            ColorPixels = colors;
        }
    }
}
