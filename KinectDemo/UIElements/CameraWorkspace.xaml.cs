using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.ComponentModel;

namespace KinectDemo
{
    /// <summary>
    /// Interaction logic for CameraWorkspace.xaml
    /// </summary>
    public partial class CameraWorkspace : UserControl
    {

        private KinectSensor kinectSensor = null;

        private MultiSourceFrameReader multiSourceFrameReader = null;

        private FrameDescription colorFrameDescription = null;

        private FrameDescription depthFrameDescription = null;
        
        private WriteableBitmap colorBitmap = null;

        private byte[] colorPixels;

        public int[] depthFrameSize;
        
        public CameraWorkspace(KinectSensor kinectSensor)
        {
            this.kinectSensor = kinectSensor;

            this.multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color);

            this.multiSourceFrameReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            this.colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            this.depthFrameSize = new int[] { this.depthFrameDescription.Width, this.depthFrameDescription.Height };

            this.colorPixels = new byte[this.colorFrameDescription.Width * this.colorFrameDescription.Height];

            this.colorBitmap = new WriteableBitmap(this.colorFrameDescription.Width, this.colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.kinectSensor.Open();

            this.DataContext = this;

            this.InitializeComponent();
        }

        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.multiSourceFrameReader != null)
            {
                this.multiSourceFrameReader.Dispose();
                this.multiSourceFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }



        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {

            int depthWidth = 0;
            int depthHeight = 0;

            DepthFrame depthFrame = null;
            ColorFrame colorFrame = null;

            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            // If the Frame has expired by the time we process this event, return.
            if (multiSourceFrame == null)
            {
                return;
            }

            // We use a try/finally to ensure that we clean up before we exit the function.  
            // This includes calling Dispose on any Frame objects that we may have and unlocking the bitmap back buffer.
            try
            {
                depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
                colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();

                // If any frame has expired by the time we process this event, return.
                // The "finally" statement will Dispose any that are not null.
                if ((depthFrame == null) || (colorFrame == null))
                {
                    return;
                }

                depthWidth = depthFrameDescription.Width;
                depthHeight = depthFrameDescription.Height;

                using (colorFrame)
                {
                    if (colorFrame != null)
                    {
                        FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                        using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                        {
                            this.colorBitmap.Lock();

                            // verify data and write the new color frame data to the display bitmap
                            if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                            {
                                colorFrame.CopyConvertedFrameDataToIntPtr(
                                    this.colorBitmap.BackBuffer,
                                    (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                    ColorImageFormat.Bgra);

                                this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                            }

                            this.colorBitmap.Unlock();
                        }
                    }
                }

                //using (depthFrame)
                //{
                //    if (depthFrame != null)
                //    {

                //        // the fastest way to process the body index data is to directly access 
                //        // the underlying buffer
                //        using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                //        {
                //            // verify data and write the color data to the display bitmap
                //            if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                //                (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                //            {
                //                // Note: In order to see the full range of depth (including the less reliable far field depth)
                //                // we are setting maxDepth to the extreme potential depth threshold
                //                ushort maxDepth = ushort.MaxValue;

                //                // If you wish to filter by reliable depth distance, uncomment the following line:
                //                //// maxDepth = depthFrame.DepthMaxReliableDistance

                //                this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                //                depthFrameProcessed = true;
                //            }
                //        }

                //    }
                //}

            }
            finally
            {
                if (depthFrame != null)
                {
                    depthFrame.Dispose();
                }
                if (colorFrame != null)
                {
                    colorFrame.Dispose();
                }
            }
        }
    }
}
