using System;
using System.Windows;

namespace KinectDemoCommon.Messages
{
    [Serializable]
    public class WorkspaceMessage : KinectDemoMessage
    {
        public Point[] Vertices { get; set; }
    }
}
