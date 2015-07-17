using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class PointCloudStreamMessage : KinectClientMessage
    {
        public Point3D[] FullPointCloud { get; set; }

        public PointCloudStreamMessage(Point3D[] pointCloud)
        {
            FullPointCloud = pointCloud;
        }
    }
}
