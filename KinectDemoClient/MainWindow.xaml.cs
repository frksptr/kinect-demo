using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using KinectDemoCommon;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectClientMessages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;
using KinectDemoCommon.Messages.KinectServerMessages;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;

namespace KinectDemoClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Socket clientSocket;
        public int[] DepthFrameSize { get; set; }
        public string Ip { get; set; }
        readonly KinectStreamer kinectStreamer;
        private byte[] buffer;
        private bool pointCloudSent = false;
        private bool serverReady = true;
        private bool calibrationDataSent = false;

        public MainWindow()
        {
            InitializeComponent();

            Ip = NetworkHelper.LocalIPAddress();
            ServerIpTextBox.Text = Ip;

            DataContext = this;

            kinectStreamer = KinectStreamer.Instance;

            //Restore permanent settings
            AutoConnectCheckbox.IsChecked = Properties.Settings.Default.AutoConnect;


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

        private void SerializeAndSendMessage(KinectDemoMessage msg)
        {
            if (!serverReady)
            {
                return;
            }
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, msg);
            byte[] buffer = stream.ToArray();


            if (clientSocket != null)
            {
                if (clientSocket.Connected)
                {

                    Debug.WriteLine("Sending message: " + msg.GetType() + " | " + buffer.Length);
                    //clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
                    clientSocket.Send(buffer, SocketFlags.None);
                }
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndConnect(ar);

                buffer = new byte[clientSocket.ReceiveBufferSize];
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);

                Dispatcher.Invoke(() => {
                    StatusTextBox.Text += "Connected to server.\n";
                });

                SerializeAndSendMessage(new ClientConfigurationMessage()
                {
                    Configuration = kinectStreamer.KinectStreamerConfig
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }


        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int received = clientSocket.EndReceive(ar);
                Array.Resize(ref buffer, received);
                BinaryFormatter formatter = new BinaryFormatter();

                MemoryStream stream = new MemoryStream(buffer);

                object obj = null;
                stream.Position = 0;
                try
                {
                    obj = formatter.Deserialize(stream);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "\n" + ex.StackTrace);

                }

                if (obj is KinectDemoMessage)
                {
                    if (obj is WorkspaceMessage)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            WorkspaceMessage msg = (WorkspaceMessage)obj;
                            TextBox.Text = ((WorkspaceMessage)obj).Vertices.ToString();
                            Workspace workspace = WorkspaceProcessor.ProcessWorkspace(
                                new Workspace() { Vertices = new ObservableCollection<Point>(msg.Vertices) });
                            WorkspaceMessage updatedMessage = new WorkspaceMessage()
                            {
                                ID = msg.ID,
                                Name = msg.Name,
                                VertexDepths = workspace.VertexDepths,

                                Vertices3D = workspace.Vertices3D.ToArray(),
                                Vertices = workspace.Vertices.ToArray(),
                            };
                            SerializeAndSendMessage(updatedMessage);
                        });
                    }
                    else if (obj is KinectServerReadyMessage)
                    {
                        serverReady = ((KinectServerReadyMessage)obj).Ready;
                    }
                    else if (obj is ClientConfigurationMessage)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ClientConfigurationMessage msg = (ClientConfigurationMessage) obj;
                            //  TODO: bind
                            KinectStreamerConfig config = msg.Configuration;
                            kinectStreamer.KinectStreamerConfig = config;
                            DepthCheckbox.IsChecked = config.StreamDepthData;
                            ColorCheckbox.IsChecked = config.StreamColorData;
                            SkeletonCheckbox.IsChecked = config.StreamBodyData;
                            UnifiedCheckbox.IsChecked = config.SendAsOne;
                            PointCloudCheckbox.IsChecked = config.StreamPointCloudData;
                            CalibrationCheckbox.IsChecked = config.ProvideCalibrationData;
                        });
                    }
                }

                Array.Resize(ref buffer, clientSocket.ReceiveBufferSize);

                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                if (!clientSocket.Connected)
                {
                    clientSocket = null;
                    StatusTextBox.Text += "\n Server disconnected.";
                }
                else
                {
                    MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
                Debug.WriteLine("Message sent.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void ConnectToServer(object sender, RoutedEventArgs e)
        {
            try
            {
                if (clientSocket == null)
                {
                    clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
                if (!clientSocket.Connected)
                {
                    clientSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ServerIpTextBox.Text), 3333), ConnectCallback,
                        null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void SendMessage(object sender, RoutedEventArgs e)
        {
            SerializeAndSendMessage(new TextMessage { Text = TextBox.Text });
            TextBox.Text = "";
        }

        private void DepthCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.DepthDataReady += kinectStreamer_DepthDataReady;
            kinectStreamer.KinectStreamerConfig.StreamDepthData = true;
        }

        private void ColorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.ColorDataReady += kinectStreamer_ColorDataReady;
            kinectStreamer.KinectStreamerConfig.StreamColorData = true;
        }

        private void ColorCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.ColorDataReady -= kinectStreamer_ColorDataReady;
            kinectStreamer.KinectStreamerConfig.StreamColorData = false;
        }

        private void DepthCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.DepthDataReady -= kinectStreamer_DepthDataReady;
            kinectStreamer.KinectStreamerConfig.StreamDepthData = false;
        }

        private void PointCloudCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.PointCloudDataReady += kinectStreamer_PointCloudDataReady;
            kinectStreamer.KinectStreamerConfig.StreamPointCloudData = true;
        }

        private void PointCloudCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.PointCloudDataReady -= kinectStreamer_PointCloudDataReady;
            kinectStreamer.KinectStreamerConfig.StreamPointCloudData = false;
        }

        private void SkeletonCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.BodyDataReady += kinectStreamer_BodyDataReady;
            kinectStreamer.KinectStreamerConfig.StreamBodyData = true;
        }

        private void SkeletonCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.BodyDataReady -= kinectStreamer_BodyDataReady;
            kinectStreamer.KinectStreamerConfig.StreamBodyData = false;
        }

        private void ColoredPointCloudCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.ColoredPointCloudDataReady += kinectStreamer_ColoredPointCloudDataReady;
            kinectStreamer.KinectStreamerConfig.StreamColoredPointCloudData = true;
        }


        private void ColoredPointCloudCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.ColoredPointCloudDataReady -= kinectStreamer_ColoredPointCloudDataReady;
            kinectStreamer.KinectStreamerConfig.StreamColoredPointCloudData = true;
        }

        private void UnifiedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.UnifiedDataReady += kinectStreamer_UnifiedDataReady;
            kinectStreamer.KinectStreamerConfig.SendAsOne = true;
        }

        private void UnifiedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.UnifiedDataReady -= kinectStreamer_UnifiedDataReady;
            kinectStreamer.KinectStreamerConfig.SendAsOne = false;
        }

        private void CalibrationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            calibrationDataSent = false;
            kinectStreamer.CalibrationDataReady += kinectStreamer_CalibrationDataReady;
            kinectStreamer.KinectStreamerConfig.ProvideCalibrationData = true;
        }

        private void CalibrationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            calibrationDataSent = true;
            kinectStreamer.CalibrationDataReady -= kinectStreamer_CalibrationDataReady;
            kinectStreamer.KinectStreamerConfig.ProvideCalibrationData = false;
        }

        private void AutoConnectCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoConnect = true;
            Properties.Settings.Default.Save();
        }

        private void AutoConnectCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoConnect = false;
            Properties.Settings.Default.Save();
        }

    }
}
