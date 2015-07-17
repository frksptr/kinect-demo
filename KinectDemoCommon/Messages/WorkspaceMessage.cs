using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Media3D;

namespace KinectDemoCommon.Messages
{
    [Serializable]
    public class WorkspaceMessage : KinectDemoMessage
    {
        public Point[] Vertices { get; set; }

        public Point3D[] PointCloud { get; set; }

        public Point3D[] FittedVertices { get;set; }

    }
}
