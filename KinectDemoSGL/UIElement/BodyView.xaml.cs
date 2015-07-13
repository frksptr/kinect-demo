using KinectDemo.Util;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectDemo.UIElements
{
    /// <summary>
    /// Interaction logic for BodyView.xaml
    /// </summary>
    public partial class BodyView : UserControl
    {

        //      TODO: bind workspacelist

        public ObservableCollection<Workspace> workspaceList = new ObservableCollection<Workspace>();

        private WriteableBitmap colorBitmap = null;

        private FrameDescription colorFrameDescription = null;

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
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private MultiSourceFrameReader multiSourceFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        public Body[] bodies = null;

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
        private const double DISTANCE_TOLERANCE = 0.1;

        public BodyView(KinectSensor kinectSensor)
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // open the reader for the body frames
            this.multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);

            this.multiSourceFrameReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            this.colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            this.colorBitmap = new WriteableBitmap(this.colorFrameDescription.Width, this.colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            this.workspacePen = new Pen();
            this.workspacePen.Brush = Brushes.LightBlue;
            this.workspacePen.Thickness = 5;

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

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // open the sensor
            this.kinectSensor.Open();

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        public ImageSource ColorImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
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

            if (this.IsVisible)
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
                            this.displayHeight = colorFrameDescription.Height;
                            this.displayWidth = colorFrameDescription.Width;

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


                    bool dataReceived = false;

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
                            dataReceived = true;
                        }
                        if (dataReceived)
                        {
                            using (DrawingContext dc = this.drawingGroup.Open())
                            {
                                dc.DrawImage(this.ColorImageSource, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                                if (!workspacesDrawn)
                                {
                                    drawWorksapces(dc);
                                }
                                // Draw a transparent background to set the render size
                                //dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));


                                int penIndex = 0;
                                foreach (Body body in this.bodies)
                                {
                                    Pen drawPen = this.bodyColors[penIndex++];

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
                                            ColorSpacePoint colorSpacePoint = this.coordinateMapper.MapCameraPointToColorSpace(position);
                                            jointPoints[jointType] = new Point(colorSpacePoint.X, colorSpacePoint.Y);
                                        }

                                        checkActiveWorkspace(body, dc);

                                        this.DrawBody(joints, jointPoints, dc, drawPen);

                                        this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                                        this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                                    }
                                }
                                // prevent drawing outside of our render area
                                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
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


        private void checkActiveWorkspace(Body body, DrawingContext dc)
        {

            foreach (Workspace workspace in workspaceList)
            {
                ObservableCollection<Point3D> vertices = workspace.FittedVertices;

                Point3D nearLeft = vertices.Aggregate((a, b) => a.X < b.X && a.Z < b.Z ? a : b);

                Point3D nearRight = vertices.Aggregate((a, b) => a.X > b.X && a.Z < b.Z ? a : b);

                Point3D farLeft = vertices.Aggregate((a, b) => a.X < b.X && a.Z > b.Z ? a : b);

                Point3D farRight = vertices.Aggregate((a, b) => a.X > b.X && a.Z > b.Z ? a : b);

                Polygon poly = new Polygon();
                poly.Points = new PointCollection() { 
                    new Point(nearLeft.X, nearLeft.Y),
                    new Point(nearRight.X, nearRight.Y),
                    new Point(farRight.X, farRight.Y),
                    new Point(farLeft.X, farLeft.Y) };

                CameraSpacePoint handPos = body.Joints[JointType.HandRight].Position;

                Vector<double> handVector = new DenseVector(new double[] {
                    (double)handPos.X,
                    (double)handPos.Y,
                    (double)handPos.Z
                });

                if (GeometryHelper.insidePolygon3D(vertices.ToArray(), GeometryHelper.projectPoint3DToPlane(GeometryHelper.cameraSpacePointToPoint3D(handPos),workspace.planeVector) ))
                {
                    double distance = GeometryHelper.calculatePointPlaneDistance(GeometryHelper.cameraSpacePointToPoint3D(handPos), workspace.planeVector);

                    if (Math.Abs(distance) <= DISTANCE_TOLERANCE)
                    {
                        workspace.Active = true;
                    }
                    else
                    {
                        workspace.Active = false;
                    }
                }
                else
                {
                    workspace.Active = false;
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
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
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
            Pen drawPen = this.inferredBonePen;
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
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        private void drawWorksapces(DrawingContext dc)
        {
            
            CoordinateMapper coordinateMapper = this.kinectSensor.CoordinateMapper;

            foreach (Workspace workspace in workspaceList)
            {
                CameraSpacePoint p0 = point3DtoCameraSpacePoint(workspace.FittedVertices[0]);
                CameraSpacePoint p1 = point3DtoCameraSpacePoint(workspace.FittedVertices[1]);
                CameraSpacePoint p2 = point3DtoCameraSpacePoint(workspace.FittedVertices[2]);
                CameraSpacePoint p3 = point3DtoCameraSpacePoint(workspace.FittedVertices[3]);

                ColorSpacePoint[] colorPoints = new ColorSpacePoint[4];
                coordinateMapper.MapCameraPointsToColorSpace(new CameraSpacePoint[] { p0, p1, p2, p3 }, colorPoints);

                for (int i = 0; i < colorPoints.Length; i++)
                {
                    dc.DrawLine(workspace.Active ? new Pen(Brushes.Blue, 5) : new Pen(Brushes.Red, 5),
                        new Point(colorPoints[i % colorPoints.Length].X, colorPoints[i % colorPoints.Length].Y),
                        new Point(colorPoints[(i + 1) % colorPoints.Length].X, colorPoints[(i + 1) % colorPoints.Length].Y));
                }
            }
            //this.workspacesDrawn = true;
        }

        private CameraSpacePoint point3DtoCameraSpacePoint(Point3D point3D)
        {
            return new CameraSpacePoint()
            {
                X = (float)point3D.X,
                Y = (float)point3D.Y,
                Z = (float)point3D.Z
            };
        }
    }
}
