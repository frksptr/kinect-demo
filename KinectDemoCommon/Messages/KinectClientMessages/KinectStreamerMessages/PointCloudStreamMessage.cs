using KinectDemoCommon.Model;
using System;
using System.Windows.Media.Media3D;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class PointCloudStreamMessage : KinectClientMessage
    {
        public NullablePoint3D[] FullPointCloud { get; set; }

        public PointCloudStreamMessage(NullablePoint3D[] pointCloud)
        {
            FullPointCloud = pointCloud;
        }
    }
}
