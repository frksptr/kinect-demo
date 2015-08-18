using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KinectDemoCommon;
using KinectDemoCommon.Messages.KinectClientMessages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using Microsoft.Kinect;

namespace KinectDemoClient
{
    public delegate void KinectStreamerEventHandler(KinectClientMessage message);

    ///
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
        public event KinectStreamerEventHandler PointCloudDataReady;
        public event KinectStreamerEventHandler ColoredPointCloudDataReady;
        public event KinectStreamerEventHandler UnifiedDataReady;
        public event KinectStreamerEventHandler CalibrationDataReady;

        private KinectStreamerConfig kinectStreamerConfig;

        public KinectStreamerConfig KinectStreamerConfig
        {
            get { return kinectStreamerConfig; }
            set
            {
                kinectStreamerConfig = value;

            }
        }

        public NullablePoint3D[] FullPointCloud { get; set; }

        KinectSensor kinectSensor;

        public CoordinateMapper CoordinateMapper { get; set; }

        DepthFrame depthFrame;

        public FrameDescription DepthFrameDescription { get; set; }

        ColorFrame colorFrame;
        public FrameDescription ColorFrameDescription { get; set; }

        BodyFrame bodyFrame;

        public FrameDescription BodyFrameDescription { get; set; }

        MultiSourceFrame multiSourceFrame;

        Body[] Bodies { get; set; }

        public List<Tuple<JointType, JointType>> Bones { get; set; }

        WriteableBitmap colorBitmap;

        WriteableBitmap depthBitmap;

        byte[] colorPixels;

        public byte[] DepthPixels { get; set; }

        ushort[] depthArray;

        const int MapDepthToByte = 8000 / 256;

        private static KinectStreamer kinectStreamer;

        public WorkspaceChecker WorkspaceChecker { get; set; }

        private CameraSpacePoint[] pointCloudCandidates;

        private DepthSpacePoint[] allDepthSpacePoints;

        private DepthStreamMessage depthStreamMessage;
        private ColorStreamMessage colorStreamMessage;
        private BodyStreamMessage bodyStreamMessage;
        private PointCloudStreamMessage pointCloudStreamMessage;
        private CalibrationDataMessage calibrationDataMessage;
        private ColoredPointCloudStreamMessage coloredPointCloudStreamMessage;

        public static KinectStreamer Instance
        {
            get { return kinectStreamer ?? (kinectStreamer = new KinectStreamer()); }
        }

        private KinectStreamer()
        {
            KinectStreamerConfig = new KinectStreamerConfig();
            SetupKinectSensor();

            SetupBody();

            SetupHelpArrays();

            kinectSensor.Open();
        }

        private void SetupHelpArrays()
        {
            int width = DepthFrameDescription.Width;
            int height = DepthFrameDescription.Height;
            int frameSize = width * height;

            pointCloudCandidates = new CameraSpacePoint[frameSize];
            allDepthSpacePoints = new DepthSpacePoint[frameSize];

            for (int i = 0; i < height; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    int index = i * width + j;
                    allDepthSpacePoints[index] = new DepthSpacePoint { X = j, Y = i };
                    pointCloudCandidates[index] = new CameraSpacePoint();
                }
            }
        }

        private void SetupKinectSensor()
        {
            kinectSensor = KinectSensor.GetDefault();

            CoordinateMapper = kinectSensor.CoordinateMapper;

            MultiSourceFrameReader multiSourceFrameReader =
                kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.Body);

            multiSourceFrameReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            ColorFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;

            DepthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            depthBitmap = new WriteableBitmap(DepthFrameDescription.Width, DepthFrameDescription.Height, 96.0, 96.0,
                PixelFormats.Gray8, null);

            colorBitmap = new WriteableBitmap(ColorFrameDescription.Width, ColorFrameDescription.Height, 96.0, 96.0,
                PixelFormats.Bgr32, null);

            colorPixels = new byte[ColorFrameDescription.Width * ColorFrameDescription.Height * 4];

            DepthPixels = new byte[DepthFrameDescription.Width * DepthFrameDescription.Height];

            depthArray = new ushort[DepthFrameDescription.Width * DepthFrameDescription.Height];
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

            if (!(KinectStreamerConfig.StreamBodyData ||
                KinectStreamerConfig.StreamColorData ||
                KinectStreamerConfig.StreamDepthData ||
                KinectStreamerConfig.StreamPointCloudData ||
                KinectStreamerConfig.ProvideCalibrationData ||
                KinectStreamerConfig.StreamColoredPointCloudData
                ))
            {
                return;
            }

            depthFrame = null;
            colorFrame = null;
            bodyFrame = null;

            bodyStreamMessage = null;
            colorStreamMessage = null;
            pointCloudStreamMessage = null;
            depthStreamMessage = null;
            calibrationDataMessage = null;
            coloredPointCloudStreamMessage = null;

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
                //Debug.Write(colorFrame.RelativeTime.ToString());
                bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();

                // If any frame has expired by the time we process this event, return.
                // The "finally" statement will Dispose any that are not null.
                if ((depthFrame == null) || (colorFrame == null) || (bodyFrame == null))
                {
                    return;
                }

                // Process color stream if needed

                if (KinectStreamerConfig.StreamColorData || KinectStreamerConfig.StreamColoredPointCloudData)
                {
                    ProcessColorData();
                }

                // Process depth frame if needed

                if (KinectStreamerConfig.StreamDepthData || KinectStreamerConfig.StreamPointCloudData || KinectStreamerConfig.StreamColoredPointCloudData)
                {
                    ProcessDepthData();

                    if (KinectStreamerConfig.StreamPointCloudData || KinectStreamerConfig.StreamColoredPointCloudData)
                    {
                        GenerateFullPointCloud();
                    }
                }

                if (KinectStreamerConfig.StreamColoredPointCloudData)
                {
                    ProcessPointCloudColors();
                }

                // Process body data if needed
                if (KinectStreamerConfig.StreamBodyData || KinectStreamerConfig.ProvideCalibrationData)
                {
                    ProcessBodyData();
                }

                SendData();
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

        private void ProcessPointCloudColors()
        {
            byte[] pointCloudColors = new byte[depthArray.Length * 4];
            ColorSpacePoint[] colorSpacePoints = new ColorSpacePoint[depthArray.Length];
            var colorWidth = ColorFrameDescription.Width;
            var colorHeight = ColorFrameDescription.Height;

            CoordinateMapper.MapDepthFrameToColorSpace(depthArray, colorSpacePoints);
            float maxX = 0;
            float maxY = 0;

            for (int i = 0; i < depthArray.Length; i++)
            {
                var point = colorSpacePoints[i];
                if (GeometryHelper.IsValidPoint(point))
                {
                    maxX = point.X > maxX ? point.X : maxX;
                    maxY = point.Y > maxY ? point.Y : maxY;

                    int colorX = (int) Math.Floor(point.X + 0.5);
                    int colorY = (int)Math.Floor(point.Y + 0.5);

                    if ((colorX > 0 && colorX < colorWidth) && (colorY > 0 && colorY < colorHeight))
                    {
                        
                        int index = (int)(((colorWidth*colorY)+colorX) * 4);
                        pointCloudColors[i * 4] = colorPixels[index];
                        pointCloudColors[i * 4 + 1] = colorPixels[index + 1];
                        pointCloudColors[i * 4 + 2] = colorPixels[index + 2];
                        pointCloudColors[i * 4 + 3] = colorPixels[index + 3];
                    }


                    //int index = ((int)point.X + DepthFrameDescription.Width / 2) +
                    //            ((int)point.Y + DepthFrameDescription.Height / 2) * DepthFrameDescription.Width;

                    //int index = (int)((point.X + DepthFrameDescription.Width / 2) * ColorFrameDescription.BytesPerPixel +
                    //    (point.Y+ DepthFrameDescription.Height/2) * stride);

                }
            }
            coloredPointCloudStreamMessage = new ColoredPointCloudStreamMessage(FullPointCloud, pointCloudColors);
        }

        private void SendData()
        {
            if (KinectStreamerConfig.SendAsOne)
            {
                if (UnifiedDataReady != null)
                {
                    UnifiedDataReady(new UnifiedStreamerMessage(bodyStreamMessage, colorStreamMessage, depthStreamMessage,
                        pointCloudStreamMessage, coloredPointCloudStreamMessage));
                }
            }
            else
            {
                if (BodyDataReady != null && bodyStreamMessage != null)
                {
                    BodyDataReady(bodyStreamMessage);
                }
                if (ColorDataReady != null && colorStreamMessage != null)
                {
                    ColorDataReady(colorStreamMessage);
                }
                if (DepthDataReady != null && depthStreamMessage != null)
                {
                    DepthDataReady(depthStreamMessage);
                }
                if (PointCloudDataReady != null && pointCloudStreamMessage != null)
                {
                    PointCloudDataReady(pointCloudStreamMessage);
                }
                if (ColoredPointCloudDataReady != null && coloredPointCloudStreamMessage != null)
                {
                    ColoredPointCloudDataReady(coloredPointCloudStreamMessage);
                }
                if (CalibrationDataReady != null && calibrationDataMessage != null)
                {
                    CalibrationDataReady(calibrationDataMessage);
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

                    using (colorFrame.LockRawImageBuffer())
                    {
                        colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((ColorFrameDescription.Width == colorBitmap.PixelWidth) &&
                            (ColorFrameDescription.Height == colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToArray(
                                colorPixels,
                                ColorImageFormat.Bgra);
                        }
                        colorBitmap.Unlock();
                    }
                }
            }

            colorStreamMessage = new ColorStreamMessage(colorPixels,
                new FrameSize(ColorFrameDescription.Width, ColorFrameDescription.Height));

        }

        private void ProcessDepthData()
        {
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
                        }
                    }
                }
            }
            depthStreamMessage = new DepthStreamMessage(DepthPixels, new FrameSize(DepthFrameDescription.Width, DepthFrameDescription.Height));
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
                    bool s = Bodies.GetType().IsSerializable;
                }
            }
            List<SerializableBody> serializableBodies = new List<SerializableBody>();
            foreach (Body body in Bodies)
            {
                serializableBodies.Add(new SerializableBody(body));
                if (body.IsTracked)
                {
                    if (body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Closed)
                    {
                        calibrationDataMessage = new CalibrationDataMessage(new SerializableBody(body));
                    }
                }
            }
            bodyStreamMessage = new BodyStreamMessage(serializableBodies.ToArray());
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
                DepthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        public NullablePoint3D[] GenerateFullPointCloud()
        {
            List<NullablePoint3D> validPointList = new List<NullablePoint3D>();

            kinectSensor.CoordinateMapper.MapDepthPointsToCameraSpace(allDepthSpacePoints, depthArray, pointCloudCandidates);
            int i = 0;
            foreach (CameraSpacePoint point in pointCloudCandidates)
            {
                if (GeometryHelper.IsValidPoint(point))
                {
                    //validPointList.Add(GeometryHelper.CameraSpacePointToPoint3D(point));
                    validPointList.Add(new NullablePoint3D(point.X, point.Y, point.Z));
                }
                //  Keep invalid points for easier depth space-camera space mapping on client side
                else
                {
                    validPointList.Add(null);
                }
                i++;
            }

            FullPointCloud = validPointList.ToArray();

            pointCloudStreamMessage = new PointCloudStreamMessage(FullPointCloud);

            return validPointList.ToArray();
        }
    }
}
