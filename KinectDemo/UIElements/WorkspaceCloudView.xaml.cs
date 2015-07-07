using KinectDemo.Util;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace KinectDemo.UIElements
{
    /// <summary>
    /// Interaction logic for CloudView.xaml
    /// </summary>
    public partial class CloudView : UserControl, INotifyPropertyChanged
    {
        private KinectSensor kinectSensor;

        private FrameDescription depthFrameDescription = null;

        private CameraSpacePoint[] allCameraSpacePoints;

        private ushort[] depthArray = null;

        private Workspace ActiveWorkspace { get; set; }

        public Point3D[] Data { get; set; }

        public double[] Values { get; set; }

        public CloudView(KinectSensor kinectSensor)
        {
            this.ActiveWorkspace = new Workspace();

            this.kinectSensor = kinectSensor;

            this.depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            DepthFrameReader depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();

            depthFrameReader.FrameArrived += this.Reader_FrameArrived;

            int depthFrameWidth = this.depthFrameDescription.Width;

            int depthFrameHeight = this.depthFrameDescription.Height;

            this.depthArray = new ushort[depthFrameWidth * depthFrameHeight];

            InitializeComponent();

            this.DataContext = this;

            UpdateModel();            
        }

        public Model3DGroup Lights
        {
            get
            {
                var group = new Model3DGroup();
                group.Children.Add(new AmbientLight(Colors.White));
                return group;
            }
        }

        public Brush SurfaceBrush
        {
            get
            {
                // return BrushHelper.CreateGradientBrush(Colors.White, Colors.Blue);
                return HelixToolkit.Wpf.GradientBrushes.RainbowStripes;
                // return GradientBrushes.BlueWhiteRed;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void UpdateModel()
        {
            if (this.ActiveWorkspace.PointCloud != null)
            {
                Data = this.ActiveWorkspace.PointCloud.ToArray();
            }
            else
            {
                Data = Enumerable.Range(0, 7 * 7 * 7).Select(i => new Point3D(i % 7, (i % 49) / 7, i / 49)).ToArray();
            }

            var rnd = new Random();
            this.Values = Data.Select(d => rnd.NextDouble()).ToArray();

            RaisePropertyChanged("Values");
            RaisePropertyChanged("Data");
            RaisePropertyChanged("SurfaceBrush");
        }

        protected void RaisePropertyChanged(string property)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        public void refreshAllPointsView()
        {
            this.allCameraSpacePoints = generate3DPoints();

            Workspace all = new Workspace()
            {
                Vertices = new ObservableCollection<Point>()
                {
                    new Point(0,0),
                    new Point(0,depthFrameDescription.Height),
                    new Point(depthFrameDescription.Width, depthFrameDescription.Height),
                    new Point(depthFrameDescription.Width, 0)
                }
            };
            
            setWorkspaceCloudAndCenter(all);
            //setCameraCenterAndShowCloud(this.AllPointsViewport, all);


        }

        public void setWorkspace(Workspace workspaceSource)
        {

            ActiveWorkspace = workspaceSource;

            if (ActiveWorkspace.PointCloud == null)
            {
                setWorkspaceCloudAndCenter(ActiveWorkspace);
            }
            //setCameraCenterAndShowCloud(MainViewPort, ActiveWorkspace);

            //setRealVertices(ActiveWorkspace);
            UpdateModel();
            //drawFittedPlane();

            //refreshAllPointsView();
        }

        public void setRealVertices(Workspace workspace)
        {
            Vector<double> fittedPlaneVector = GeometryHelper.fitPlaneToPoints(workspace.PointCloud.ToArray());

            Point3D projectedPoint = GeometryHelper.projectPoint3DToPlane(workspace.PointCloud.First(), fittedPlaneVector);

            Vector<double> planeNormal = new DenseVector(new double[] { fittedPlaneVector[0], fittedPlaneVector[1], fittedPlaneVector[2] });

            CameraSpacePoint[] csps = new CameraSpacePoint[] { new CameraSpacePoint() };

            Point[] vertices = workspace.Vertices.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Point vertex = vertices[i];

                this.kinectSensor.CoordinateMapper.MapDepthPointsToCameraSpace(
                    new DepthSpacePoint[] {
                        new DepthSpacePoint {
                            X = (float)vertex.X,
                            Y = (float)vertex.Y
                        },
                    },
                    new ushort[] { 1 }, csps);

                Vector<double> pointOnPlane = new DenseVector(new double[] { projectedPoint.X, projectedPoint.Y, projectedPoint.Z });
                Vector<double> pointOnLine = new DenseVector(new double[] { csps[0].X, csps[0].Y, csps[0].Z });

                double d = (pointOnPlane.Subtract(pointOnLine)).DotProduct(planeNormal) / (pointOnLine.DotProduct(planeNormal));

                Vector<double> intersection = pointOnLine + pointOnLine.Multiply(d);

                workspace.FittedVertices[i] = new Point3D(intersection[0], intersection[1], intersection[2]);
            }

            workspace.planeVector = fittedPlaneVector;
        }

        public void updatePointCloudAndCenter()
        {
            setWorkspaceCloudAndCenter(ActiveWorkspace);
            //setCameraCenterAndShowCloud(MainViewPort, ActiveWorkspace);
        }

        private CameraSpacePoint[] generate3DPoints()
        {
            int width = this.depthFrameDescription.Width;
            int height = this.depthFrameDescription.Height;
            int frameSize = width * height;
            allCameraSpacePoints = new CameraSpacePoint[frameSize];
            DepthSpacePoint[] allDepthSpacePoints = new DepthSpacePoint[frameSize];

            ushort[] depths = new ushort[frameSize];

            for (int i = 0; i < height; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    int index = i * width + j;
                    allDepthSpacePoints[index] = new DepthSpacePoint() { X = j, Y = i };
                    allCameraSpacePoints[index] = new CameraSpacePoint();
                    depths[index] = this.depthArray[index];
                }
            }

            this.kinectSensor.CoordinateMapper.MapDepthPointsToCameraSpace(allDepthSpacePoints, depths, allCameraSpacePoints);

            return allCameraSpacePoints;
        }

        private void setCameraCenterAndShowCloud(Viewport3D viewport, Workspace workspace)
        {
            viewport.Children.Clear();

            foreach (Point3D point in workspace.PointCloud)
            {
                drawTriangle(viewport, point, Colors.Black);
            }

            Point3D center = workspace.Center;

            //this.xRotation.CenterX = center.X;
            //this.xRotation.CenterY = center.Y;
            //this.xRotation.CenterZ = center.Z;

            //this.yRotation.CenterX = center.X;
            //this.yRotation.CenterY = center.Y;
            //this.yRotation.CenterZ = center.Z;

            //this.scale.CenterX = center.X;
            //this.scale.CenterY = center.Y;
            //this.scale.CenterZ = center.Z;

            //this.Camera.Position = new Point3D(center.X, center.Y, -3);
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

        private void setWorkspaceCloudAndCenter(Workspace workspace)
        {
            this.allCameraSpacePoints = generate3DPoints();

            Polygon polygon = new Polygon();
            PointCollection pointCollection = new PointCollection();
            foreach (Point p in workspace.Vertices)
            {
                pointCollection.Add(p);
            }

            polygon.Points = pointCollection;
            polygon.Stroke = System.Windows.Media.Brushes.Black;
            polygon.Fill = System.Windows.Media.Brushes.LightSeaGreen;
            polygon.StrokeThickness = 2;

            int height = (int)polygon.ActualHeight;
            int width = (int)polygon.ActualWidth;

            double sumX = 0;
            double sumY = 0;
            double sumZ = 0;
            double numberOfPoints = 0;

            List<Point3D> cameraSpacePoints = new List<Point3D>();
            List<DepthSpacePoint> dspList = new List<DepthSpacePoint>();
            foreach (CameraSpacePoint csp in allCameraSpacePoints)
            {
                if (GeometryHelper.isValidCameraPoint(csp))
                {

                    DepthSpacePoint dsp = this.kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(csp);
                    dspList.Add(dsp);

                    if (GeometryHelper.insidePolygon(polygon, new Point(dsp.X, dsp.Y)))
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

            workspace.PointCloud = new System.Collections.ObjectModel.ObservableCollection<Point3D>(cameraSpacePoints);
        }

        private void drawFittedPlane()
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
            //this.MainViewPort.Children.Add(model);
        }

        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
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
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // Note: In order to see the full range of depth (including the less reliable far field depth)
                        // we are setting maxDepth to the extreme potential depth threshold
                        ushort maxDepth = ushort.MaxValue;

                        // If you wish to filter by reliable depth distance, uncomment the following line:
                        //// maxDepth = depthFrame.DepthMaxReliableDistance

                        this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                    }
                }
            }
        }

    }
}
