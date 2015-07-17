﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Model;
using KinectDemoCommon.UIElement;
using Microsoft.Kinect;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;

namespace KinectDemoCommon
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
        private CloudView cloudView;

        // Displays skeleton model and indicates active workspace
        private BodyView bodyView;

        // Displays full point floud (with fitted planes to workspaces)
        private RoomPointCloudView roomPointCloudView;

        private const int MARGIN = 5;

        private string statusText;

        private FrameSize depthFrameSize;

        private Workspace activeWorkspace;

        private ObservableCollection<Workspace> workspaceList = new ObservableCollection<Workspace>();
        private KinectServer kinectServer;

        public MainWindow()
        {

            InitializeComponent();

            kinectServer = KinectServer.Instance;

            kinectServer.WorkspaceUpdated += kinectServer_WorkspaceUpdated;
            kinectServer.DepthDataArrived += kinectServer_DepthDataArrived;

            activeWorkspace = new Workspace()
            {
                Name = "asd",
                Vertices = new ObservableCollection<Point>
                {
                    new Point(0,0),
                    new Point(0,50),
                    new Point(50,50),
                    new Point(50,0)
                }
            };

            AddCameraWorkspace();

            cloudView = new CloudView();

            WorkspacePointCloudHolder.Children.Add(cloudView);

            bodyView = new BodyView();

            HandCheckBodyViewHolder.Children.Add(bodyView);

            //roomPointCloudView = new RoomPointCloudView();

            //RoomPointCloudHolder.Children.Add(roomPointCloudView);

            //roomPointCloudView.DataContext = cloudView.AllCameraSpacePoints;

            //WorkspaceList.ItemsSource = workspaceList;

            //EditWorkspace.DataContext = activeWorkspace;


        }

        private void kinectServer_DepthDataArrived(KinectDemoMessage message)
        {
            depthFrameSize = ((DepthStreamMessage)message).DepthFrameSize;
            kinectServer.DepthDataArrived -= kinectServer_DepthDataArrived;
        }

        private void kinectServer_WorkspaceUpdated(Messages.KinectDemoMessage message)
        {
            WorkspaceMessage msg = (WorkspaceMessage)message;
            cloudView.SetWorkspace(new Workspace()
            {
                Vertices = new ObservableCollection<Point>(msg.Vertices),
                PointCloud = msg.PointCloud,
                FittedVertices = msg.Vertices3D
            });
        }

        private void AddCameraWorkspace()
        {
            cameraWorkspace = new CameraWorkspace();
            cameraWorkspace.MouseLeftButtonDown += cameraWorkspace_MouseLeftButtonDown;

            CameraHolder.Children.Add(cameraWorkspace);
        }



        private void cameraWorkspace_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FocusManager.GetFocusedElement(this) is TextBox)
            {
                TextBox focusedTextBox = (TextBox)FocusManager.GetFocusedElement(this);
                if (focusedTextBox != null)
                {
                    // Get Depth coordinates from clicked point
                    double actualWidth = cameraWorkspace.ActualWidth;
                    double actualHeight = cameraWorkspace.ActualHeight;

                    double x = e.GetPosition(cameraWorkspace).X;
                    double y = e.GetPosition(cameraWorkspace).Y;

                    if (depthFrameSize != null)
                    {
                        focusedTextBox.Text = (int)(x * depthFrameSize.Width / actualWidth) + "," + (int)(y * depthFrameSize.Height / actualHeight);
                    }
                }

                TraversalRequest tRequest = new TraversalRequest(FocusNavigationDirection.Next);
                focusedTextBox.MoveFocus(tRequest);
            }
        }

        public string StatusText
        {
            get
            {
                return statusText;
            }

            set
            {
                if (statusText != value)
                {
                    statusText = value;

                    // notify any bound elements that the text has changed
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        private void HandCheck_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //bodyView.WorkspaceList = workspaceList;
        }

        private void RoomPointCloudHolder_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //CameraSpacePoint[] csps = cloudView.AllCameraSpacePoints;
            //List<Point3D> pointCloud = GeometryHelper.CameraSpacePointsToPoint3Ds(csps);

            //roomPointCloudView.Center = GeometryHelper.CalculateCenterPoint(pointCloud);

            //roomPointCloudView.FullPointCloud = pointCloud;

            //roomPointCloudView.WorkspaceList = workspaceList;
        }

        private void AddWorkspace(object sender, RoutedEventArgs e)
        {
            if (!workspaceList.Contains(activeWorkspace))
            {
                workspaceList.Add(activeWorkspace);
            }
            kinectServer.AddWorkspace(activeWorkspace);
            activeWorkspace = new Workspace();
            EditWorkspace.DataContext = activeWorkspace;
            WorkspaceList.Items.Refresh();
        }

        private void WorkspaceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((Workspace)WorkspaceList.SelectedItem == null)
            {
                return;
            }
            activeWorkspace = (Workspace)WorkspaceList.SelectedItem;
            EditWorkspace.DataContext = activeWorkspace;
        }
        private void RemoveWorkspace(object sender, RoutedEventArgs e)
        {
            //workspaceList.Remove((Workspace)WorkspaceList.SelectedItem);
            //activeWorkspace = new Workspace();
            //cloudView.ClearScreen();
        }
    }
}
