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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using Microsoft.Kinect;
using KinectDemo.UIElements;
using System.ComponentModel;
using MathNet.Numerics.LinearAlgebra;
using KinectDemo.Util;
using System.Windows.Media.Media3D;
using MathNet.Numerics.LinearAlgebra.Double;
using KinectDemoSGL.UIElement;
namespace KinectDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        // Displays color image on which to select workspaces
        private CameraWorkspace cameraWorkspace;

        // Displays point clouds
        private CloudView workspaceCloudView;

        // Displays skeleton model and indicates active workspace
        private BodyView bodyView;

        // Displays full point floud (with fitted planes to workspaces)
        private RoomPointCloudView roomPointCloudView;

        private const int MARGIN = 5;

        private string statusText = null;

        private KinectSensor kinectSensor;

        private Workspace activeWorkspace = new Workspace()
        {
            Name = "asd",
            Vertices = new ObservableCollection<Point>(){
                    new Point(0,0),
                    new Point(0,50),
                    new Point(50,50),
                    new Point(50,0)
                }
        };


        private ObservableCollection<Workspace> workspaceList = new ObservableCollection<Workspace>() { 
            new Workspace()
        };

        public MainWindow()
        {
            InitializeComponent();

            addCameraWorkspace();

            this.kinectSensor = KinectSensor.GetDefault();

            workspaceCloudView = new CloudView(this.kinectSensor);

            workspacePointCloudHolder.Children.Add(workspaceCloudView);

            bodyView = new BodyView(this.kinectSensor);

            handCheck_BodyViewHolder.Children.Add(bodyView);

            roomPointCloudView = new RoomPointCloudView();

            RoomPointCloudHolder.Children.Add(roomPointCloudView);

            roomPointCloudView.DataContext = workspaceCloudView.AllCameraSpacePoints;

            WorkspaceList.ItemsSource = workspaceList;

            EditWorkspace.DataContext = activeWorkspace;
        }

        private void addCameraWorkspace()
        {
            cameraWorkspace = new CameraWorkspace(KinectSensor.GetDefault());
            cameraWorkspace.MouseLeftButtonDown += cameraWorkspace_MouseLeftButtonDown;

            cameraHolder.Children.Add(cameraWorkspace);
        }

// select workspace            this.workspaceCloudView.setWorkspace(activeWorkspace.Workspace);

        private void cameraWorkspace_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FocusManager.GetFocusedElement(this) is TextBox)
            {
                TextBox focusedTextBox = (TextBox)FocusManager.GetFocusedElement(this);
                if (focusedTextBox != null)
                {
                    // Get Depth coordinates from clicked point
                    double actualWidth = this.cameraWorkspace.ActualWidth;
                    double actualHeight = this.cameraWorkspace.ActualHeight;
                    
                    double x = e.GetPosition(this.cameraWorkspace).X;
                    double y = e.GetPosition(this.cameraWorkspace).Y;

                    int depthWidth = cameraWorkspace.depthFrameSize[0];
                    int depthHeight = cameraWorkspace.depthFrameSize[1];

                    focusedTextBox.Text = (int)((x / actualWidth) * depthWidth) + "," + (int)((y / actualHeight) * depthHeight);
                }
                
                TraversalRequest tRequest = new TraversalRequest(FocusNavigationDirection.Next);
                focusedTextBox.MoveFocus(tRequest);
            }
        }

        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        private void HandCheck_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bodyView.workspaceList = this.workspaceList;
        }

        private void RoomPointCloudHolder_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CameraSpacePoint[] csps = workspaceCloudView.AllCameraSpacePoints;
            List<Point3D> pointCloud = GeometryHelper.cameraSpacePointsToPoint3Ds(csps);

            roomPointCloudView.Center = GeometryHelper.calculateCenterPoint(pointCloud);

            roomPointCloudView.FullPointCloud = pointCloud;
        }

        private void addWorkspace(object sender, RoutedEventArgs e)
        {
            if (!workspaceList.Contains(activeWorkspace))
            {
                workspaceList.Add(activeWorkspace);
            }
            activeWorkspace = new Workspace();
            EditWorkspace.DataContext = activeWorkspace;
            WorkspaceList.Items.Refresh();
        }

        private void WorkspaceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            activeWorkspace = (Workspace)WorkspaceList.SelectedItem;
            EditWorkspace.DataContext = activeWorkspace;
        }
        private void removeWorkspace(object sender, RoutedEventArgs e)
        {
            workspaceList.Remove((Workspace)WorkspaceList.SelectedItem);
        }
    }
}
