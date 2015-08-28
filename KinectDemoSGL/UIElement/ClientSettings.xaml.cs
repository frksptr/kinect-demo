using System.Windows;
using System.Windows.Controls;
using KinectDemoCommon;
using KinectDemoCommon.Messages;

namespace KinectDemoSGL.UIElement
{
    public delegate void ClientSettingsChanged(KinectClient client, KinectStreamerConfig config);
    //  TODO:   move to common, also use in client
    /// <summary>
    /// Interaction logic for ClientS.ettings.xaml
    /// </summary>
    public partial class ClientSettings : UserControl
    {
        public KinectStreamerConfig KinectStreamerConfig { get; set; }
        public KinectClient Client { get; set; }

        public ClientSettingsChanged ClientSettingsChanged;

        private bool initialized = false;
        public ClientSettings(KinectClient client, KinectStreamerConfig config)
        {
            KinectStreamerConfig = config;
            Client = client;
            InitializeComponent();
            DataContext = this;
            DepthCheckbox.IsChecked = config.StreamDepthData;
            ColorCheckbox.IsChecked = config.StreamColorData;
            PointCloudCheckbox.IsChecked = config.StreamPointCloudData;
            SkeletonCheckbox.IsChecked = config.StreamBodyData;
            CalibrationCheckbox.IsChecked = config.ProvideCalibrationData;
            SendAsOneCheckbox.IsChecked = config.SendAsOne;

            foreach (FrameworkElement item in CheckboxContainer.Children)
            {
                if (item is CheckBox)
                {
                    item.IsEnabled = client.Connected;
                }
            }

            initialized = true;
        }

        private void ConfigurationDataArrived(KinectDemoMessage message, KinectClient kinectClient)
        {
            Dispatcher.Invoke(() =>
            {
                ClientConfigurationMessage msg = (ClientConfigurationMessage)message;
                //  TODO: bind
                KinectStreamerConfig config = msg.Configuration;
                DepthCheckbox.IsChecked = config.StreamDepthData;
                ColorCheckbox.IsChecked = config.StreamColorData;
                SkeletonCheckbox.IsChecked = config.StreamBodyData;
                SendAsOneCheckbox.IsChecked = config.SendAsOne;
                PointCloudCheckbox.IsChecked = config.StreamPointCloudData;
                ColoredPointCloudCheckbox.IsChecked = config.StreamColoredPointCloudData;
                CalibrationCheckbox.IsChecked = config.ProvideCalibrationData;
            });
        }

        private void SendAsOne_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.SendAsOne = true;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void DepthImage_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.StreamDepthData = true;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void ColorImage_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.StreamColorData = true;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void PointCloud_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.StreamPointCloudData = true;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void Skeleton_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.StreamBodyData = true;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void SendAsOne_Unchecked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.SendAsOne = false;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void DepthImage_Unchecked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.StreamDepthData = false;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void ColorImage_Unchecked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.StreamColorData = false;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void PointCloud_Unchecked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.StreamPointCloudData = false;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void Skeleton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.StreamBodyData = false;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void Calibration_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.ProvideCalibrationData = true;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void Calibration_Unchecked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.ProvideCalibrationData = false;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void ColoredPointCloudCheckbox_OnCheckedPointCloud_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.StreamColoredPointCloudData = true;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void ColoredPointCloud_Unchecked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                KinectStreamerConfig.StreamColoredPointCloudData = false;
                ClientSettingsChanged(Client, KinectStreamerConfig);
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool) e.NewValue)
            {
                ServerMessageProcessor.Instance.ConfigurationMessageArrived += ConfigurationDataArrived;
            }
            else
            {
                ServerMessageProcessor.Instance.ConfigurationMessageArrived -= ConfigurationDataArrived;
            }
        }
    }
}
