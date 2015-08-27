using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using KinectDemoClient.Properties;
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
    /// 
    ///     TODO: refactor to client + messageprocessor
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
            AutoConnectCheckbox.IsChecked = Settings.Default.AutoConnect;


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
            if (!serverReady || clientSocket == null)
            {
                return;
            }
            if (!clientSocket.Connected) return;

            serverReady = false;
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

                Dispatcher.Invoke(() =>
                {
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
                            ClientConfigurationMessage msg = (ClientConfigurationMessage)obj;
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
                    else if (obj is CalibrationMessage)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            CalibrationMessage msg = (CalibrationMessage) obj;
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
                }

                Array.Resize(ref buffer, clientSocket.ReceiveBufferSize);

                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                if (!clientSocket.Connected)
                {
                    clientSocket = null;
                    Dispatcher.Invoke(() =>
                    {
                        StatusTextBox.Text += "\n Server disconnected.";
                    });
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
    }
}
