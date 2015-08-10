using System.Windows.Controls;
using KinectDemoCommon;

namespace KinectDemoSGL.UIElement
{
    public delegate void ClientSettingsChanged(KinectStreamerConfig config);
    //  TODO:   move to common, use in client
    /// <summary>
    /// Interaction logic for ClientSettings.xaml
    /// </summary>
    public partial class ClientSettings : UserControl
    {
        public KinectStreamerConfig KinectStreamerConfig { get; set; }
        public ClientSettingsChanged ClientSettingsChanged;
        public ClientSettings(KinectStreamerConfig config)
        {
            InitializeComponent();
            DataContext = this;
            KinectStreamerConfig = config;
        }

        private void SendAsOne_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.SendAsOne = true;
            ClientSettingsChanged(KinectStreamerConfig);
        }

        private void DepthImage_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideDepthData = true;
            ClientSettingsChanged(KinectStreamerConfig);
        }

        private void ColorImage_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideColorData = true;
            ClientSettingsChanged(KinectStreamerConfig);
        }

        private void PointCloud_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvidePointCloudData = true;
            ClientSettingsChanged(KinectStreamerConfig);
        }

        private void Skeleton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideBodyData = true;
            ClientSettingsChanged(KinectStreamerConfig);
        }

        private void SendAsOne_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.SendAsOne = false;
            ClientSettingsChanged(KinectStreamerConfig);
        }

        private void DepthImage_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideDepthData = false;
            ClientSettingsChanged(KinectStreamerConfig);
        }

        private void ColorImage_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideColorData = false;
            ClientSettingsChanged(KinectStreamerConfig);
        }

        private void PointCloud_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvidePointCloudData = false;
            ClientSettingsChanged(KinectStreamerConfig);
        }

        private void Skeleton_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            KinectStreamerConfig.ProvideBodyData = false;
            ClientSettingsChanged(KinectStreamerConfig);
        }

    }
}
    