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
namespace KinectDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private WorkspaceControl workspaceControl;

        private WorkspaceView activeWorkspace;

        // Displays color image on which to select workspaces
        private CameraWorkspace cameraWorkspace;

        // Displays point clouds
        private CloudView cloudView;

        private List<Workspace> workspaceList = new List<Workspace>();

        private const int MARGIN = 5;

        private string statusText = null;

        public MainWindow()
        {
            InitializeComponent();

            addWorkspaceControl();

            addCameraWorkspace();

            cloudView = new CloudView(KinectSensor.GetDefault());

            pointCloudHolder.Children.Add(cloudView);
        }

        private void addCameraWorkspace()
        {
            cameraWorkspace = new CameraWorkspace(KinectSensor.GetDefault());
            cameraWorkspace.MouseLeftButtonDown += cameraWorkspace_MouseLeftButtonDown;

            cameraHolder.Children.Add(cameraWorkspace);
        }

        private void addWorkspaceControl()
        {
            workspaceControl = new WorkspaceControl();
            workspaceControl.AddButton.Click += addButton_Click;
            workspaceControl.DeleteButton.Click += deleteButton_Click;
            workspaceControl.setSource(new Workspace());
            workspaceControl.Margin = new Thickness(MARGIN);

            listHolder.Children.Add(workspaceControl);
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            listHolder.Children.Remove(activeWorkspace);
            workspaceControl.Mode = WorkspaceControl.WorkspaceControlMode.Add;
            workspaceControl.setSource(new Workspace());
        }


        void addButton_Click(object sender, RoutedEventArgs e)
        {
            // New Workspace added
            if (workspaceControl.Mode == WorkspaceControl.WorkspaceControlMode.Add)
            {
                workspaceList.Add(workspaceControl.Workspace);

                WorkspaceView workspaceView = new WorkspaceView(workspaceControl.Workspace);
                workspaceView.wsName.MouseDown += selectWorkspace;
                workspaceView.Margin = new Thickness(MARGIN);
                workspaceView.DeleteButton.Click += deleteWorkspace;
                this.listHolder.Children.Add(workspaceView);
            }
            // Existing Workspace edited
            else
            {
                workspaceControl.Mode = WorkspaceControl.WorkspaceControlMode.Add;
                cloudView.updatePointCloudAndCenter();
            }
            workspaceControl.setSource(new Workspace());
        }

        private void deleteWorkspace(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent((Button) sender);
            while (!(parent is WorkspaceView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            WorkspaceView workspaceView = ((WorkspaceView)parent);
            ((Panel)(workspaceView.Parent)).Children.Remove(workspaceView);

            workspaceControl.Mode = WorkspaceControl.WorkspaceControlMode.Add;
            workspaceControl.setSource(new Workspace());
        }

        void selectWorkspace(object sender, MouseButtonEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent((TextBlock)sender);
            while (!(parent is WorkspaceView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            activeWorkspace = (WorkspaceView)parent;
            workspaceControl.setSource(activeWorkspace.Workspace);

            workspaceControl.Mode = WorkspaceControl.WorkspaceControlMode.Edit;

            this.cloudView.setWorkspace(activeWorkspace.Workspace);
        }

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            cloudView.setRealVertices();
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
    }
}
