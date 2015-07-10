using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace KinectDemoSGL
{
    public delegate void KinectStreamerEventHandler(object sender, KinectStreamerEventArgs e);


    /* Provides depth, color and body data streamed by the Kinect. Each of the streams only get processed
     * if the corresponding flag in the kinectStremerConfig object is true. When the needed stream datas are
     * ready an event is fired.
     */
    // Singleton
    class KinectStreamer
    {
        public event KinectStreamerEventHandler BodyDataReady;
        public event KinectStreamerEventHandler ColorDataReady;
        public event KinectStreamerEventHandler DepthDataReady;

        public KinectStreamerConfig KinectStreamerConfig { get; set; }

        KinectSensor kinectSensor;

        DepthFrame depthFrame;

        public FrameDescription DepthFrameDescription { get; set; }

        ColorFrame colorFrame;

        FrameDescription colorFrameDescription;

        BodyFrame bodyFrame;

        FrameDescription bodyFrameDescription;

        MultiSourceFrame multiSourceFrame;

        MultiSourceFrameReader multiSourceFrameReader;

        Body[] bodies;

        List<Tuple<JointType, JointType>> bones;

        WriteableBitmap colorBitmap = null;

        WriteableBitmap depthBitmap = null;

        byte[] colorPixels = null;

        byte[] depthPixels = null;

        const int MapDepthToByte = 8000 / 256;

        private static KinectStreamer kinectStreamer;
        public static KinectStreamer Instance
        {
            get { return kinectStreamer ?? (kinectStreamer = new KinectStreamer()); }
        }

        private KinectStreamer()
        {
            KinectStreamerConfig = new KinectStreamerConfig();

            kinectSensor = KinectSensor.GetDefault();

            multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.Body);

            multiSourceFrameReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            colorFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;

            DepthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            SetupBody();

            kinectSensor.Open();
        }

        private void SetupBody()
        {
            bones = new List<Tuple<JointType, JointType>>();

            // Torso
            bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));
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

                depthWidth = DepthFrameDescription.Width;
                depthHeight = DepthFrameDescription.Height;

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
                                colorBitmap.Lock();

                                // verify data and write the new color frame data to the display bitmap
                                if ((colorFrameDescription.Width == colorBitmap.PixelWidth) && (colorFrameDescription.Height == colorBitmap.PixelHeight))
                                {
                                    colorFrame.CopyConvertedFrameDataToIntPtr(
                                        colorBitmap.BackBuffer,
                                        (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                        ColorImageFormat.Bgra);

                                    colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));
                                }

                                colorBitmap.Unlock();
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
                            using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                            {
                                // verify data and write the color data to the display bitmap
                                if (((DepthFrameDescription.Width * DepthFrameDescription.Height) == (depthBuffer.Size / DepthFrameDescription.BytesPerPixel)) &&
                                    (DepthFrameDescription.Width == depthBitmap.PixelWidth) && (DepthFrameDescription.Height == depthBitmap.PixelHeight))
                                {
                                    // Note: In order to see the full range of depth (including the less reliable far field depth)
                                    // we are setting maxDepth to the extreme potential depth threshold
                                    ushort maxDepth = ushort.MaxValue;

                                    // If you wish to filter by reliable depth distance, uncomment the following line:
                                    //// maxDepth = depthFrame.DepthMaxReliableDistance

                                    ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                                    depthFrameProcessed = true;
                                }
                            }
                        }
                    }

                    if (depthFrameProcessed)
                    {
                        RenderDepthPixels();
                    }
                    DepthDataReady(this, new KinectStreamerEventArgs
                    {
                        DepthBitmap = depthBitmap
                    });
                }

                // Process body data if needed

                if (KinectStreamerConfig.ProvideBodyData)
                {
                    using (bodyFrame)
                    {

                        if (bodyFrame != null)
                        {
                            if (bodies == null)
                            {
                                bodies = new Body[bodyFrame.BodyCount];
                            }
                            // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                            // As long as those body objects are not disposed and not set to null in the array,
                            // those body objects will be re-used.
                            bodyFrame.GetAndRefreshBodyData(bodies);
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
            depthBitmap.WritePixels(
                new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight),
                depthPixels,
                depthBitmap.PixelWidth,
                0);
        }

        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / DepthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

    }
}
