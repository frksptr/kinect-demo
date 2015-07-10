using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using KinectDemoSGL.UIElement.Model;
using KinectDemoSGL.Util;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;

namespace KinectDemoSGL.UIElement
{
    /// <summary>
    /// Interaction logic for BodyView.xaml
    /// </summary>
    public partial class BodyView : UserControl
    {

        //      TODO: bind workspacelist

        public ObservableCollection<Workspace> WorkspaceList = new ObservableCollection<Workspace>();

        private WriteableBitmap colorBitmap;

        private FrameDescription colorFrameDescription;

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private MultiSourceFrameReader multiSourceFrameReader;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        public Body[] Bodies;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        private bool workspacesDrawn = false;

        private Pen workspacePen;

        // Distance tolerance in meters
        private const double DistanceTolerance = 0.1;

        public BodyView(KinectSensor kinectSensor)
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // open the reader for the body frames
            multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);

            multiSourceFrameReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // a bone defined as a line between two joints
            bones = new List<Tuple<JointType, JointType>>();

            workspacePen = new Pen();
            workspacePen.Brush = Brushes.LightBlue;
            workspacePen.Thickness = 5;

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

            // populate body colors, one for each BodyIndex
            bodyColors = new List<Pen>();

            bodyColors.Add(new Pen(Brushes.Red, 6));
            bodyColors.Add(new Pen(Brushes.Orange, 6));
            bodyColors.Add(new Pen(Brushes.Green, 6));
            bodyColors.Add(new Pen(Brushes.Blue, 6));
            bodyColors.Add(new Pen(Brushes.Indigo, 6));
            bodyColors.Add(new Pen(Brushes.Violet, 6));

            // open the sensor
            this.kinectSensor.Open();

            // Create the drawing group we'll use for drawing
            drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            imageSource = new DrawingImage(drawingGroup);

            // use the window object as the view model in this simple example
            DataContext = this;

            // initialize the components (controls) of the window
            InitializeComponent();

        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return imageSource;
            }
        }

        public ImageSource ColorImageSource
        {
            get
            {
                return colorBitmap;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (multiSourceFrameReader != null)
            {
                multiSourceFrameReader.Dispose();
                multiSourceFrameReader = null;
            }

            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {

            if (IsVisible)
            {
                BodyFrame bodyFrame = null;
                ColorFrame colorFrame = null;

                MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

                // If the Frame has expired by the time we process this event, return.
                if (multiSourceFrame == null)
                {
                    return;
                }

                try
                {
                    bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();
                    colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();

                    // If any frame has expired by the time we process this event, return.
                    // The "finally" statement will Dispose any that are not null.
                    if ((bodyFrame == null) || (colorFrame == null))
                    {
                        return;
                    }

                    using (colorFrame)
                    {
                        if (colorFrame != null)
                        {
                            FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                            displayHeight = colorFrameDescription.Height;
                            displayWidth = colorFrameDescription.Width;

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



                    bool dataReceived = false;

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
                            dataReceived = true;
                        }
                        if (dataReceived)
                        {
                            using (DrawingContext dc = drawingGroup.Open())
                            {
                                dc.DrawImage(ColorImageSource, new Rect(0.0, 0.0, displayWidth, displayHeight));
                                if (!workspacesDrawn)
                                {
                                    DrawWorksapces(dc);
                                }
                                // Draw a transparent background to set the render size
                                //dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));


                                int penIndex = 0;
                                foreach (Body body in Bodies)
                                {
                                    Pen drawPen = bodyColors[penIndex++];

                                    if (body.IsTracked)
                                    {
                                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                                        // convert the joint points to depth (display) space
                                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                                        foreach (JointType jointType in joints.Keys)
                                        {
                                            // sometimes the depth(Z) of an inferred joint may show as negative
                                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                            CameraSpacePoint position = joints[jointType].Position;
                                            if (position.Z < 0)
                                            {
                                                position.Z = InferredZPositionClamp;
                                            }
                                            ColorSpacePoint colorSpacePoint = coordinateMapper.MapCameraPointToColorSpace(position);
                                            jointPoints[jointType] = new Point(colorSpacePoint.X, colorSpacePoint.Y);
                                        }

                                        CheckActiveWorkspace(body, dc);

                                        DrawBody(joints, jointPoints, dc, drawPen);

                                        DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                                        DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                                    }
                                }
                                // prevent drawing outside of our render area
                                drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, displayWidth, displayHeight));
                            }
                        }
                    }
                }
                finally
                {
                    if (bodyFrame != null)
                    {
                        bodyFrame.Dispose();
                    }
                    if (colorFrame != null)
                    {
                        colorFrame.Dispose();
                    }
                }
            }
        }


        private void CheckActiveWorkspace(Body body, DrawingContext dc)
        {

            foreach (Workspace workspace in WorkspaceList)
            {
                ObservableCollection<Point3D> vertices = workspace.FittedVertices;

                Point3D nearLeft = vertices.Aggregate((a, b) => a.X < b.X && a.Z < b.Z ? a : b);// new Point(vertices.Min(p => p.X), vertices.Min(p => p.Z));

                Point3D nearRight = vertices.Aggregate((a, b) => a.X > b.X && a.Z < b.Z ? a : b);// new Point(vertices.Max(p => p.X), vertices.Min(p => p.Z));

                Point3D farLeft = vertices.Aggregate((a, b) => a.X < b.X && a.Z > b.Z ? a : b); //new Point(vertices.Min(p => p.X), vertices.Max(p => p.Z));

                Point3D farRight = vertices.Aggregate((a, b) => a.X > b.X && a.Z > b.Z ? a : b); //new Point(vertices.Max(p => p.X), vertices.Max(p => p.Z));

                Polygon poly = new Polygon();
                poly.Points = new PointCollection
                { 
                    new Point(nearLeft.X, nearLeft.Y),
                    new Point(nearRight.X, nearRight.Y),
                    new Point(farRight.X, farRight.Y),
                    new Point(farLeft.X, farLeft.Y) };

                CameraSpacePoint handPos = body.Joints[JointType.HandRight].Position;

                Vector<double> handVector = new DenseVector( new double[] {
                    handPos.X,
                    handPos.Y,
                    handPos.Z
                });

                if (GeometryHelper.InsidePolygon3D(vertices.ToArray(), GeometryHelper.ProjectPoint3DToPlane(GeometryHelper.CameraSpacePointToPoint3D(handPos),workspace.PlaneVector) ))
                {
                    double distance = GeometryHelper.CalculatePointPlaneDistance(GeometryHelper.CameraSpacePointToPoint3D(handPos), workspace.PlaneVector);

                    if (Math.Abs(distance) <= DistanceTolerance)
                    {
                        workspacePen.Brush = Brushes.Red;
                    }
                    else
                    {
                        workspacePen.Brush = Brushes.Blue;
                    }
                }
                else
                {
                    workspacePen.Brush = Brushes.Blue;
                }
            }
        }


        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in bones)
            {
                DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        private void DrawWorksapces(DrawingContext dc)
        {
            Pen pen = workspacePen;
            CoordinateMapper coordinateMapper = kinectSensor.CoordinateMapper;

            foreach (Workspace workspace in WorkspaceList)
            {
                CameraSpacePoint p0 = Point3DtoCameraSpacePoint(workspace.FittedVertices[0]);
                CameraSpacePoint p1 = Point3DtoCameraSpacePoint(workspace.FittedVertices[1]);
                CameraSpacePoint p2 = Point3DtoCameraSpacePoint(workspace.FittedVertices[2]);
                CameraSpacePoint p3 = Point3DtoCameraSpacePoint(workspace.FittedVertices[3]);

                ColorSpacePoint[] colorPoints = new ColorSpacePoint[4];
                coordinateMapper.MapCameraPointsToColorSpace(new[] { p0, p1, p2, p3 }, colorPoints);

                for (int i = 0; i < colorPoints.Length; i++)
                {
                    dc.DrawLine(pen,
                        new Point(colorPoints[i % colorPoints.Length].X, colorPoints[i % colorPoints.Length].Y),
                        new Point(colorPoints[(i + 1) % colorPoints.Length].X, colorPoints[(i + 1) % colorPoints.Length].Y));
                }
            }
            //this.workspacesDrawn = true;
        }

        private CameraSpacePoint Point3DtoCameraSpacePoint(Point3D point3D)
        {
            return new CameraSpacePoint
            {
                X = (float)point3D.X,
                Y = (float)point3D.Y,
                Z = (float)point3D.Z
            };
        }
    }
}
