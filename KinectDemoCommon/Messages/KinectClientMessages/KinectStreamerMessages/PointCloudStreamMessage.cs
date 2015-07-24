using KinectDemoCommon.Model;
using System;

namespace KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages
{
    [Serializable]
    public class PointCloudStreamMessage : KinectClientMessage
    {
        public double[] PointCloud { get; set; }

        public PointCloudStreamMessage(double [] pointCloud)
        {
            PointCloud = pointCloud;
        }

        public PointCloudStreamMessage(NullablePoint3D[] pointCloud)
        {
            PointCloud = new double[pointCloud.Length * 3];
            int i = 0;
            foreach (NullablePoint3D point in pointCloud)
            {
                if (point == null)
                {
                    PointCloud[i] = double.NegativeInfinity;
                    PointCloud[i++] = double.NegativeInfinity;
                    PointCloud[i++] = double.NegativeInfinity;
                }
                else
                {
                    PointCloud[i] = point.X;
                    PointCloud[i] = point.Y;
                    PointCloud[i] = point.Z;
                }
                i++;
            }
        }
    }
}
