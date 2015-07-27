﻿using System.Collections.Generic;
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media.Animation;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;
using Microsoft.Kinect;

namespace KinectDemoCommon.UIElement
{

    /// <summary>
    /// Interaction logic for RoomPointCloudView.xaml
    /// </summary>
    public partial class RoomPointCloudView : UserControl
    {
        public Point3D Center { get; set; }

        double rotationFactor = 0.1;

        double zoomFactor = 0.5;

        private Point3D cameraPosSphere;

        private Point3D cameraPos;

        public KinectClient activeClient { get; set; }

        public Dictionary<KinectClient, NullablePoint3D[]> pointCloudDictionary = new Dictionary<KinectClient, NullablePoint3D[]>();

        public double DistanceTolerance = 0.2;

        private KinectServer kinectServer;

        private MessageProcessor messageProcessor;

        private List<CameraSpacePoint> handPositions = new List<CameraSpacePoint>();

        private bool showMerged = false;

        NullablePoint3D[] transformedPointCloud;

        public RoomPointCloudView()
        {
            InitializeComponent();

            kinectServer = KinectServer.Instance;
            messageProcessor = kinectServer.MessageProcessor;
            messageProcessor.BodyDataArrived += BodyDataArrived;
            pointCloudDictionary = DataStore.Instance.clientPointClouds;
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
                if (pointCloudDictionary.Count == 0)
                {
                    return;
                }
                if (pointCloudDictionary[activeClient] == null)
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

                gl.Begin(OpenGL.GL_POINTS);
                gl.Color(1.0f, 0.0f, 0.0f);
                //  Move the geometry into a fairly central position.
                foreach (NullablePoint3D point in pointCloudDictionary[activeClient])
                {
                    if (point != null)
                    {
                        gl.Vertex(point.X, point.Y, point.Z);
                    }
                }

                gl.End();

                if (showMerged)
                {
                    gl.Begin(OpenGL.GL_POINTS);
                    gl.Color(1.0f, 0.0f, 1.0f);
                    //  Move the geometry into a fairly central position.
                    foreach (NullablePoint3D point in transformedPointCloud)
                    {
                        if (point != null)
                        {
                            gl.Vertex(point.X, point.Y, point.Z);
                        }
                    }

                    gl.End();
                }

                gl.Begin(OpenGL.GL_TRIANGLES);
                foreach (Workspace workspace in DataStore.Instance.WorkspaceDictionary.Values)
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
                CheckActiveWorkspace(GeometryHelper.CameraSpacePointsToPoint3Ds(handPositions).ToArray());
            }
        }

        public void CheckActiveWorkspace(Point3D[] handPositions)
        {
            if (handPositions.Length == 0)
            {
                return;
            }
            foreach (Workspace workspace in DataStore.Instance.WorkspaceDictionary.Values)
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

                    if (GeometryHelper.InsidePolygon3D(vertices, GeometryHelper.ProjectPoint3DToPlane(handPosition, workspace.PlaneVector)))
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
                try
                {
                    activeClient = DataStore.Instance.KinectClients[0];
                    OpenGlControl.Focus();
                }
                catch (Exception)
                {
                    
//                    throw;
                }
                
            }
        }

        private void SwitchButton_Click(object sender, RoutedEventArgs e)
        {
            int activeIndex = DataStore.Instance.KinectClients.IndexOf(activeClient);
            try
            {
                activeClient = DataStore.Instance.KinectClients[activeIndex + 1];
            }
            catch
            {
                activeClient = DataStore.Instance.KinectClients[0];
            }
        }

        private void MergeButton_Click(object sender, RoutedEventArgs e)
        {
            showMerged = true;
            //fal
            var kinect1CalPoints = new NullablePoint3D[]{

                //new NullablePoint3D(-0.2141886, -0.3827868,  2.077 ),
                //new NullablePoint3D(-0.5510268, -0.3471858,  2.119 ),

                //new NullablePoint3D(0, 0, 0),
                //new NullablePoint3D(1, 1, 1),

                //new NullablePoint3D(-0.4770563, -0.09818071, 2.456), 
                //new NullablePoint3D(-0.5368629, -0.3702611,  2.065 ),
                //new NullablePoint3D(-0.08871523, -0.3175019, 2.163), 
                //new NullablePoint3D(-0.3015684, -0.3948682,  2.046 )
            };
            //ajtó
            var kinect2CalPoints = new NullablePoint3D[]{

                //new NullablePoint3D(-0.3635642, -0.4667397, 1.891),
                //new NullablePoint3D(-0.2965499, -0.2976018, 2.139),

                //new NullablePoint3D(1.02, 0.99,  1.01),
                //new NullablePoint3D(0.03,-0.01,  0),

                //new NullablePoint3D(0.1402809, -0.3342402,  2.077),
                //new NullablePoint3D(-0.3312522, -0.2975046, 2.139),
                //new NullablePoint3D(-0.2726442, -0.5310401, 1.798),
                //new NullablePoint3D(-0.3915003, -0.4241526, 1.953)
            };

            var A = GeometryHelper.GetTransformationAndRotation(kinect1CalPoints, kinect2CalPoints);

            Matrix<double> rot = A.R;
            Vector<double> translate = A.T;


            var a = DenseVector.OfArray(new[] { kinect1CalPoints[0].X, kinect1CalPoints[0].Y, kinect1CalPoints[0].Z }) * rot + translate;



            List<NullablePoint3D> transformedPointCloudList = new List<NullablePoint3D>();

            foreach (NullablePoint3D point in pointCloudDictionary[DataStore.Instance.KinectClients[1]])
            {
                if (point != null)
                {
                    var pointVector = DenseVector.OfArray(new[] { point.X, point.Y, point.Z });
                    var rottranv = (pointVector * rot) + translate;
                    transformedPointCloudList.Add(new NullablePoint3D(rottranv[0], rottranv[1], rottranv[2]));
                }
            }

            transformedPointCloud = transformedPointCloudList.ToArray();

        }


    }
}