using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using KinectDemoCommon;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using KinectDemoSGL.UIElement;

namespace KinectDemoSGL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //  TODO: const strings to resources

        public event PropertyChangedEventHandler PropertyChanged;

        // Displays color image on which to select workspaces
        private DefineWorkspaceView defineWorkspaceView;

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
        private MessageProcessor messageProcessor;
        private DataStore dataStore = DataStore.Instance;

        public MainWindow()
        {

            InitializeComponent();

            kinectServer = KinectServer.Instance;
            messageProcessor = kinectServer.MessageProcessor;

            messageProcessor.WorkspaceUpdated += kinectServer_WorkspaceUpdated;
            messageProcessor.DepthDataArrived += kinectServer_DepthDataArrived;
            messageProcessor.TextMessageArrived += kinectServer_TextMessageArrived;

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

            roomPointCloudView = new RoomPointCloudView();

            RoomPointCloudHolder.Children.Add(roomPointCloudView);

            WorkspaceList.ItemsSource = workspaceList;

            EditWorkspace.DataContext = activeWorkspace;

        }

        private void kinectServer_TextMessageArrived(KinectDemoMessage message, KinectClient client)
        {
            Dispatcher.Invoke(() =>
            {
                ClientMessageBox.Text += "\nFrom " + client.Name + ":\n" + ((TextMessage)message).Text;
            });
        }

        private void kinectServer_DepthDataArrived(KinectDemoMessage message, KinectClient client)
        {
            depthFrameSize = ((DepthStreamMessage)message).DepthFrameSize;
            messageProcessor.DepthDataArrived -= kinectServer_DepthDataArrived;
        }

        private void kinectServer_WorkspaceUpdated(KinectDemoMessage message, KinectClient client)
        {
            WorkspaceMessage msg = (WorkspaceMessage)message;
            cloudView.SetWorkspace(dataStore.GetWorkspace(msg.ID));
        }

        private void AddCameraWorkspace()
        {
            defineWorkspaceView = new DefineWorkspaceView();
            defineWorkspaceView.MouseLeftButtonDown += DefineWorkspaceViewMouseLeftButtonDown;

            CameraHolder.Children.Add(defineWorkspaceView);
        }



        private void DefineWorkspaceViewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FocusManager.GetFocusedElement(this) is TextBox)
            {

                TextBox focusedTextBox = (TextBox)FocusManager.GetFocusedElement(this);
                if (focusedTextBox != null)
                {
                    // Get Depth coordinates from clicked point
                    double actualWidth = defineWorkspaceView.ActualWidth;
                    double actualHeight = defineWorkspaceView.ActualHeight;

                    double x = e.GetPosition(defineWorkspaceView).X;
                    double y = e.GetPosition(defineWorkspaceView).Y;

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

        private void AddWorkspace(object sender, RoutedEventArgs e)
        {
            KinectClient activeClient = defineWorkspaceView.ActiveClient;

            if (!workspaceList.Contains(activeWorkspace))
            {
                workspaceList.Add(activeWorkspace);
                dataStore.AddOrUpdateWorkspace(activeWorkspace.ID, activeWorkspace, activeClient);
            }
            kinectServer.AddWorkspace(activeWorkspace, activeClient);
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
            cloudView.SetWorkspace(activeWorkspace);
            EditWorkspace.DataContext = activeWorkspace;
        }
        private void RemoveWorkspace(object sender, RoutedEventArgs e)
        {
            workspaceList.Remove((Workspace)WorkspaceList.SelectedItem);
            dataStore.DeleteWorkspace((Workspace)WorkspaceList.SelectedItem);
            activeWorkspace = new Workspace();
            cloudView.ClearScreen();
        }

        private void SaveWorkspaceToFile(object sender, RoutedEventArgs e)
        {
            Workspace workspace = (Workspace)(((ListBoxItem) WorkspaceList.ContainerFromElement((Button) sender)).Content);
            FileHelper.WritePCD(new List<Point3D>(workspace.PointCloud), @"C:/asd/" + workspace.Name + "_pointcloud.pcd");
        }

        private void ClientSettingsHolder_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                //TabControl clients = new TabControl();
                //List<TabItem> clientTabs = new List<TabItem>();
                //foreach (KinectClient client in DataStore.Instance.GetClients())
                //{
                //    TabItem tabItem = new TabItem();
                //    ClientSettings settings = new ClientSettings();
                //    settings.ClientSettingsChanged += ClientSettingsChanged;

                //    tabItem.Header = client.Name;
                //    tabItem.Content = settings;
                //    clientTabs.Add(tabItem);
                //}
                //ClientSettings.DataContext = clientTabs;
            }
        }

        private void ClientSettingsChanged(KinectStreamerConfig config)
        {
            //throw new NotImplementedException();
        }
    }
}
