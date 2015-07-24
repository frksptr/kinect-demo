using System;
using KinectDemoCommon.Model;

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
                    PointCloud[i+1] = double.NegativeInfinity;
                    PointCloud[i+2] = double.NegativeInfinity;
                }
                else
                {
                    PointCloud[i] = point.X;
                    PointCloud[i+1] = point.Y;
                    PointCloud[i+2] = point.Z;
                }
                i+=3;
            }
        }
    }
}
