﻿using System;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class UnifiedStreamerMessage : KinectClientMessage
    {
        public BodyStreamMessage BodyStreamMessage { get; set; }
        public ColorStreamMessage ColorStreamMessage { get; set; }
        public DepthStreamMessage DepthStreamMessage { get; set; }
        public PointCloudStreamMessage PointCloudStreamMessage { get; set; }
        public ColoredPointCloudStreamMessage ColoredPointCloudStreamMessage { get; set; }

        public UnifiedStreamerMessage(
            BodyStreamMessage bodyStreamMessage,
            ColorStreamMessage colorStreamMessage,
            DepthStreamMessage depthStreamMessage,
            PointCloudStreamMessage pointCloudStreamMessage,
            ColoredPointCloudStreamMessage coloredPointCloudStreamMessage
            )
        {
            BodyStreamMessage = bodyStreamMessage;
            ColorStreamMessage = colorStreamMessage;
            DepthStreamMessage = depthStreamMessage;
            PointCloudStreamMessage = pointCloudStreamMessage;
            ColoredPointCloudStreamMessage = coloredPointCloudStreamMessage;
        }
    }
}
