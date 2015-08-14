using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using SharpGL;
using SharpGL.SceneGraph;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;
using Microsoft.Kinect;

namespace KinectDemoSGL.UIElement
{

    /// <summary>
    /// Interaction logic for RoomPointCloudView.xaml
    /// </summary>
    public partial class RoomPointCloudView : UserControl
    {
        public Point3D Center { get; set; }

        double rotationFactor = 0.05;

        double zoomFactor = 0.5;

        private Point3D cameraPosSphere;

        private Point3D cameraPos;

        public KinectClient activeClient { get; set; }

        public double DistanceTolerance = 0.2;

        private KinectServer kinectServer;

        private MessageProcessor messageProcessor;

        private List<CameraSpacePoint> handPositions = new List<CameraSpacePoint>();

        private Point3D projectedHandPoint = new Point3D();

        private DataStore dataStore = DataStore.Instance;

        public RoomPointCloudView()
        {
            InitializeComponent();

            kinectServer = KinectServer.Instance;
            messageProcessor = kinectServer.MessageProcessor;
            messageProcessor.BodyDataArrived += BodyDataArrived;
        }

        private void BodyDataArrived(KinectDemoMessage message, KinectClient kinectClient)
        {
            BodyStreamMessage msg = (BodyStreamMessage)message;
            handPositions = new List<CameraSpacePoint>();
            foreach (SerializableBody body in msg.Bodies)
            {
                if (body != null)
                {
                    if (body.IsTracked)
                    {
                        IDictionary<JointType, Joint> joints = new Dictionary<JointType, Joint>();
                        body.Joints.CopyToDictionary(joints);
                        handPositions.Add(joints[JointType.HandLeft].Position);
                        handPositions.Add(joints[JointType.HandRight].Position);
                    }
                }

            }
            CheckActiveWorkspace(handPositions.ToArray());
        }

        private void OpenGLControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            double radius = -4;
            double theta = 0;
            double phi = 0;

            cameraPosSphere = new Point3D(
                radius,
                theta,
                phi
                );

            cameraPos = GeometryHelper.SphericalToCartesian(cameraPosSphere);
            //  Enable the OpenGL depth testing functionality.
            //args.OpenGL.Enable(OpenGL.GL_DEPTH_TEST);
        }

        private List<Point3D> CenterPointCloud(List<Point3D> points)
        {
            List<Point3D> centeredPoints = new List<Point3D>();
            foreach (Point3D point in points)
            {
                centeredPoints.Add(new Point3D(
                    point.X - Center.X,
                    point.Y - Center.Y,
                    point.Z - Center.Z
                    ));
            }
            return centeredPoints;

        }

        private void OpenGLControl_Resized(object sender, OpenGLEventArgs args)
        {
            Transform();
        }

        private void Transform()
        {
            // Get the OpenGL instance.
            OpenGL gl = OpenGlControl.OpenGL;

            // Load and clear the projection matrix.
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity();

            // Perform a perspective transformation
            gl.Perspective(60.0f, gl.RenderContextProvider.Width /
                (float)gl.RenderContextProvider.Height,
                0.1f, 100.0f);


            gl.LookAt(cameraPos.X, cameraPos.Y, cameraPos.Z, Center.X, Center.Y, Center.Z, 0, 1, 0);

            // Load the modelview.
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
        }

        private void OpenGLControl_OpenGLDraw(object sender, OpenGLEventArgs args)
        {
            if (IsVisible)
            {
                if (dataStore.GetPointCloudForClient(activeClient) == null)
                {
                    return;
                }
                //  Get the OpenGL instance that's been passed to us.
                OpenGL gl = args.OpenGL;

                gl.PointSize(1.0f);

                //  Clear the color and depth buffers.
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

                //  Reset the modelview matrix.
                gl.LoadIdentity();

                PointCloud pointCloud = dataStore.GetColoredPointCloudForClient(activeClient);
                NullablePoint3D[] points = pointCloud.Points;
                byte[] colors = pointCloud.ColorBytes;
                gl.Begin(OpenGL.GL_POINTS);
                gl.Color(1.0f, 0.0f, 0.0f);
                for (int i = 0; i < pointCloud.Points.Length; i++)
                {
                    var point = points[i];
                    if (point != null)
                    {
                        if (colors != null)
                        {
                            var b = colors[i*4];
                            var g = colors[i*4 + 1];
                            var r = colors[i*4 + 2];
                            var a = colors[i*4 + 3];

                            gl.Color(r, g, b, a);
                        }
                        gl.Vertex(point.X, point.Y, point.Z);
                    }
                }

                gl.End();

                //gl.Begin(OpenGL.GL_POINTS);
                //gl.Color(1.0f, 1.0f, 0.0f);
                //gl.PointSize(5.0f); 
                //gl.Vertex(projectedHandPoint.X, projectedHandPoint.Y, projectedHandPoint.Z);

                //gl.End();

                gl.Begin(OpenGL.GL_TRIANGLES);
                foreach (Workspace workspace in dataStore.GetAllWorkspaces())
                {
                    if (workspace.Active)
                    {
                        gl.Color(0.0f, 0.0f, 1.0f);
                    }
                    else
                    {
                        gl.Color(0.0f, 1.0f, 1.0f);
                    }
                    Point3D[] vertices = workspace.Vertices3D;
                    Point3D v0 = vertices[0];
                    Point3D v1 = vertices[1];
                    Point3D v2 = vertices[2];
                    Point3D v3 = vertices[3];
                    gl.Vertex(v0.X, v0.Y, v0.Z);
                    gl.Vertex(v1.X, v1.Y, v1.Z);
                    gl.Vertex(v2.X, v2.Y, v2.Z);

                    gl.Vertex(v2.X, v2.Y, v2.Z);
                    gl.Vertex(v3.X, v3.Y, v3.Z);
                    gl.Vertex(v0.X, v0.Y, v0.Z);

                }
                gl.End();

                if (handPositions.Count > 0)
                {
                    gl.Color(0.0f, 1.0f, 0.0f);
                    gl.PointSize(5.0f);
                    gl.Begin(OpenGL.GL_POINTS);
                    foreach (CameraSpacePoint hand in handPositions)
                    {
                        gl.Vertex(hand.X, hand.Y, hand.Z);
                    }
                    gl.End();
                }
            }
        }

        private void openGLControl_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key.Equals(Key.S))
            {
                cameraPosSphere.Y -= rotationFactor;
            }
            else if (e.Key.Equals(Key.W))
            {
                cameraPosSphere.Y += rotationFactor;
            }
            else if (e.Key.Equals(Key.A))
            {
                cameraPosSphere.Z -= rotationFactor;
            }
            else if (e.Key.Equals(Key.D))
            {
                cameraPosSphere.Z += rotationFactor;
            }
            else  if (e.Key.Equals(Key.Q))
            {
                cameraPosSphere.X += zoomFactor;
            }
            else if (e.Key.Equals(Key.E))
            {
                cameraPosSphere.X -= zoomFactor;
            }
            else
            {
                return;
            }



            cameraPos = GeometryHelper.SphericalToCartesian(cameraPosSphere);

            //cameraPos.X += Center.X;
            //cameraPos.Y += Center.Y;
            //cameraPos.Z += Center.Z;
            Transform();
        }

        private void openGLControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                OpenGlControl.OpenGL.Rotate(10.0f, 0.0f, 0.0f);
                cameraPosSphere.X += zoomFactor;
            }
            else
            {
                cameraPosSphere.X -= zoomFactor;
            }

            cameraPos = GeometryHelper.SphericalToCartesian(cameraPosSphere);
            //cameraPos.X += Center.X;
            //cameraPos.Y += Center.Y;
            //cameraPos.Z += Center.Z;
            Transform();
        }

        public void CheckActiveWorkspace(CameraSpacePoint[] handPositions)
        {
            if (handPositions.Length > 0)
            {
                CheckActiveWorkspace(Converter.CameraSpacePointsToPoint3Ds(handPositions).ToArray());
            }
        }

        public void CheckActiveWorkspace(Point3D[] handPositions)
        {
            if (handPositions.Length == 0)
            {
                return;
            }
            foreach (Workspace workspace in dataStore.GetAllWorkspaces())
            {
                Point3D[] vertices = workspace.FittedVertices;

                Point[] vertices2d = new[]
                { 
                    new Point(vertices[0].X, vertices[0].Y),
                    new Point(vertices[1].X, vertices[1].Y),
                    new Point(vertices[2].X, vertices[2].Y),
                    new Point(vertices[3].X, vertices[3].Y) };

                bool isActive = false;
                foreach (Point3D handPosition in handPositions)
                {
                    Vector<double> handVector = new DenseVector(new double[] {
                        handPosition.X,
                        handPosition.Y,
                        handPosition.Z
                    });

                    projectedHandPoint = GeometryHelper.ProjectPoint3DToPlane(handPosition, workspace.PlaneVector);

                    if (GeometryHelper.InsidePolygon3D(vertices, projectedHandPoint))
                    {
                        double distance = GeometryHelper.CalculatePointPlaneDistance(handPosition, workspace.PlaneVector);

                        if (Math.Abs(distance) <= DistanceTolerance)
                        {
                            isActive = true;
                        }
                    }
                }
                workspace.Active = isActive;
            }
        }

        private void openGLControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == false)
            {
            }
            else if ((bool)e.NewValue)
            {
                activeClient = dataStore.GetClients()[0];
                OpenGlControl.Focus();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int activeIndex = dataStore.GetClients().IndexOf(activeClient);
            try
            {
                activeClient = dataStore.GetClients()[activeIndex + 1];
            }
            catch
            {
                activeClient = dataStore.GetClients()[0];
            }
        }

        private void SwitchButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MergeButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void OpenGlControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenGlControl.Focus();

        }

    }
}