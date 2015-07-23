using KinectDemoCommon.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class SmartPointCloudStreamMessage : KinectClientMessage
    {
        public double[] PointCloud { get; set; }

        public SmartPointCloudStreamMessage(double [] pointCloud)
        {
            PointCloud = pointCloud;
        }

        public SmartPointCloudStreamMessage(NullablePoint3D[] pointCloud)
        {
            PointCloud = new double[PointCloud.Length * 3];
            int i = 0;
            foreach (NullablePoint3D point in pointCloud)
            {
                if (point == null)
                {
                    PointCloud[++i] = double.NegativeInfinity;
                    PointCloud[++i] = double.NegativeInfinity;
                    PointCloud[++i] = double.NegativeInfinity;
                }
                else
                {
                    PointCloud[++i] = point.X;
                    PointCloud[++i] = point.Y;
                    PointCloud[++i] = point.Z;
                }
            }
        }
    }
}
