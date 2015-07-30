using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using GlmNet;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;
using SharpGL;
using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Assets;
using SharpGL.Shaders;
using SharpGL.VertexBuffers;
using System.Diagnostics;

namespace KinectDemoSGL.UIElement
{

    //  TODO: bind vertex buffer to point cloud changes

    /// <summary>
    /// Interaction logic for RoomPointCloudView.xaml
    /// </summary>
    /// 
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
        float[] pointCloudVertices = { };
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
            Dispatcher.Invoke(() =>
            {
                LoadPointCloud(DataStore.Instance.clientPointClouds[activeClient]);
                //
                createVerticesForPointCloud(OpenGlControl.OpenGL);
            });
        }

        private void LoadPointCloud(NullablePoint3D[] pointCloud)
        {
            //string[] cloudLines = System.IO.File.ReadAllLines("cloud.txt");
            ////string[] wsLines = System.IO.File.ReadAllLines("ws.txt");

            //List<float> cloudVerticiesList = new List<float>();
            //List<float> wsVerticiesList = new List<float>();

            //foreach (string l in cloudLines)
            //{
            //    string lc = l.Replace('.', ',');
            //    string[] coords = lc.Split(' ');
            //    cloudVerticiesList.Add(Single.Parse(coords[0]));
            //    cloudVerticiesList.Add(Single.Parse(coords[1]));
            //    cloudVerticiesList.Add(Single.Parse(coords[2]));
            //}
            //pointCloudVertices = cloudVerticiesList.ToArray();

            
            List<float> cloudVerticesList = new List<float>();
            foreach (NullablePoint3D point in pointCloud)
            {
                if (point != null)
                {
                    cloudVerticesList.Add((float)point.X);
                    cloudVerticesList.Add((float)point.Y);
                    cloudVerticesList.Add((float)point.Z);
                }
            }
            pointCloudVertices = cloudVerticesList.ToArray();
            createVerticesForPointCloud(OpenGlControl.OpenGL);
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
            var vertexShaderSource = File.ReadAllText("pointcloud.vert");
            var fragmentShaderSource = File.ReadAllText("pointcloud.frag");
            shaderProgramPointCloud = new ShaderProgram();
            shaderProgramPointCloud.Create(gl, vertexShaderSource, fragmentShaderSource, null);
            shaderProgramPointCloud.BindAttributeLocation(gl, pointCloudAttributeIndexPosition, "in_Position");
            shaderProgramPointCloud.BindAttributeLocation(gl, pointCloudAttributeIndexColor, "in_Color");
            shaderProgramPointCloud.AssertValid(gl);
            createVerticesForPointCloud(gl);
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
            //  Create a perspective projection matrix.
            const float rads = (45.0f / 360.0f) * (float)Math.PI * 2.0f;
            projectionMatrix = glm.perspective(rads, (float)ActualWidth / (float)ActualHeight, 0.1f, 100.0f);

            //  Create a view matrix to move us back a bit.
            //viewMatrix = glm.translate(new mat4(1.0f), new vec3(0.0f, 0.0f, -10.0f));

            viewMatrix = glm.lookAt(position, lookat, up);

            //  Create a model matrix to make the model a little bigger.
            modelMatrix = glm.scale(new mat4(1.0f), new vec3(2.5f));
            //Transform();
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
                if (default(mat4).Equals(projectionMatrix))
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


                //floorTexture.Bind(gl);
                //floorShaderProgram.Bind(gl);
                //floorShaderProgram.SetUniformMatrix4(gl, "projectionMatrix", projectionMatrix.to_array());
                //floorShaderProgram.SetUniformMatrix4(gl, "viewMatrix", viewMatrix.to_array());
                //floorVertexBufferArray.Bind(gl);
                //gl.DrawArrays(OpenGL.GL_QUADS, 0, 4);
                //floorVertexBufferArray.Unbind(gl);
                //floorShaderProgram.Unbind(gl);


            }
        }

        private void openGLControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W)
            {
                vec3 forward = lookat - position;
                forward = glm.normalize(forward);
                vec3 diff = forward * new vec3(0.1f, 0.1f, 0.1f);
                position += diff;
                lookat += diff;
                viewMatrix = glm.lookAt(position, lookat, up);


            }
            if (e.Key == Key.S)
            {
                vec3 forward = lookat - position;
                forward = glm.normalize(forward);
                vec3 diff = forward * new vec3(0.1f, 0.1f, 0.1f);
                position -= diff;
                lookat += diff;
                viewMatrix = glm.lookAt(position, lookat, up);
            }
            if (e.Key == Key.A)
            {
                vec3 diff = new vec3(-0.1f, 0f, 0.0f);
                position += diff;
                lookat += diff;
                viewMatrix = glm.lookAt(position, lookat, up);
            }
            if (e.Key == Key.D)
            {
                vec3 diff = new vec3(0.1f, 0f, 0.0f);
                position += diff;
                lookat += diff;
                viewMatrix = glm.lookAt(position, lookat, up);
            }

            if (e.Key == Key.Space)
            {
                vec3 diff = glm.normalize(up) * new vec3(0.1f, 0.1f, 0.1f);
                position += diff;
                lookat += diff;
                viewMatrix = glm.lookAt(position, lookat, up);
            }

            if (e.Key == Key.C)
            {
                vec3 diff = glm.normalize(up) * new vec3(-0.1f, -0.1f, -0.1f);
                position += diff;
                lookat += diff;
                viewMatrix = glm.lookAt(position, lookat, up);
            }


            if (e.Key == Key.Q)
            {
                mat4 rot = glm.rotate(new mat4(1.0f), 0.025f, up);
                vec4 diff = (rot * new vec4(lookat - position, 1));
                lookat = position + new vec3(diff);
                viewMatrix = glm.lookAt(position, lookat, up);
            }

            if (e.Key == Key.E)
            {
                mat4 rot = glm.rotate(new mat4(1.0f), -0.025f, up);
                vec4 diff = (rot * new vec4(lookat - position, 1));
                lookat = position + new vec3(diff);
                viewMatrix = glm.lookAt(position, lookat, up);
            }


            if (e.Key == Key.R)
            {
                vec3 right = glm.cross(up, lookat - position);
                mat4 rot = glm.rotate(new mat4(1.0f), -0.025f, right);
                vec4 diff = (rot * new vec4(lookat - position, 1));
                lookat = position + new vec3(diff);
                viewMatrix = glm.lookAt(position, lookat, up);
            }

            if (e.Key == Key.T)
            {
                vec3 right = glm.cross(up, lookat - position);
                mat4 rot = glm.rotate(new mat4(1.0f), 0.025f, right);
                vec4 diff = (rot * new vec4(lookat - position, 1));
                lookat = position + new vec3(diff);
                viewMatrix = glm.lookAt(position, lookat, up);
            }

            if (e.Key == Key.F)
            {
                mat4 rot = glm.rotate(new mat4(1.0f), 0.025f, lookat - position);
                vec4 upNorm = rot * new vec4(up, 1);
                up = new vec3(upNorm);
                viewMatrix = glm.lookAt(position, lookat, up);
            }

            if (e.Key == Key.G)
            {
                mat4 rot = glm.rotate(new mat4(1.0f), -0.025f, lookat - position);
                vec4 upNorm = rot * new vec4(up, 1);
                up = new vec3(upNorm);
                viewMatrix = glm.lookAt(position, lookat, up);
            }

            //if (e.Key.Equals(Key.S))
            //{
            //    cameraPosSphere.Y -= rotationFactor;
            //}
            //else if (e.Key.Equals(Key.W))
            //{
            //    cameraPosSphere.Y += rotationFactor;
            //}
            //else if (e.Key.Equals(Key.A))
            //{
            //    cameraPosSphere.Z -= rotationFactor;
            //}
            //else if (e.Key.Equals(Key.D))
            //{
            //    cameraPosSphere.Z += rotationFactor;
            //}
            //else
            //{
            //    return;
            //}

            //cameraPos = GeometryHelper.SphericalToCartesian(cameraPosSphere);

            ////cameraPos.X += Center.X;
            ////cameraPos.Y += Center.Y;
            ////cameraPos.Z += Center.Z;
            //Transform();
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
            LoadPointCloud(pointCloudDictionary[activeClient]);
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
            //List<NullablePoint3D[]> pointClouds = GetPointClouds();
            showMerged = true;
            //fal
            //var kinect1CalPoints = pointClouds[0];
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
            //var kinect2CalPoints = pointClouds[1];
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

            //var A = GeometryHelper.GetTransformationAndRotation(kinect1CalPoints, kinect2CalPoints);

            //Matrix<double> rot = A.R;
            //Vector<double> translate = A.T;

            Matrix<double> rot = DenseMatrix.OfColumnArrays(new List<double[]>{
                    new[]{-0.01868811233840361,
                    -0.34501377370318648,
                    -0.93841155705389412},
                    new[]{0.5320510930426815,
                    0.79121684933040015,
                    -0.30149217523472005},
                    new[]{0.84650598866713045,
                    -0.50491721429434455,
                    0.16877860604923636	}
            });
            Vector<double> translate = DenseVector.OfArray(new[]{
                -1.875871904795692,	
                0.47458577514168565,	
                1.1320225857947943
            });



            //var a = rot * DenseVector.OfArray(new[] { kinect1CalPoints[0].X, kinect1CalPoints[0].Y, kinect1CalPoints[0].Z }) + translate;

            List<NullablePoint3D> transformedPointCloudList = new List<NullablePoint3D>();
            NullablePoint3D[] pointCloud1 = pointCloudDictionary[DataStore.Instance.KinectClients[0]];
            //NullablePoint3D[] pointCloud1 = FileHelper.ParsePCD(@"C:\asd\cloud1.pcd").ToArray();


            NullablePoint3D[] pointCloud2 = pointCloudDictionary[DataStore.Instance.KinectClients[1]];
            foreach (NullablePoint3D point in pointCloud1)
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
            var mergedCloud = new NullablePoint3D[pointCloud1.Length + transformedPointCloud.Length];
            pointCloud2.CopyTo(mergedCloud, 0);
            transformedPointCloud.CopyTo(mergedCloud, pointCloud2.Length);
            LoadPointCloud(mergedCloud);
            createVerticesForPointCloud(OpenGlControl.OpenGL);
            OpenGlControl.Focus();

            transformedPointCloud = transformedPointCloudList.ToArray();

            FileHelper.WritePCD(Converter.NullablePoint3DsToPoint3Ds(new List<NullablePoint3D>(pointCloud1)), @"C:\asd\kinect1cloud.pcd");
            FileHelper.WritePCD(Converter.NullablePoint3DsToPoint3Ds(new List<NullablePoint3D>(pointCloud2)), @"C:\asd\kinect2cloud.pcd");
            FileHelper.WritePCD(Converter.NullablePoint3DsToPoint3Ds(new List<NullablePoint3D>(transformedPointCloudList)), @"C:\asd\cloud1to2.pcd");
        }


        private void OpenGlControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenGlControl.Focus();

        }

        private void OpenGlControl_MouseMove(object sender, MouseEventArgs e)
        {
            
        }
    }
}