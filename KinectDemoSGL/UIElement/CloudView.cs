using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using KinectDemoSGL.UIElement.Model;
using KinectDemoSGL.Util;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;

namespace KinectDemoSGL.UIElement
{
    /// <summary>
    /// Interaction logic for CloudView.xaml
    /// </summary>
    public partial class CloudView : UserControl
    {
        private KinectSensor kinectSensor;

        private FrameDescription depthFrameDescription;

        public CameraSpacePoint[] AllCameraSpacePoints { get; set; }

        private ushort[] depthArray;

        private Workspace ActiveWorkspace { get; set; }

        public CloudView(KinectSensor kinectSensor)
        {
            ActiveWorkspace = new Workspace();

            this.kinectSensor = kinectSensor;

            depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            DepthFrameReader depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();

            depthFrameReader.FrameArrived += Reader_FrameArrived;

            int depthFrameWidth = depthFrameDescription.Width;

            int depthFrameHeight = depthFrameDescription.Height;

            depthArray = new ushort[depthFrameWidth * depthFrameHeight];

            InitializeComponent();
        }

        public void RefreshAllPointsView()
        {
            AllCameraSpacePoints = Generate3DPoints();

            Workspace all = new Workspace
            {
                Vertices = new ObservableCollection<Point>
                {
                    new Point(0,0),
                    new Point(0,depthFrameDescription.Height),
                    new Point(depthFrameDescription.Width, depthFrameDescription.Height),
                    new Point(depthFrameDescription.Width, 0)
                }
            };
            
            SetWorkspaceCloudAndCenter(all);
            SetCameraCenterAndShowCloud(AllPointsViewport, all);
        }

        public void SetWorkspace(Workspace workspaceSource)
        {

            ActiveWorkspace = workspaceSource;

            if (ActiveWorkspace.PointCloud == null)
            {
                SetWorkspaceCloudAndCenter(ActiveWorkspace);
            }

            SetCameraCenterAndShowCloud(MainViewPort, ActiveWorkspace);

            SetRealVertices(ActiveWorkspace);

            DrawFittedPlane();
        }

        public void SetRealVertices(Workspace workspace)
        {
            Vector<double> fittedPlaneVector = GeometryHelper.FitPlaneToPoints(workspace.PointCloud.ToArray());

            if (fittedPlaneVector == null)
            {
                return;
            }

            Point3D projectedPoint = GeometryHelper.ProjectPoint3DToPlane(workspace.PointCloud.First(), fittedPlaneVector);

            Vector<double> planeNormal = new DenseVector(new[] { fittedPlaneVector[0], fittedPlaneVector[1], fittedPlaneVector[2] });

            CameraSpacePoint[] csps = { new CameraSpacePoint() };

            Point[] vertices = workspace.Vertices.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Point vertex = vertices[i];

                kinectSensor.CoordinateMapper.MapDepthPointsToCameraSpace(
                    new[] {
                        new DepthSpacePoint {
                            X = (float)vertex.X,
                            Y = (float)vertex.Y
                        }
                    },
                    new ushort[] { 1 }, csps);

                Vector<double> pointOnPlane = new DenseVector(new[] { projectedPoint.X, projectedPoint.Y, projectedPoint.Z });
                Vector<double> pointOnLine = new DenseVector(new double[] { csps[0].X, csps[0].Y, csps[0].Z });

                double d = (pointOnPlane.Subtract(pointOnLine)).DotProduct(planeNormal) / (pointOnLine.DotProduct(planeNormal));

                Vector<double> intersection = pointOnLine + pointOnLine.Multiply(d);

                workspace.FittedVertices[i] = new Point3D(intersection[0], intersection[1], intersection[2]);
            }

            workspace.PlaneVector = fittedPlaneVector;
        }

        public void UpdatePointCloudAndCenter()
        {
            SetWorkspaceCloudAndCenter(ActiveWorkspace);
            SetCameraCenterAndShowCloud(MainViewPort, ActiveWorkspace);
        }

        private CameraSpacePoint[] Generate3DPoints()
        {
            int width = depthFrameDescription.Width;
            int height = depthFrameDescription.Height;
            int frameSize = width * height;
            AllCameraSpacePoints = new CameraSpacePoint[frameSize];
            DepthSpacePoint[] allDepthSpacePoints = new DepthSpacePoint[frameSize];

            ushort[] depths = new ushort[frameSize];

            for (int i = 0; i < height; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    int index = i * width + j;
                    allDepthSpacePoints[index] = new DepthSpacePoint { X = j, Y = i };
                    AllCameraSpacePoints[index] = new CameraSpacePoint();
                    depths[index] = depthArray[index];
                }
            }

            kinectSensor.CoordinateMapper.MapDepthPointsToCameraSpace(allDepthSpacePoints, depths, AllCameraSpacePoints);

            return AllCameraSpacePoints;
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
                drawTriangle(viewport, point, Colors.Black);
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

            Camera.Position = new Point3D(center.X, center.Y, -3);
        }

        private void drawTriangle(Viewport3D viewport, Point3D point, Color color)
        {
            drawTriangle(viewport, point, 0.005, color);
        }

        private void drawTriangle(Viewport3D viewport, Point3D point, double size, Color color)
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

        private void SetWorkspaceCloudAndCenter(Workspace workspace)
        {
            AllCameraSpacePoints = Generate3DPoints();

            Polygon polygon = new Polygon();
            PointCollection pointCollection = new PointCollection();
            foreach (Point p in workspace.Vertices)
            {
                pointCollection.Add(p);
            }

            polygon.Points = pointCollection;
            polygon.Stroke = Brushes.Black;
            polygon.Fill = Brushes.LightSeaGreen;
            polygon.StrokeThickness = 2;

            int height = (int)polygon.ActualHeight;
            int width = (int)polygon.ActualWidth;

            double sumX = 0;
            double sumY = 0;
            double sumZ = 0;
            double numberOfPoints = 0;

            List<Point3D> cameraSpacePoints = new List<Point3D>();
            List<DepthSpacePoint> dspList = new List<DepthSpacePoint>();
            foreach (CameraSpacePoint csp in AllCameraSpacePoints)
            {
                if (GeometryHelper.IsValidCameraPoint(csp))
                {

                    DepthSpacePoint dsp = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(csp);
                    dspList.Add(dsp);

                    if (GeometryHelper.InsidePolygon(polygon, new Point(dsp.X, dsp.Y)))
                    {
                        double x = csp.X;
                        double y = csp.Y;
                        double z = csp.Z;

                        sumX += x;
                        sumY += y;
                        sumZ += z;

                        numberOfPoints += 1;

                        cameraSpacePoints.Add(new Point3D(csp.X, csp.Y, csp.Z));

                    }
                }
            }

            workspace.Center = new Point3D(sumX / numberOfPoints, sumY / numberOfPoints, sumZ / numberOfPoints);

            workspace.PointCloud = new ObservableCollection<Point3D>(cameraSpacePoints);
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

        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                depthArray[i] = (ushort)(depth >= minDepth && depth <= maxDepth ? (depth) : 0);
            }
        }

        private void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {

                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // Note: In order to see the full range of depth (including the less reliable far field depth)
                        // we are setting maxDepth to the extreme potential depth threshold
                        ushort maxDepth = ushort.MaxValue;

                        // If you wish to filter by reliable depth distance, uncomment the following line:
                        //// maxDepth = depthFrame.DepthMaxReliableDistance

                        ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                    }
                }
            }
        }

    }
}
