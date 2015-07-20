using System;
using System.Windows;
using System.Windows.Media.Media3D;

namespace KinectDemoCommon.Messages
{
    [Serializable]
    public class WorkspaceMessage : KinectDemoMessage
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public Point[] Vertices { get; set; }

        public Point3D[] PointCloud { get; set; }

        public Point3D[] Vertices3D { get;set; }

    }
}
