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
using GlmNet;
using SharpGL.VertexBuffers;
using SharpGL.Shaders;
using SharpGL.SceneGraph.Assets;

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

        //

        vec3 position = new vec3(0, 0, -1);
        vec3 lookat = new vec3(0, 0, 0);
        vec3 up = new vec3(0, 1, 0);

        //Common
        mat4 projectionMatrix;
        mat4 viewMatrix;
        mat4 modelMatrix;

        //PointCloud
        const uint pointCloudAttributeIndexPosition = 0;
        const uint pointCloudAttributeIndexColor = 1;
        float[] pointCloudVertices;
        VertexBufferArray pointCloudVertexBufferArray;
        private ShaderProgram shaderProgramPointCloud; //irregular name

        //Floor
        const uint attributeIndexPosition = 0;
        const uint attributeIndexVertexUV = 1;
        Texture floorTexture = new Texture();
        VertexBufferArray floorVertexBufferArray;
        private ShaderProgram floorShaderProgram;

        //

        public RoomPointCloudView()
        {
            InitializeComponent();

            kinectServer = KinectServer.Instance;
            messageProcessor = kinectServer.MessageProcessor;
            
            pointCloudDictionary = DataStore.Instance.clientPointClouds;
        }

        private void PointCloudDataArrived(KinectDemoMessage message, KinectClient kinectClient)
        {
            if (default(mat4).Equals(projectionMatrix))
            {
                createVerticesForPointCloud(OpenGlControl.OpenGL);
            }
            LoadData();
        }

        private void LoadData()
        {
            NullablePoint3D[] pointCloud = DataStore.Instance.clientPointClouds[activeClient];
            List<float> cloudVerticesList = new List<float>();
            foreach (NullablePoint3D point in pointCloud)
            {
                cloudVerticesList.Add((float)point.X);
                cloudVerticesList.Add((float)point.Y);
                cloudVerticesList.Add((float)point.Z);
            }
            pointCloudVertices = cloudVerticesList.ToArray();
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
            OpenGL gl = OpenGlControl.OpenGL;

            gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.Enable(OpenGL.GL_BLEND);
            gl.Enable(OpenGL.GL_TEXTURE_2D);
            gl.Enable(OpenGL.GL_PROGRAM_POINT_SIZE);
            gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);

            //  Create the shader program for point cloud
            var vertexShaderSource = System.IO.File.ReadAllText("pointcloud.vert");
            var fragmentShaderSource = System.IO.File.ReadAllText("pointcloud.frag");
            shaderProgramPointCloud = new ShaderProgram();
            shaderProgramPointCloud.Create(gl, vertexShaderSource, fragmentShaderSource, null);
            shaderProgramPointCloud.BindAttributeLocation(gl, pointCloudAttributeIndexPosition, "in_Position");
            shaderProgramPointCloud.BindAttributeLocation(gl, pointCloudAttributeIndexColor, "in_Color");
            shaderProgramPointCloud.AssertValid(gl);
            //createVerticesForPointCloud(gl);
            //createVerticesForFloor(gl);


            //var floorVertSource = System.IO.File.ReadAllText("floor.vert");
            //var floorFragSource = System.IO.File.ReadAllText("floor.frag");
            //floorShaderProgram = new ShaderProgram();
            //floorShaderProgram.Create(gl, floorVertSource, floorFragSource, null);
            //floorShaderProgram.BindAttributeLocation(gl, pointCloudAttributeIndexPosition, "in_Position");
            //floorShaderProgram.BindAttributeLocation(gl, pointCloudAttributeIndexColor, "vertexUV"); //change
            //floorShaderProgram.AssertValid(gl);
            //floorTexture.Create(gl, "cat1.jpg");

            //----------------

            //double radius = -4;
            //double theta = 0;
            //double phi = 0;

            //cameraPosSphere = new Point3D(
            //    radius,
            //    theta,
            //    phi
            //    );

            //cameraPos = GeometryHelper.SphericalToCartesian(cameraPosSphere);
            ////  Enable the OpenGL depth testing functionality.
            ////args.OpenGL.Enable(OpenGL.GL_DEPTH_TEST);
        }

        private void createVerticesForPointCloud(OpenGL gl)
        {
            pointCloudVertexBufferArray = new VertexBufferArray();
            pointCloudVertexBufferArray.Create(gl);
            pointCloudVertexBufferArray.Bind(gl);

            var vertexDataBuffer = new VertexBuffer();
            vertexDataBuffer.Create(gl);
            vertexDataBuffer.Bind(gl);
            vertexDataBuffer.SetData(gl, 0, pointCloudVertices, false, 3);

            /* var colorDataBuffer = new VertexBuffer();
             colorDataBuffer.Create(gl);
             colorDataBuffer.Bind(gl);
             colorDataBuffer.SetData(gl, 1, colors, false, 3);*/

            pointCloudVertexBufferArray.Unbind(gl);
        }

        private void createVerticesForFloor(OpenGL gl)
        {

            float[] vertices = {
                                    -0.34172125205764f, 0.53836830689914f, 2.85399643991607f,
                                    -0.416902514984282f, 0.376399677859286f, 2.64154877524325f,
                                    -0.114916737823797f, 0.353512639083294f, 2.62640796532687f,
                                    -0.0765370615054481f, 0.482218856037199f, 2.79420990925737f
                               };
            float[] texUV = {
                              0.0f, 0.0f,
                              1.0f, 0.0f,
                              1.0f, 1.0f,
                              0.0f, 1.0f
                            };



            floorVertexBufferArray = new VertexBufferArray();
            floorVertexBufferArray.Create(gl);
            floorVertexBufferArray.Bind(gl);

            var vertexDataBuffer = new VertexBuffer();
            vertexDataBuffer.Create(gl);
            vertexDataBuffer.Bind(gl);
            vertexDataBuffer.SetData(gl, 0, vertices, false, 3);

            var colorDataBuffer = new VertexBuffer();
            colorDataBuffer.Create(gl);
            colorDataBuffer.Bind(gl);
            colorDataBuffer.SetData(gl, 1, texUV, false, 2);

            pointCloudVertexBufferArray.Unbind(gl);
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
                if (default(mat4).Equals(projectionMatrix ))
                {
                    return;
                }

                OpenGL gl = OpenGlControl.OpenGL;
                //  Clear the scene.
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);

                //  Bind the shader, set the matrices.
                shaderProgramPointCloud.Bind(gl);
                shaderProgramPointCloud.SetUniformMatrix4(gl, "projectionMatrix", projectionMatrix.to_array());
                shaderProgramPointCloud.SetUniformMatrix4(gl, "viewMatrix", viewMatrix.to_array());
                shaderProgramPointCloud.SetUniform3(gl, "uColor", 0.0f, 1.0f, 0.0f);
                shaderProgramPointCloud.SetUniform1(gl, "uSize", 1.0f);


                pointCloudVertexBufferArray.Bind(gl);
                gl.DrawArrays(OpenGL.GL_POINTS, 0, pointCloudVertices.Length / 3);
                pointCloudVertexBufferArray.Unbind(gl);


                shaderProgramPointCloud.Unbind(gl);


                floorTexture.Bind(gl);
                floorShaderProgram.Bind(gl);
                floorShaderProgram.SetUniformMatrix4(gl, "projectionMatrix", projectionMatrix.to_array());
                floorShaderProgram.SetUniformMatrix4(gl, "viewMatrix", viewMatrix.to_array());
                floorVertexBufferArray.Bind(gl);
                gl.DrawArrays(OpenGL.GL_QUADS, 0, 4);
                floorVertexBufferArray.Unbind(gl);
                floorShaderProgram.Unbind(gl);




                //if (pointCloudDictionary.Count == 0)
                //{
                //    return;
                //}
                //if (pointCloudDictionary[activeClient] == null)
                //{
                //    return;
                //}
                ////  Get the OpenGL instance that's been passed to us.
                //OpenGL gl = args.OpenGL;

                //gl.PointSize(1.0f);

                ////  Clear the color and depth buffers.
                //gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

                ////  Reset the modelview matrix.
                //gl.LoadIdentity();

                //gl.Begin(OpenGL.GL_POINTS);
                //gl.Color(1.0f, 0.0f, 0.0f);
                ////  Move the geometry into a fairly central position.
                //foreach (NullablePoint3D point in pointCloudDictionary[activeClient])
                //{
                //    if (point != null)
                //    {
                //        gl.Vertex(point.X, point.Y, point.Z);
                //    }
                //}

                //gl.End();

                //if (showMerged)
                //{
                //    gl.Begin(OpenGL.GL_POINTS);
                //    gl.Color(1.0f, 0.0f, 1.0f);
                //    //  Move the geometry into a fairly central position.
                //    foreach (NullablePoint3D point in transformedPointCloud)
                //    {
                //        if (point != null)
                //        {
                //            gl.Vertex(point.X, point.Y, point.Z);
                //        }
                //    }

                //    gl.End();
                //}

                //gl.Begin(OpenGL.GL_TRIANGLES);
                //foreach (Workspace workspace in DataStore.Instance.WorkspaceDictionary.Values)
                //{
                //    if (workspace.Active)
                //    {
                //        gl.Color(0.0f, 0.0f, 1.0f);
                //    }
                //    else
                //    {
                //        gl.Color(0.0f, 1.0f, 1.0f);
                //    }
                //    Point3D[] vertices = workspace.Vertices3D;
                //    Point3D v0 = vertices[0];
                //    Point3D v1 = vertices[1];
                //    Point3D v2 = vertices[2];
                //    Point3D v3 = vertices[3];
                //    gl.Vertex(v0.X, v0.Y, v0.Z);
                //    gl.Vertex(v1.X, v1.Y, v1.Z);
                //    gl.Vertex(v2.X, v2.Y, v2.Z);

                //    gl.Vertex(v2.X, v2.Y, v2.Z);
                //    gl.Vertex(v3.X, v3.Y, v3.Z);
                //    gl.Vertex(v0.X, v0.Y, v0.Z);

                //}
                //gl.End();

                //if (handPositions.Count > 0)
                //{
                //    gl.Color(0.0f, 1.0f, 0.0f);
                //    gl.PointSize(5.0f);
                //    gl.Begin(OpenGL.GL_POINTS);
                //    foreach (CameraSpacePoint hand in handPositions)
                //    {
                //        gl.Vertex(hand.X, hand.Y, hand.Z);
                //    }
                //    gl.End();
                //}
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
                messageProcessor.BodyDataArrived -= BodyDataArrived;
                messageProcessor.PointCloudDataArrived -= PointCloudDataArrived;
            }
            else if ((bool)e.NewValue)
            {
                try
                {
                    activeClient = DataStore.Instance.KinectClients[0];
                    messageProcessor.BodyDataArrived += BodyDataArrived;
                    messageProcessor.PointCloudDataArrived += PointCloudDataArrived;
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

        private List<NullablePoint3D[]> GetPointClouds()
        {
            SerializableBody[] bodies1;
            SerializableBody[] bodies2;
            KinectClient client1 = DataStore.Instance.KinectClients[0];
            KinectClient client2 = DataStore.Instance.KinectClients[1];
            bodies1 = DataStore.Instance.clientCalibrationBodies[client1].ToArray();
            bodies2 = DataStore.Instance.clientCalibrationBodies[client2].ToArray();

            List<NullablePoint3D> pointCloud1 = new List<NullablePoint3D>();
            List<NullablePoint3D> pointCloud2 = new List<NullablePoint3D>();

            for (int i = 0; i < bodies1.Length; i++)
            {
                Dictionary<JointType, Joint> joints1 = new Dictionary<JointType, Joint>();
                Dictionary<JointType, Joint> joints2 = new Dictionary<JointType, Joint>();
                bodies1[i].Joints.CopyToDictionary(joints1);
                bodies2[i].Joints.CopyToDictionary(joints2);

                foreach (JointType type in joints1.Keys)
                {
                    if (joints1[type].TrackingState == TrackingState.Tracked && joints2[type].TrackingState == TrackingState.Tracked)
                    {
                        pointCloud1.Add(
                            new NullablePoint3D(
                                joints1[type].Position.X,
                                joints1[type].Position.Y,
                                joints1[type].Position.Z
                                ));
                        pointCloud2.Add(
                            new NullablePoint3D(
                                joints2[type].Position.X,
                                joints2[type].Position.Y,
                                joints2[type].Position.Z
                                ));
                    }
                }
            }

            return new List<NullablePoint3D[]>() { 
                pointCloud1.ToArray(),
                pointCloud2.ToArray(),
            };

        }

        private void MergeButton_Click(object sender, RoutedEventArgs e)
        {
            List<NullablePoint3D[]> pointClouds = GetPointClouds();
            showMerged = true;
            //fal
            var kinect1CalPoints = pointClouds[0];
            //var kinect1CalPoints = new NullablePoint3D[]{

            //    new NullablePoint3D(-0.2141886, -0.3827868,  2.077 ),
            //    new NullablePoint3D(-0.5510268, -0.3471858,  2.119 ),
            //    new NullablePoint3D(-0.4770563, -0.09818071, 2.456), 
            //    new NullablePoint3D(-0.5368629, -0.3702611,  2.065 ),
            //    new NullablePoint3D(-0.08871523, -0.3175019, 2.163), 
            //    new NullablePoint3D(-0.3015684, -0.3948682,  2.046 )

            //    //new NullablePoint3D(0, 0, 0),
            //    //new NullablePoint3D(1, 0, 0),
            //    //new NullablePoint3D(1, 0, 1), 
            //    //new NullablePoint3D(0, 0,  1),
            //    //new NullablePoint3D(0, 1, 0), 
            //    //new NullablePoint3D(1, 1,  0)
            //};
            //ajtó
            var kinect2CalPoints = pointClouds[1];
            //var kinect2CalPoints = new NullablePoint3D[]{

            //    new NullablePoint3D(-0.3635642, -0.4667397, 1.891),
            //    new NullablePoint3D(-0.2965499, -0.2976018, 2.139),
            //    new NullablePoint3D(0.1402809,  -0.3342402,  2.077),
            //    new NullablePoint3D(-0.3312522, -0.2975046, 2.139),
            //    new NullablePoint3D(-0.2726442, -0.5310401, 1.798),
            //    new NullablePoint3D(-0.3915003, -0.4241526, 1.953)

            //    //new NullablePoint3D(1.05, -0.05, -0.05),
            //    //new NullablePoint3D(2.05, 0.05, -0.05),
            //    //new NullablePoint3D(2.05, -1.05, -0.05),
            //    //new NullablePoint3D(1.05, -1.05, -0.05),
            //    //new NullablePoint3D(1.05, 0.05, -0.95),
            //    //new NullablePoint3D(1.95, -0.05, -1.05)
            //};

            var A = GeometryHelper.GetTransformationAndRotation(kinect1CalPoints, kinect2CalPoints);

            Matrix<double> rot = A.R;
            Vector<double> translate = A.T;


            var a = rot * DenseVector.OfArray(new[] { kinect1CalPoints[0].X, kinect1CalPoints[0].Y, kinect1CalPoints[0].Z })  + translate;

            List<NullablePoint3D> transformedPointCloudList = new List<NullablePoint3D>();

            foreach (NullablePoint3D point in pointCloudDictionary[DataStore.Instance.KinectClients[0]])
            //foreach (NullablePoint3D point in kinect1CalPoints)
            {
                if (point != null)
                {
                    var pointVector = DenseVector.OfArray(new[] { point.X, point.Y, point.Z });
                    var rottranv = (rot * pointVector) + translate;
                    transformedPointCloudList.Add(new NullablePoint3D(rottranv[0], rottranv[1], rottranv[2]));
                }
            }

            transformedPointCloud = transformedPointCloudList.ToArray();

        }

        private void OpenGlControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenGlControl.Focus();

        }
    }
}