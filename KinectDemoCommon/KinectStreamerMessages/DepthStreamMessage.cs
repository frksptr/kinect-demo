using System;
using Microsoft.Kinect;

namespace KinectDemoCommon.KinectStreamerMessages
{
    [Serializable]
    public class DepthStreamMessage : KinectStreamerMessage
    {
        public byte[] DepthPixels { get;set; }

        public int[] DepthFrameSize { get; set; }

        public DepthStreamMessage(byte[] depthPixels, int[] depthFrameSize)
        {
            DepthPixels = depthPixels;
            DepthFrameSize = depthFrameSize;
        }
    }
}
