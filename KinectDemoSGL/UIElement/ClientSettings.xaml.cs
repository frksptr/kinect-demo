using System.Windows.Controls;
using KinectDemoCommon;

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
        public ClientSettings()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void SendAsOne_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.SendAsOne = true;
            ClientSettingsChanged(Client, KinectStreamerConfig);
        }

        private void DepthImage_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideDepthData = true;
            ClientSettingsChanged(Client, KinectStreamerConfig);
        }

        private void ColorImage_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideColorData = true;
            ClientSettingsChanged(Client, KinectStreamerConfig);
        }

        private void PointCloud_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvidePointCloudData = true;
            ClientSettingsChanged(Client, KinectStreamerConfig);
        }

        private void Skeleton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideBodyData = true;
            ClientSettingsChanged(Client, KinectStreamerConfig);
        }

        private void SendAsOne_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.SendAsOne = false;
            ClientSettingsChanged(Client, KinectStreamerConfig);
        }

        private void DepthImage_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideDepthData = false;
            ClientSettingsChanged(Client, KinectStreamerConfig);
        }

        private void ColorImage_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideColorData = false;
            ClientSettingsChanged(Client, KinectStreamerConfig);
        }

        private void PointCloud_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvidePointCloudData = false;
            ClientSettingsChanged(Client, KinectStreamerConfig);
        }

        private void Skeleton_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideBodyData = false;
            ClientSettingsChanged(Client, KinectStreamerConfig);
        }

        private void Calibration_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideCalibrationData = true;
            ClientSettingsChanged(Client, KinectStreamerConfig);
        }

        private void Calibration_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideCalibrationData = false;
            ClientSettingsChanged(Client, KinectStreamerConfig);
        }

    }
}
    