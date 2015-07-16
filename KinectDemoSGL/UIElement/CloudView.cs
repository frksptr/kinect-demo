using System.Windows.Controls;
using KinectDemoCommon.Model;
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

        //private readonly KinectStreamer kinectStreamer;

        public CloudView()
        {
            ActiveWorkspace = new Workspace();

            InitializeComponent();

            //kinectStreamer = KinectStreamer.Instance;

        }

        //public void SetWorkspace(Workspace workspaceSource)
        //{

        //    ActiveWorkspace = workspaceSource;

        //    if (ActiveWorkspace.PointCloud == null)
        //    {
        //        SetWorkspaceCloudAndCenter(ActiveWorkspace);
        //    }

        //    SetCameraCenterAndShowCloud(MainViewPort, ActiveWorkspace);

        //    SetRealVertices(ActiveWorkspace);

        //    DrawFittedPlane();
        //}

        //public void SetRealVertices(Workspace workspace)
        //{
        //    Vector<double> fittedPlaneVector = GeometryHelper.FitPlaneToPoints(workspace.PointCloud.ToArray());

        //    if (fittedPlaneVector == null)
        //    {
        //        return;
        //    }

        //    Point3D projectedPoint = GeometryHelper.ProjectPoint3DToPlane(workspace.PointCloud.First(), fittedPlaneVector);

        //    Vector<double> planeNormal = new DenseVector(new[] { fittedPlaneVector[0], fittedPlaneVector[1], fittedPlaneVector[2] });

        //    CameraSpacePoint[] csps = { new CameraSpacePoint() };

        //    Point[] vertices = workspace.Vertices.ToArray();

        //    for (int i = 0; i < vertices.Length; i++)
        //    {
        //        Point vertex = vertices[i];

        //        kinectStreamer.CoordinateMapper.MapDepthPointsToCameraSpace(
        //            new[] {
        //                new DepthSpacePoint {
        //                    X = (float)vertex.X,
        //                    Y = (float)vertex.Y
        //                }
        //            },
        //            new ushort[] { 1 }, csps);

        //        Vector<double> pointOnPlane = new DenseVector(new[] { projectedPoint.X, projectedPoint.Y, projectedPoint.Z });
        //        Vector<double> pointOnLine = new DenseVector(new double[] { csps[0].X, csps[0].Y, csps[0].Z });

        //        double d = (pointOnPlane.Subtract(pointOnLine)).DotProduct(planeNormal) / (pointOnLine.DotProduct(planeNormal));

        //        Vector<double> intersection = pointOnLine + pointOnLine.Multiply(d);

        //        workspace.FittedVertices[i] = new Point3D(intersection[0], intersection[1], intersection[2]);
        //    }

        //    workspace.PlaneVector = fittedPlaneVector;
        //}

        //public void UpdatePointCloudAndCenter()
        //{
        //    SetWorkspaceCloudAndCenter(ActiveWorkspace);
        //    SetCameraCenterAndShowCloud(MainViewPort, ActiveWorkspace);
        //}

        //public void ClearScreen()
        //{
        //    MainViewPort.Children.Clear();
        //}

        //private void SetCameraCenterAndShowCloud(Viewport3D viewport, Workspace workspace)
        //{
        //    ClearScreen();
            
        //    foreach (Point3D point in workspace.PointCloud)
        //    {
        //        DrawTriangle(viewport, point, Colors.Black);
        //    }

        //    Point3D center = workspace.Center;

        //    XRotation.CenterX = center.X;
        //    XRotation.CenterY = center.Y;
        //    XRotation.CenterZ = center.Z;

        //    YRotation.CenterX = center.X;
        //    YRotation.CenterY = center.Y;
        //    YRotation.CenterZ = center.Z;

        //    Scale.CenterX = center.X;
        //    Scale.CenterY = center.Y;
        //    Scale.CenterZ = center.Z;

        //    Camera.Position = new Point3D(center.X, center.Y, -3);
        //}

        //private void DrawTriangle(Viewport3D viewport, Point3D point, Color color)
        //{
        //    DrawTriangle(viewport, point, 0.005, color);
        //}

        //private void DrawTriangle(Viewport3D viewport, Point3D point, double size, Color color)
        //{
        //    Model3DGroup triangle = new Model3DGroup();

        //    double x = point.X;
        //    double y = point.Y;
        //    double z = point.Z;

        //    Point3D p0 = new Point3D(x, y, z);
        //    Point3D p1 = new Point3D(x + size, y, z);
        //    Point3D p2 = new Point3D(x, y + size, z);

        //    triangle.Children.Add(GeometryHelper.CreateTriangleModel(p0, p2, p1, color));

        //    ModelVisual3D model = new ModelVisual3D();
        //    model.Content = triangle;
        //    viewport.Children.Add(model);
        //}

        //private void SetWorkspaceCloudAndCenter(Workspace workspace)
        //{
        //    AllCameraSpacePoints = kinectStreamer.GenerateFullPointCloud();

        //    Polygon polygon = new Polygon();
        //    PointCollection pointCollection = new PointCollection();
        //    foreach (Point p in workspace.Vertices)
        //    {
        //        pointCollection.Add(p);
        //    }

        //    polygon.Points = pointCollection;
        //    polygon.Stroke = Brushes.Black;
        //    polygon.Fill = Brushes.LightSeaGreen;
        //    polygon.StrokeThickness = 2;

        //    int height = (int)polygon.ActualHeight;
        //    int width = (int)polygon.ActualWidth;

        //    double sumX = 0;
        //    double sumY = 0;
        //    double sumZ = 0;
        //    double numberOfPoints = 0;

        //    List<Point3D> cameraSpacePoints = new List<Point3D>();
        //    List<DepthSpacePoint> dspList = new List<DepthSpacePoint>();
        //    foreach (CameraSpacePoint csp in AllCameraSpacePoints)
        //    {
        //        if (GeometryHelper.IsValidCameraPoint(csp))
        //        {

        //            DepthSpacePoint dsp = kinectStreamer.CoordinateMapper.MapCameraPointToDepthSpace(csp);
        //            dspList.Add(dsp);

        //            if (GeometryHelper.InsidePolygon(polygon, new Point(dsp.X, dsp.Y)))
        //            {
        //                double x = csp.X;
        //                double y = csp.Y;
        //                double z = csp.Z;

        //                sumX += x;
        //                sumY += y;
        //                sumZ += z;

        //                numberOfPoints += 1;

        //                cameraSpacePoints.Add(new Point3D(csp.X, csp.Y, csp.Z));

        //            }
        //        }
        //    }

        //    workspace.Center = new Point3D(sumX / numberOfPoints, sumY / numberOfPoints, sumZ / numberOfPoints);

        //    workspace.PointCloud = new ObservableCollection<Point3D>(cameraSpacePoints);
        //}

        //private void DrawFittedPlane()
        //{

        //    Model3DGroup tetragon = new Model3DGroup();

        //    Point3D p0 = ActiveWorkspace.FittedVertices[0];
        //    Point3D p1 = ActiveWorkspace.FittedVertices[1];
        //    Point3D p2 = ActiveWorkspace.FittedVertices[2];
        //    Point3D p3 = ActiveWorkspace.FittedVertices[3];

        //    tetragon.Children.Add(GeometryHelper.CreateTriangleModel(p0, p2, p1, Colors.Red));
        //    tetragon.Children.Add(GeometryHelper.CreateTriangleModel(p2, p0, p3, Colors.Red));

        //    ModelVisual3D model = new ModelVisual3D();
        //    model.Content = tetragon;
        //    MainViewPort.Children.Add(model);
        //}
    }
}
