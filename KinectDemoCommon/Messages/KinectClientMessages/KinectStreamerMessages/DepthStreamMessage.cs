using System;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class DepthStreamMessage : KinectClientMessage
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
