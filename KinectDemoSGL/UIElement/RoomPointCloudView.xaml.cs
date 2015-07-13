using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using KinectDemoSGL.UIElement.Model;
using KinectDemoSGL.Util;
using SharpGL;
using SharpGL.SceneGraph;

namespace KinectDemoSGL.UIElement
{


    /// <summary>
    /// Interaction logic for RoomPointCloudView.xaml
    /// </summary>
    public partial class RoomPointCloudView : UserControl
    {
        public List<Point3D> FullPointCloud { get; set; }

        public ObservableCollection<Workspace> WorkspaceList { get; set; }

        public Point3D Center { get; set; }

        double rotationFactor = 0.1;

        double zoomFactor = 0.1;

        private Point3D cameraPosSphere;

        private Point3D cameraPos;

        public RoomPointCloudView()
        {
            InitializeComponent();
        }
        
        private void OpenGLControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            double radius = -5;
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
                if (FullPointCloud == null)
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
                foreach (Point3D point in FullPointCloud)
                {
                    gl.Vertex(point.X, point.Y, point.Z);
                }

                gl.End();

                gl.Begin(OpenGL.GL_TRIANGLES);
                gl.Color(0-0f, 1.0f, 0.0f);
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

    }
}
