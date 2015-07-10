using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace KinectDemoSGL
{
    public delegate void KinectStreamerEventHandler(object sender, EventArgs e);

    /* Provides depth, color and body data streamed by the Kinect. Each of the streams only get processed
     * if the corresponding flag in the kinectStremerConfig object is true. When the needed stream datas are
     * ready an event is fired.
     */
    class KinectStreamer
    {
        public event KinectStreamerEventHandler BodyDataReady;
        public event KinectStreamerEventHandler ColorDataReady;
        public event KinectStreamerEventHandler DepthDataReady;

        public KinectStreamerConfig KinectStreamerConfig { get; set; }

        KinectSensor kinectSensor;

        DepthFrame depthFrame;

        FrameDescription depthFrameDescription;

        ColorFrame colorFrame;

        FrameDescription colorFrameDescription;

        BodyFrame bodyFrame;

        FrameDescription bodyFrameDescription;

        MultiSourceFrame multiSourceFrame;

        MultiSourceFrameReader multiSourceFrameReader;

        Body[] bodies = null;

        List<Tuple<JointType, JointType>> bones;

        WriteableBitmap colorBitmap = null;

        WriteableBitmap depthBitmap = null;

        byte[] colorPixels = null;

        byte[] depthPixels = null;

        const int MapDepthToByte = 8000 / 256;

        public KinectStreamer(KinectStreamerConfig config)
        {
            this.KinectStreamerConfig = config;

            this.kinectSensor = KinectSensor.GetDefault();

            this.multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.Body);

            this.multiSourceFrameReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            this.colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            setupBody();

            this.kinectSensor.Open();
        }

        private void setupBody()
        {
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));
        }


        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {

            if (!(KinectStreamerConfig.ProvideBodyData || KinectStreamerConfig.ProvideColorData || KinectStreamerConfig.ProvideDepthData))
            {
                return;
            }

            int depthWidth = 0;
            int depthHeight = 0;

            depthFrame = null;
            colorFrame = null;

            multiSourceFrame = e.FrameReference.AcquireFrame();

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
                bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();

                // If any frame has expired by the time we process this event, return.
                // The "finally" statement will Dispose any that are not null.
                if ((depthFrame == null) || (colorFrame == null) || (bodyFrame == null))
                {
                    return;
                }

                depthWidth = depthFrameDescription.Width;
                depthHeight = depthFrameDescription.Height;

                // Process color stream if needed

                if (KinectStreamerConfig.ProvideColorData)
                {
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
                    ColorDataReady(this, null);
                }

                // Process depth frame if needed

                if (KinectStreamerConfig.ProvideDepthData)
                {
                    bool depthFrameProcessed = false;

                    using (depthFrame)
                    {
                        if (depthFrame != null)
                        {
                            // the fastest way to process the body index data is to directly access 
                            // the underlying buffer
                            using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                            {
                                // verify data and write the color data to the display bitmap
                                if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                                    (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                                {
                                    // Note: In order to see the full range of depth (including the less reliable far field depth)
                                    // we are setting maxDepth to the extreme potential depth threshold
                                    ushort maxDepth = ushort.MaxValue;

                                    // If you wish to filter by reliable depth distance, uncomment the following line:
                                    //// maxDepth = depthFrame.DepthMaxReliableDistance

                                    this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                                    depthFrameProcessed = true;
                                }
                            }
                        }
                    }

                    if (depthFrameProcessed)
                    {
                        this.RenderDepthPixels();
                    }
                    DepthDataReady(this, null);
                }

                // Process body data if needed

                if (KinectStreamerConfig.ProvideBodyData)
                {
                    using (bodyFrame)
                    {

                        if (bodyFrame != null)
                        {
                            if (this.bodies == null)
                            {
                                this.bodies = new Body[bodyFrame.BodyCount];
                            }
                            // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                            // As long as those body objects are not disposed and not set to null in the array,
                            // those body objects will be re-used.
                            bodyFrame.GetAndRefreshBodyData(this.bodies);
                        }
                    }
                }
                BodyDataReady(this, null);
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
                if (bodyFrame != null)
                {
                    bodyFrame.Dispose();
                }
            }
        }

        private void RenderDepthPixels()
        {
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);
        }

        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

    }
}
