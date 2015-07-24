using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using KinectDemoCommon.Annotations;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;

namespace KinectDemoCommon.UIElement
{
    /// <summary>
    /// Interaction logic for BodyView.xaml
    /// </summary>
    public partial class BodyView : INotifyPropertyChanged
    {

        //      TODO: bind workspacelist

        public ObservableCollection<Workspace> WorkspaceList = new ObservableCollection<Workspace>();

        public Dictionary<Workspace, bool> WorkspaceActiveMap = new Dictionary<Workspace, bool>();

        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

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

        private Pen activeWorkspacePen = new Pen(Brushes.Red, 3);

        private Pen passiveWorkspacePen = new Pen(Brushes.Aqua, 3);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

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

        //private readonly KinectStreamer kinectStreamer;

        private DrawingImage imageSource;

        // Distance tolerance in meters
        private const double DistanceTolerance = 0.1;

        private MessageProcessor messageProcessor;

        public BodyView()
        {

            KinectServer kinectServer = KinectServer.Instance;

            messageProcessor = kinectServer.MessageProcessor;
            //  TODO: only bind when visible
            messageProcessor.ColorDataArrived += kinectServer_ColorDataArrived;
            //messageProcessor.BodyDataArrived += kinectServer_BodyDataArrived;

            imageSource = new DrawingImage(drawingGroup);

            //// use the window object as the view model in this simple example
            DataContext = this;

            //// initialize the components (controls) of the window
            InitializeComponent();
        }

        private void kinectServer_BodyDataArrived(KinectDemoMessage message, KinectClient kinectClient)
        {
            BodyStreamMessage msg = (BodyStreamMessage) message;

            //bones = kinectStreamer.Bones;

            //// populate body colors, one for each BodyIndex
            //bodyColors = new List<Pen>();

            //bodyColors.Add(new Pen(Brushes.Red, 6));
            //bodyColors.Add(new Pen(Brushes.Orange, 6));
            //bodyColors.Add(new Pen(Brushes.Green, 6));
            //bodyColors.Add(new Pen(Brushes.Blue, 6));
            //bodyColors.Add(new Pen(Brushes.Indigo, 6));
            //bodyColors.Add(new Pen(Brushes.Violet, 6));

        }

        private void kinectServer_ColorDataArrived(KinectDemoMessage message, KinectClient client)
        {
            ColorStreamMessage msg = (ColorStreamMessage)message;
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (colorBitmap == null)
                    {
                        colorBitmap = new WriteableBitmap(msg.ColorFrameSize.Width, msg.ColorFrameSize.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                    }
                    colorBitmap.WritePixels(
                        new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                        msg.ColorPixels,
                        colorBitmap.PixelWidth*4,
                        0);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        public ImageSource ColorImageSource
        {
            get
            {
                return colorBitmap;
            }
        }

        public ImageSource ImageSource
        {
            get
            {
                return colorBitmap;
            }
        }

        private void CheckActiveWorkspace(CameraSpacePoint[] handPositions)
        {
            foreach (Workspace workspace in WorkspaceList)
            {
                Point3D[] vertices = workspace.FittedVertices;

                Polygon poly = new Polygon();
                poly.Points = new PointCollection
                { 
                    new Point(vertices[0].X, vertices[0].Y),
                    new Point(vertices[1].X, vertices[1].Y),
                    new Point(vertices[2].X, vertices[2].Y),
                    new Point(vertices[3].X, vertices[3].Y) };

                bool isActive = false;
                foreach (CameraSpacePoint handPosition in handPositions)
                {
                    Vector<double> handVector = new DenseVector(new double[] {
                        handPosition.X,
                        handPosition.Y,
                        handPosition.Z
                    });

                    if (GeometryHelper.InsidePolygon3D(vertices.ToArray(), GeometryHelper.ProjectPoint3DToPlane(GeometryHelper.CameraSpacePointToPoint3D(handPosition), workspace.PlaneVector)))
                    {
                        double distance = GeometryHelper.CalculatePointPlaneDistance(GeometryHelper.CameraSpacePointToPoint3D(handPosition), workspace.PlaneVector);

                        if (Math.Abs(distance) <= DistanceTolerance)
                        {
                            isActive = true;
                        }
                    }
                }
                WorkspaceActiveMap[workspace] = isActive;
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
            //CoordinateMapper coordinateMapper = kinectStreamer.CoordinateMapper;

            //foreach (Workspace workspace in WorkspaceList)
            //{
            //    Pen pen = WorkspaceActiveMap[workspace] ? activeWorkspacePen : passiveWorkspacePen;

            //    CameraSpacePoint p0 = Point3DtoCameraSpacePoint(workspace.FittedVertices[0]);
            //    CameraSpacePoint p1 = Point3DtoCameraSpacePoint(workspace.FittedVertices[1]);
            //    CameraSpacePoint p2 = Point3DtoCameraSpacePoint(workspace.FittedVertices[2]);
            //    CameraSpacePoint p3 = Point3DtoCameraSpacePoint(workspace.FittedVertices[3]);

            //    ColorSpacePoint[] colorPoints = new ColorSpacePoint[4];
            //    coordinateMapper.MapCameraPointsToColorSpace(new[] { p0, p1, p2, p3 }, colorPoints);

            //    for (int i = 0; i < colorPoints.Length; i++)
            //    {
            //        dc.DrawLine(pen,
            //            new Point(colorPoints[i % colorPoints.Length].X, colorPoints[i % colorPoints.Length].Y),
            //            new Point(colorPoints[(i + 1) % colorPoints.Length].X, colorPoints[(i + 1) % colorPoints.Length].Y));
            //    }
            //}
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void BodyView_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                //kinectStreamer.ColorDataReady += kinectStreamer_ColorDataReady;
                //kinectStreamer.KinectStreamerConfig.ProvideColorData = true;

                //kinectStreamer.BodyDataReady += kinectStreamer_BodyDataReady;
                //kinectStreamer.KinectStreamerConfig.ProvideBodyData = true;

                //WorkspaceActiveMap.Clear();
                //foreach (Workspace workspace in WorkspaceList)
                //{
                //    WorkspaceActiveMap.Add(workspace, false);
                //}
            }
            else
            {
                //kinectStreamer.DepthDataReady -= kinectStreamer_ColorDataReady;
                //kinectStreamer.KinectStreamerConfig.ProvideDepthData = false;

                //kinectStreamer.BodyDataReady -= kinectStreamer_BodyDataReady;
                //kinectStreamer.KinectStreamerConfig.ProvideBodyData = false;
            }
        }

        //void kinectStreamer_BodyDataReady(object sender, KinectStreamerEventArgs e)
        //{
            //Bodies = e.Bodies;
            
            //using (DrawingContext dc = drawingGroup.Open())
            //{
            //    dc.DrawImage(ColorImageSource, new Rect(0.0, 0.0, displayWidth, displayHeight));


            //    int penIndex = 0;
            //    foreach (Body body in Bodies)
            //    {
            //        Pen drawPen = bodyColors[penIndex++];

            //        if (body.IsTracked)
            //        {
            //            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

            //            // convert the joint points to depth (display) space
            //            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

            //            foreach (JointType jointType in joints.Keys)
            //            {
            //                // sometimes the depth(Z) of an inferred joint may show as negative
            //                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
            //                CameraSpacePoint position = joints[jointType].Position;
            //                if (position.Z < 0)
            //                {
            //                    position.Z = InferredZPositionClamp;
            //                }
            //                ColorSpacePoint colorSpacePoint = kinectStreamer.CoordinateMapper.MapCameraPointToColorSpace(position);
            //                jointPoints[jointType] = new Point(colorSpacePoint.X, colorSpacePoint.Y);
            //            }

            //            DrawBody(joints, jointPoints, dc, drawPen);

            //            DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
            //            DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);

            //            CheckActiveWorkspace(new CameraSpacePoint[]{
            //                body.Joints[JointType.HandRight].Position,
            //                body.Joints[JointType.HandLeft].Position});
            //        }
            //    }
            //    // prevent drawing outside of our render area
            //    drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, displayWidth, displayHeight));
            //    DrawWorksapces(dc);
            //    OnPropertyChanged("ColorImageSource");
            //    OnPropertyChanged("ImageSource");
            //}
        //}

        //void kinectStreamer_ColorDataReady(object sender, KinectStreamerEventArgs e)
        //{
        //    colorBitmap = e.ColorBitmap;
        //    OnPropertyChanged("ColorImageSource");
        //    OnPropertyChanged("ImageSource");
        //}
    }
}
