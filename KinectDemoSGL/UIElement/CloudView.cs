using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using Microsoft.Kinect;

namespace KinectDemoCommon.UIElement
{
    /// <summary>
    /// Interaction logic for CloudView.xaml
    /// </summary>
    public partial class CloudView : UserControl
    {
        public CameraSpacePoint[] AllCameraSpacePoints { get; set; }

        private Workspace ActiveWorkspace { get; set; }

        public CloudView()
        {
            ActiveWorkspace = new Workspace();

            InitializeComponent();
        }

        public void SetWorkspace(Workspace workspaceSource)
        {
            ActiveWorkspace = workspaceSource;

            Dispatcher.Invoke(() =>
            {
                SetCameraCenterAndShowCloud(MainViewPort, ActiveWorkspace);

                DrawFittedPlane();
            });
        }

        public void ClearScreen()
        {
            MainViewPort.Children.Clear();
        }

        private void SetCameraCenterAndShowCloud(Viewport3D viewport, Workspace workspace)
        {
            ClearScreen();

            foreach (Point3D point in workspace.PointCloud)
            {
                DrawTriangle(viewport, point, Colors.Black);
            }

            Point3D center = workspace.Center;

            XRotation.CenterX = center.X;
            XRotation.CenterY = center.Y;
            XRotation.CenterZ = center.Z;

            YRotation.CenterX = center.X;
            YRotation.CenterY = center.Y;
            YRotation.CenterZ = center.Z;

            Scale.CenterX = center.X;
            Scale.CenterY = center.Y;
            Scale.CenterZ = center.Z;

            Camera.Position = new Point3D(center.X, center.Y, -5);
        }

        private void DrawTriangle(Viewport3D viewport, Point3D point, Color color)
        {
            DrawTriangle(viewport, point, 0.005, color);
        }

        private void DrawTriangle(Viewport3D viewport, Point3D point, double size, Color color)
        {
            Model3DGroup triangle = new Model3DGroup();

            double x = point.X;
            double y = point.Y;
            double z = point.Z;

            Point3D p0 = new Point3D(x, y, z);
            Point3D p1 = new Point3D(x + size, y, z);
            Point3D p2 = new Point3D(x, y + size, z);

            triangle.Children.Add(GeometryHelper.CreateTriangleModel(p0, p2, p1, color));

            ModelVisual3D model = new ModelVisual3D();
            model.Content = triangle;
            viewport.Children.Add(model);
        }

        private void DrawFittedPlane()
        {

            Model3DGroup tetragon = new Model3DGroup();

            Point3D p0 = ActiveWorkspace.FittedVertices[0];
            Point3D p1 = ActiveWorkspace.FittedVertices[1];
            Point3D p2 = ActiveWorkspace.FittedVertices[2];
            Point3D p3 = ActiveWorkspace.FittedVertices[3];

            tetragon.Children.Add(GeometryHelper.CreateTriangleModel(p0, p2, p1, Colors.Red));
            tetragon.Children.Add(GeometryHelper.CreateTriangleModel(p2, p0, p3, Colors.Red));

            ModelVisual3D model = new ModelVisual3D();
            model.Content = tetragon;
            MainViewPort.Children.Add(model);
        }
    }
}
