using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using KinectDemoCommon.UIElement.Model;
using KinectDemoCommon.Util;
using SharpGL;
using SharpGL.SceneGraph;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

namespace KinectDemoCommon.UIElement
{


    /// <summary>
    /// Interaction logic for RoomPointCloudView.xaml
    /// </summary>
    public partial class RoomPointCloudView : UserControl
    {
        public List<Point3D> FullPointCloud
        {
            get
            {
                return fullPointCloud;
            }
            set
            {
                fullPointCloud = value;
            }
        }

        private List<Point3D> fullPointCloud;
        uint[] vertexBuffer = new uint[1];

        uint shaderProgram;


        public ObservableCollection<Workspace> WorkspaceList { get; set; }

        public Point3D Center { get; set; }

        double rotationFactor = 0.1;

        double zoomFactor = 0.5;

        private Point3D cameraPosSphere;

        private Point3D cameraPos;

        public RoomPointCloudView()
        {
            InitializeComponent();
            
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
            refreshVertexBuffer();
            initializeProgram();

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


            //gl.LookAt(cameraPos.X, cameraPos.Y, cameraPos.Z, Center.X, Center.Y, Center.Z, 0, 1, 0);

            // Load the modelview.
            //gl.MatrixMode(OpenGL.GL_MODELVIEW);
        }

        private void OpenGLControl_OpenGLDraw(object sender, OpenGLEventArgs args)
        {
            if (IsVisible)
            {
                if (FullPointCloud == null)
                {
                    return;
                }
                //  Get the OpenGL instance that's been passed to us.
                OpenGL gl = args.OpenGL;

                gl.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                gl.LoadIdentity();
                //gl.UseProgram(shaderProgram);
                gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vertexBuffer[0]);
                gl.EnableVertexAttribArray(0);
                gl.VertexAttribPointer(0, 4, OpenGL.GL_FLOAT, false, 0, IntPtr.Zero);
                gl.DrawArrays(OpenGL.GL_POINTS, 0, FullPointCloud.Count);
                gl.DisableVertexAttribArray(0);
                //gl.UseProgram(0);
                gl.Flush();


                gl.Begin(OpenGL.GL_TRIANGLES);
                gl.Color(0 - 0f, 1.0f, 0.0f);
                foreach (Workspace workspace in WorkspaceList)
                {
                    ObservableCollection<Point3D> vertices = workspace.FittedVertices;
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
            }
        }
        private float angle = 0.0f;
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
            //Transform();
        }

        private void openGLControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == false)
            {
            }
            else if ((bool)e.NewValue)
            {
                OpenGlControl.Focus();
            }

        }

        private void refreshVertexBuffer()
        {
            OpenGL gl = OpenGlControl.OpenGL;
            gl.GenBuffers(1, vertexBuffer);
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vertexBuffer[0]);
            List<float> verticesList = new List<float>();
            foreach (Point3D point in fullPointCloud)
            {
                verticesList.Add((float)point.X);
                verticesList.Add((float)point.Y);
                verticesList.Add((float)point.Z);
            }
            var vertices = verticesList.ToArray();
            unsafe
            {
                fixed (float* verts = vertices)
                {
                    var ptr = new IntPtr(verts);
                    gl.BufferData(OpenGL.GL_ARRAY_BUFFER, vertices.Length * sizeof(float), ptr, OpenGL.GL_STATIC_DRAW);
                }
            }
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
        }


        private uint createShader(uint eShaderType, string strShaderFile)
        {
            OpenGL gl = OpenGlControl.OpenGL;
            uint shader = gl.CreateShader(eShaderType);
            string strFileData = File.ReadAllText(strShaderFile);
            gl.ShaderSource(shader, strFileData);
            gl.CompileShader(shader);
            shaderErrorInfo(shader);
            return shader;
        }

        private void initializeProgram()
        {
            OpenGL gl = OpenGlControl.OpenGL;
            List<uint> shaderList = new List<uint>();

            shaderList.Add(createShader(OpenGL.GL_VERTEX_SHADER, "basic.vert"));
            shaderList.Add(createShader(OpenGL.GL_FRAGMENT_SHADER, "basic.frag"));

            shaderProgram = createProgram(shaderList);

            foreach (uint shader in shaderList)
                gl.DeleteShader(shader);

        }

        private uint createProgram(List<uint> shaderList)
        {
            OpenGL gl = OpenGlControl.OpenGL;
            uint program = gl.CreateProgram();

            foreach (uint shader in shaderList)
                gl.AttachShader(program, shader);

            gl.LinkProgram(program);

            programErrorInfo(program);

            foreach (uint shader in shaderList)
                gl.DetachShader(program, shader);

            return program;
        }

        private bool shaderErrorInfo(uint shaderId)
        {
            OpenGL gl = OpenGlControl.OpenGL;
            StringBuilder builder = new StringBuilder(2048);
            gl.GetShaderInfoLog(shaderId, 2048, IntPtr.Zero, builder);
            string res = builder.ToString();
            if (!res.Equals(""))
            {
                System.Console.WriteLine(res);
                return false;
            }

            return true;
        }


        private bool programErrorInfo(uint programId)
        {
            OpenGL gl = OpenGlControl.OpenGL;
            StringBuilder builder = new StringBuilder(2048);
            gl.GetProgramInfoLog(programId, 2048, IntPtr.Zero, builder);
            string res = builder.ToString();
            if (!res.Equals(""))
            {
                System.Console.WriteLine(res);
                return false;
            }

            return true;
        }

    }
}
