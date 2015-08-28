using System;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using KinectDemoClient.Properties;
using KinectDemoCommon;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectClientMessages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;

namespace KinectDemoClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    ///     TODO: refactor to client + messageprocessor
    public partial class MainWindow : Window
    {
        private readonly KinectClient client = KinectClient.Instance;
        private readonly ClientMessageProcessor clientMessageProcessor = ClientMessageProcessor.Instance;
        private readonly KinectStreamer kinectStreamer = KinectStreamer.Instance;
        private bool calibrationDataSent;
        private bool pointCloudSent;

        public MainWindow()
        {
            InitializeComponent();

            client.ConnectedEvent += ConnectedEvent;
            client.DisconnectedEvent += DisconnectedEvent;

            clientMessageProcessor.ConfigurationMessageArrived += ConfigurationMessageArrived;
            clientMessageProcessor.CalibrationMessageArrived += CalibrationMessageArrived;

            ServerIpTextBox.SetBinding(TextBox.TextProperty, new Binding()
            {
                Path = new PropertyPath("IP"),
                Source = client
            });

            //ServerIpTextBox.Text = client.IP;

            DataContext = this;

            //Restore permanent settings
            AutoConnectCheckbox.IsChecked = Settings.Default.AutoConnect;


        }

        private void CalibrationMessageArrived(KinectDemoMessage message)
        {
            Dispatcher.Invoke(() =>
            {
                CalibrationMessage msg = (CalibrationMessage)message;
                if (msg.Message.Equals(CalibrationMessage.CalibrationMessageEnum.Start))
                {
                    CalibrationCheckbox.IsChecked = true;
                }
                else
                {
                    CalibrationCheckbox.IsChecked = false;
                }
            });
        }

        private void ConfigurationMessageArrived(KinectDemoMessage message)
        {
            Dispatcher.Invoke(() =>
            {
                ClientConfigurationMessage msg = (ClientConfigurationMessage)message;
                //  TODO: bind
                KinectStreamerConfig config = msg.Configuration;
                kinectStreamer.KinectStreamerConfig = config;
                DepthCheckbox.IsChecked = config.StreamDepthData;
                ColorCheckbox.IsChecked = config.StreamColorData;
                SkeletonCheckbox.IsChecked = config.StreamBodyData;
                UnifiedCheckbox.IsChecked = config.SendAsOne;
                PointCloudCheckbox.IsChecked = config.StreamPointCloudData;
                ColoredPointCloudCheckbox.IsChecked = config.StreamColoredPointCloudData;
                CalibrationCheckbox.IsChecked = config.ProvideCalibrationData;
            });
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
            client.SerializeAndSendMessage((BodyStreamMessage)message);
        }

        private void kinectStreamer_ColorDataReady(KinectClientMessage message)
        {
            client.SerializeAndSendMessage((ColorStreamMessage)message);
        }

        private void kinectStreamer_PointCloudDataReady(KinectClientMessage message)
        {
            client.SerializeAndSendMessage((PointCloudStreamMessage)message);
        }

        private void kinectStreamer_DepthDataReady(KinectClientMessage message)
        {
            SendFirstPointCloud();
            client.SerializeAndSendMessage((DepthStreamMessage)message);
        }


        private void kinectStreamer_ColoredPointCloudDataReady(KinectClientMessage message)
        {
            client.SerializeAndSendMessage((ColoredPointCloudStreamMessage)message);
        }

        private void kinectStreamer_UnifiedDataReady(KinectClientMessage message)
        {
            SendFirstPointCloud();
            client.SerializeAndSendMessage((UnifiedStreamerMessage)message);
        }


        private void kinectStreamer_CalibrationDataReady(KinectClientMessage message)
        {
            if (!calibrationDataSent)
            {
                calibrationDataSent = true;
                CalibrationCheckbox.IsChecked = false;
                client.SerializeAndSendMessage((CalibrationDataMessage)message);
            }
        }

        private void SendFirstPointCloud()
        {
            if (!pointCloudSent)
            {
                pointCloudSent = true;
                kinectStreamer.GenerateFullPointCloud();
                client.SerializeAndSendMessage(new PointCloudStreamMessage(kinectStreamer.FullPointCloud));
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

            client.SerializeAndSendMessage(new ClientConfigurationMessage()
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

            client.SerializeAndSendMessage(new ClientConfigurationMessage()
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

            client.SerializeAndSendMessage(new ClientConfigurationMessage()
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

            client.SerializeAndSendMessage(new ClientConfigurationMessage()
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

            client.SerializeAndSendMessage(new ClientConfigurationMessage()
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

            client.SerializeAndSendMessage(new ClientConfigurationMessage()
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

            client.SerializeAndSendMessage(new ClientConfigurationMessage()
            {
                Configuration = kinectStreamer.KinectStreamerConfig
            });
        }
        #endregion

        private void ConnectToServer(object sender, RoutedEventArgs e)
        {
            client.ConnectToServer();
        }


    }
}
