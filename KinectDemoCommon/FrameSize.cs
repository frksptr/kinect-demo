using System;

namespace KinectDemoCommon
{
     [Serializable]
    public class FrameSize
    {
        public FrameSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; set; }
        public int Height { get; set; }
    }
}
