using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace KinectDemoSGL
{
    public class KinectStreamerEventArgs
    {
        public WriteableBitmap DepthBitmap { get; set; }

        public WriteableBitmap ColorBitmap { get; set; }

        public Body[] Bodies { get; set; }
        
    }
}
