using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KinectDemoCommon;
using KinectDemoCommon.KinectStreamerMessages;
using Microsoft.Kinect;

namespace KinectDemoClient
{
    public delegate void KinectStreamerEventHandler(KinectStreamerMessage message);


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

        readonly KinectSensor kinectSensor;

        public CoordinateMapper CoordinateMapper { get; set; }

        DepthFrame depthFrame;

        public FrameDescription DepthFrameDescription { get; set; }

        ColorFrame colorFrame;
        public FrameDescription ColorFrameDescription { get; set; }

        BodyFrame bodyFrame;

        public FrameDescription BodyFrameDescription { get; set; }

        MultiSourceFrame multiSourceFrame;

        MultiSourceFrameReader multiSourceFrameReader;

        Body[] Bodies { get; set; }

        public List<Tuple<JointType, JointType>> Bones { get; set; }

        readonly WriteableBitmap colorBitmap;

        readonly WriteableBitmap depthBitmap;

        byte[] colorPixels;

        readonly byte[] depthPixels;

        ushort[] depthArray;

        const int MapDepthToByte = 8000 / 256;

        readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
        
        public CameraSpacePoint[] FullPointCloud { get; set; }

        private static KinectStreamer kinectStreamer;

        private uint bitmapBackBufferSize = 0;  

        public static KinectStreamer Instance
        {
            get { return kinectStreamer ?? (kinectStreamer = new KinectStreamer()); }
        }

        private KinectStreamer()
        {
            KinectStreamerConfig = new KinectStreamerConfig();

            kinectSensor = KinectSensor.GetDefault();

            CoordinateMapper = kinectSensor.CoordinateMapper;

            multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.Body);

            multiSourceFrameReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            ColorFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;

            DepthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            depthBitmap = new WriteableBitmap(DepthFrameDescription.Width, DepthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            colorBitmap = new WriteableBitmap(ColorFrameDescription.Width, ColorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            bitmapBackBufferSize = (uint)((colorBitmap.BackBufferStride * (colorBitmap.PixelHeight - 1)) + (colorBitmap.PixelWidth * this.bytesPerPixel));

            colorPixels = new byte[ColorFrameDescription.Width * ColorFrameDescription.Height];

            depthPixels = new byte[DepthFrameDescription.Width * DepthFrameDescription.Height];

            depthArray = new ushort[DepthFrameDescription.Width * DepthFrameDescription.Height];

            SetupBody();

            kinectSensor.Open();
        }

        private void SetupBody()
        {
            Bones = new List<Tuple<JointType, JointType>>
            {
                // Torso
                new Tuple<JointType, JointType>(JointType.Head, JointType.Neck),
                new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder),
                new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid),
                new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase),
                new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight),
                new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft),
                new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight),
                new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft),

                // Right arm
                new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight),
                new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight),
                new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight),
                new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight),
                new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight),

                // Left arm
                new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft),
                new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft),
                new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft),
                new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft),
                new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft),

                // Right hip
                new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight),
                new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight),
                new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight),

                // Left hip
                new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft),
                new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft),
                new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft)
            };
        }


        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {

            if (!(KinectStreamerConfig.ProvideBodyData || KinectStreamerConfig.ProvideColorData || KinectStreamerConfig.ProvideDepthData))
            {
                return;
            }

            depthFrame = null;
            colorFrame = null;
            bodyFrame = null;

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

                // Process color stream if needed

                if (KinectStreamerConfig.ProvideColorData)
                {
                    ProcessColorData();
                }

                // Process depth frame if needed

                if (KinectStreamerConfig.ProvideDepthData)
                {
                    ProcessDepthData();
                }

                // Process body data if needed
                if (KinectStreamerConfig.ProvideBodyData)
                {
                    ProcessBodyData();
                }



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

        private void ProcessColorData()
        {
            using (colorFrame)
            {
                if (colorFrame != null)
                {
                    ColorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((ColorFrameDescription.Width == colorBitmap.PixelWidth) &&
                            (ColorFrameDescription.Height == colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                colorBitmap.BackBuffer,
                                (uint)(ColorFrameDescription.Width * ColorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));
                            
                        }

                        colorBitmap.Unlock();
                    }
                }
            }
            if (ColorDataReady != null) ColorDataReady(new ColorStreamMessage(colorPixels));
        }

        private void ProcessDepthData()
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
                        if (((DepthFrameDescription.Width * DepthFrameDescription.Height) ==
                             (depthBuffer.Size / DepthFrameDescription.BytesPerPixel)) &&
                            (DepthFrameDescription.Width == depthBitmap.PixelWidth) &&
                            (DepthFrameDescription.Height == depthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance

                            ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size,
                                depthFrame.DepthMinReliableDistance, maxDepth);
                            depthFrameProcessed = true;
                        }
                    }
                }
            }

            if (depthFrameProcessed)
            {
                RenderDepthPixels();
            }
            if (DepthDataReady != null)
                DepthDataReady(new DepthStreamMessage(depthPixels, new int[]{DepthFrameDescription.Width,DepthFrameDescription.Height}));
        }

        private void ProcessBodyData()
        {
            using (bodyFrame)
            {
                if (bodyFrame != null)
                {
                    if (Bodies == null)
                    {
                        Bodies = new Body[bodyFrame.BodyCount];
                    }
                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(Bodies);
                }
            }
            if (BodyDataReady != null) BodyDataReady(new BodyStreamMessage(Bodies));
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
                depthArray[i] = (ushort)(depth >= minDepth && depth <= maxDepth ? (depth) : 0);
                depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        public CameraSpacePoint[] GenerateFullPointCloud()
        {
            int width = DepthFrameDescription.Width;
            int height = DepthFrameDescription.Height;
            int frameSize = width * height;
            FullPointCloud = new CameraSpacePoint[frameSize];
            DepthSpacePoint[] allDepthSpacePoints = new DepthSpacePoint[frameSize];

            ushort[] depths = new ushort[frameSize];

            for (int i = 0; i < height; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    int index = i * width + j;
                    allDepthSpacePoints[index] = new DepthSpacePoint { X = j, Y = i };
                    FullPointCloud[index] = new CameraSpacePoint();
                    depths[index] = depthArray[index];
                }
            }

            kinectSensor.CoordinateMapper.MapDepthPointsToCameraSpace(allDepthSpacePoints, depths, FullPointCloud);

            return FullPointCloud;
        }
    }
}
