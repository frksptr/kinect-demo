using System;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using KinectDemoClient.Properties;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectClientMessages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;
using KinectDemoCommon.Util;

namespace KinectDemoClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    ///     TODO: refactor to client + messageprocessor
    public partial class MainWindow : Window
    {
        private KinectClient client = KinectClient.Instance;
        public MainWindow()
        {
            InitializeComponent();

            client.ConnectedEvent += ConnectedEvent;
            client.DisconnectedEvent += DisconnectedEvent;

            ServerIpTextBox.SetBinding(TextBox.TextProperty, new Binding()
            {
                Path = new PropertyPath("IP"),
                Source = client
            });
            ServerIpTextBox.Text = client.IP;

            DataContext = this;

            kinectStreamer = KinectStreamer.Instance;

            //Restore permanent settings
            AutoConnectCheckbox.IsChecked = Settings.Default.AutoConnect;


        }

        private void DisconnectedEvent(object o)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBox.Text += "\n Server disconnected.";
            });
        }

        private void ConnectedEvent(object o)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBox.Text += "Connected to server.\n";
            });
        }

        void kinectStreamer_BodyDataReady(KinectClientMessage message)
        {
            SerializeAndSendMessage((BodyStreamMessage)message);
        }

        private void kinectStreamer_ColorDataReady(KinectClientMessage message)
        {
            SerializeAndSendMessage((ColorStreamMessage)message);
        }

        private void kinectStreamer_PointCloudDataReady(KinectClientMessage message)
        {
            SerializeAndSendMessage((PointCloudStreamMessage)message);
        }

        private void kinectStreamer_DepthDataReady(KinectClientMessage message)
        {
            SendFirstPointCloud();
            SerializeAndSendMessage((DepthStreamMessage)message);
        }


        private void kinectStreamer_ColoredPointCloudDataReady(KinectClientMessage message)
        {
            SerializeAndSendMessage((ColoredPointCloudStreamMessage)message);
        }

        private void kinectStreamer_UnifiedDataReady(KinectClientMessage message)
        {
            SendFirstPointCloud();
            SerializeAndSendMessage((UnifiedStreamerMessage)message);
        }


        private void kinectStreamer_CalibrationDataReady(KinectClientMessage message)
        {
            if (!calibrationDataSent)
            {
                calibrationDataSent = true;
                CalibrationCheckbox.IsChecked = false;
                SerializeAndSendMessage((CalibrationDataMessage)message);
            }
        }

        private void SendFirstPointCloud()
        {
            if (!pointCloudSent)
            {
                pointCloudSent = true;
                kinectStreamer.GenerateFullPointCloud();
                SerializeAndSendMessage(new PointCloudStreamMessage(kinectStreamer.FullPointCloud));
            }
        }


        # region checkboxes
        private void AutoConnectCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.AutoConnect = true;
            Settings.Default.Save();
        }

        private void AutoConnectCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.AutoConnect = false;
            Settings.Default.Save();
        }

        private void DepthCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked.Value)
            {
                kinectStreamer.DepthDataReady += kinectStreamer_DepthDataReady;
                kinectStreamer.KinectStreamerConfig.StreamDepthData = true;
            }
            else
            {
                kinectStreamer.DepthDataReady -= kinectStreamer_DepthDataReady;
                kinectStreamer.KinectStreamerConfig.StreamDepthData = false;
            }

            SerializeAndSendMessage(new ClientConfigurationMessage()
            {
                Configuration = kinectStreamer.KinectStreamerConfig
            });
        }

        private void ColorCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked.Value)
            {
                kinectStreamer.ColorDataReady += kinectStreamer_ColorDataReady;
                kinectStreamer.KinectStreamerConfig.StreamColorData = true;
            }
            else
            {
                kinectStreamer.ColorDataReady -= kinectStreamer_ColorDataReady;
                kinectStreamer.KinectStreamerConfig.StreamColorData = false;
            }

            SerializeAndSendMessage(new ClientConfigurationMessage()
            {
                Configuration = kinectStreamer.KinectStreamerConfig
            });
        }

        private void PointCloudCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked.Value)
            {
                kinectStreamer.PointCloudDataReady += kinectStreamer_PointCloudDataReady;
                kinectStreamer.KinectStreamerConfig.StreamPointCloudData = true;
            }
            else
            {
                kinectStreamer.PointCloudDataReady -= kinectStreamer_PointCloudDataReady;
                kinectStreamer.KinectStreamerConfig.StreamPointCloudData = false;
            }

            SerializeAndSendMessage(new ClientConfigurationMessage()
            {
                Configuration = kinectStreamer.KinectStreamerConfig
            });
        }

        private void ColoredPointCloudCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked.Value)
            {
                kinectStreamer.ColoredPointCloudDataReady += kinectStreamer_ColoredPointCloudDataReady;
                kinectStreamer.KinectStreamerConfig.StreamColoredPointCloudData = true;
            }
            else
            {
                kinectStreamer.ColoredPointCloudDataReady -= kinectStreamer_ColoredPointCloudDataReady;
                kinectStreamer.KinectStreamerConfig.StreamColoredPointCloudData = false;
            }

            SerializeAndSendMessage(new ClientConfigurationMessage()
            {
                Configuration = kinectStreamer.KinectStreamerConfig
            });
        }

        private void SkeletonCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked.Value)
            {
                kinectStreamer.BodyDataReady += kinectStreamer_BodyDataReady;
                kinectStreamer.KinectStreamerConfig.StreamBodyData = true;
            }
            else
            {
                kinectStreamer.BodyDataReady -= kinectStreamer_BodyDataReady;
                kinectStreamer.KinectStreamerConfig.StreamBodyData = false;
            }

            SerializeAndSendMessage(new ClientConfigurationMessage()
            {
                Configuration = kinectStreamer.KinectStreamerConfig
            });
        }

        private void UnifiedCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked.Value)
            {
                kinectStreamer.UnifiedDataReady += kinectStreamer_UnifiedDataReady;
                kinectStreamer.KinectStreamerConfig.SendAsOne = true;
            }
            else
            {
                kinectStreamer.UnifiedDataReady -= kinectStreamer_UnifiedDataReady;
                kinectStreamer.KinectStreamerConfig.SendAsOne = false;
            }

            SerializeAndSendMessage(new ClientConfigurationMessage()
            {
                Configuration = kinectStreamer.KinectStreamerConfig
            });
        }

        private void CalibrationCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked.Value)
            {
                calibrationDataSent = false;
                kinectStreamer.CalibrationDataReady += kinectStreamer_CalibrationDataReady;
                kinectStreamer.KinectStreamerConfig.ProvideCalibrationData = true;
            }
            else
            {
                calibrationDataSent = true;
                kinectStreamer.ColorDataReady -= kinectStreamer_CalibrationDataReady;
                kinectStreamer.KinectStreamerConfig.ProvideCalibrationData = false;
            }

            SerializeAndSendMessage(new ClientConfigurationMessage()
            {
                Configuration = kinectStreamer.KinectStreamerConfig
            });
        }
        #endregion


    }
}
