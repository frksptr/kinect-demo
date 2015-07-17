using System;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class DepthStreamMessage : KinectClientMessage
    {
        public byte[] DepthPixels { get;set; }

        public FrameSize DepthFrameSize { get; set; }

        public DepthStreamMessage(byte[] depthPixels, FrameSize depthFrameSize)
        {
            DepthPixels = depthPixels;
            DepthFrameSize = depthFrameSize;
        }
    }
}
