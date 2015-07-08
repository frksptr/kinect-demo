using KinectDemo.Util;
using Microsoft.Kinect;
using SharpGL;
using SharpGL.SceneGraph;
using System;
using System.Collections.Generic;
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

namespace KinectDemoSGL.UIElement
{


    /// <summary>
    /// Interaction logic for RoomPointCloudView.xaml
    /// </summary>
    public partial class RoomPointCloudView : UserControl
    {
        public List<Point3D> FullPointCloud { get; set; }

        public Point3D Center { get; set; }

        float rquad = 0; 

        public RoomPointCloudView()
        {
            InitializeComponent();

        }
        
        private void OpenGLControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            //  Enable the OpenGL depth testing functionality.
            args.OpenGL.Enable(OpenGL.GL_DEPTH_TEST);
        }

        private void OpenGLControl_Resized(object sender, OpenGLEventArgs args)
        {
            // Get the OpenGL instance.
            OpenGL gl = args.OpenGL;

            // Load and clear the projection matrix.
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity();

            // Perform a perspective transformation
            gl.Perspective(45.0f, (float)gl.RenderContextProvider.Width /
                (float)gl.RenderContextProvider.Height,
                0.1f, 100.0f);

            // Load the modelview.
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
        }

        private void OpenGLControl_OpenGLDraw(object sender, OpenGLEventArgs args)
        {
            if (this.IsVisible)
            {
                if (FullPointCloud == null)
                {
                    return;
                }
                //  Get the OpenGL instance that's been passed to us.
                OpenGL gl = args.OpenGL;

                //  Clear the color and depth buffers.
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

                //  Reset the modelview matrix.
                gl.LoadIdentity();

                //  Move the geometry into a fairly central position.
                gl.Translate(-1.5f, 0.0f, -6.0f);
                gl.PointSize(1.0f);
                foreach (Point3D point in FullPointCloud)
                {
                    gl.Begin(OpenGL.GL_POINTS);
                    gl.Vertex(point.X, point.Y, point.Z);
                    gl.End();
                }
                //gl.LookAt(0, 0, -200, Center.X, Center.Y, Center.Z, 0, 1, 0);

                
                //  Flush OpenGL.
                gl.Flush();

                //  Rotate the geometry a bit.
                rquad -= 3.0f;
            }
        }
    }
}
