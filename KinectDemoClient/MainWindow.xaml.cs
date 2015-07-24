using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectClientMessages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using System.Text;
using KinectDemoCommon.Messages.KinectServerMessages;
using System.Collections.Generic;

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
        private bool canSend = true;
        private byte[] endOfObjectMark = Encoding.ASCII.GetBytes("<EOO>");
        private bool serverReady = true;

        public MainWindow()
        {
            InitializeComponent();

            Ip = NetworkHelper.LocalIPAddress();

            DataContext = this;
            
            kinectStreamer = KinectStreamer.Instance;

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
            if (message is PointCloudStreamMessage) SerializeAndSendMessage((PointCloudStreamMessage)message);
            if (message is PointCloudStreamMessage) SerializeAndSendMessage((PointCloudStreamMessage)message);
        }

        private void kinectStreamer_DepthDataReady(KinectClientMessage message)
        {
            if (!pointCloudSent)
            {
                pointCloudSent = true;
                kinectStreamer.GenerateFullPointCloud();
                SerializeAndSendMessage(new PointCloudStreamMessage(kinectStreamer.FullPointCloud));
            }
            SerializeAndSendMessage((DepthStreamMessage)message);
        }
        private void kinectStreamer_WorkspaceActivated(WorkspaceMessage message)
        {
            SerializeAndSendMessage(message);
        }

        private void SerializeAndSendMessage(KinectDemoMessage msg)
        {
            if (!serverReady)
            {
                return;
            }
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
                    //clientSocket.Send(buffer, buffer.Length, SocketFlags.None);
                    //canSend = false;
                    byte[] bufferWithEOOM = new byte[buffer.Length + endOfObjectMark.Length];
                    buffer.CopyTo(bufferWithEOOM, 0);
                    endOfObjectMark.CopyTo(bufferWithEOOM, buffer.Length);

                    clientSocket.Send(bufferWithEOOM, SocketFlags.None);
                    //clientSocket.BeginSend(bufferWithEOOM, 0, bufferWithEOOM.Length, SocketFlags.None, SendCallback, null);
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
                    if (obj is KinectServerReadyMessage)
                    {
                        serverReady = ((KinectServerReadyMessage)obj).Ready;
                    }
                }

                Array.Resize(ref buffer, clientSocket.ReceiveBufferSize);

                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
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
                    clientSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(Ip), 3333), ConnectCallback,
                        null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void DepthCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.DepthDataReady += kinectStreamer_DepthDataReady;
            kinectStreamer.KinectStreamerConfig.ProvideDepthData = true;
        }

        private void ColorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.ColorDataReady += kinectStreamer_ColorDataReady;
            kinectStreamer.KinectStreamerConfig.ProvideColorData = true;
        }

        private void ColorCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.ColorDataReady -= kinectStreamer_ColorDataReady;
            kinectStreamer.KinectStreamerConfig.ProvideColorData = false;
        }

        private void DepthCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.DepthDataReady -= kinectStreamer_DepthDataReady;
            kinectStreamer.KinectStreamerConfig.ProvideDepthData = false;
        }

        private void PointCloudCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.PointCloudDataReady += kinectStreamer_PointCloudDataReady;
            kinectStreamer.KinectStreamerConfig.ProvidePointCloudData = true;
        }

        private void PointCloudCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.PointCloudDataReady -= kinectStreamer_PointCloudDataReady;
            kinectStreamer.KinectStreamerConfig.ProvidePointCloudData = false;
        }

        private void SendMessage(object sender, RoutedEventArgs e)
        {
            SerializeAndSendMessage(new TextMessage { Text = TextBox.Text });
            TextBox.Text = "";
        }

        private void SkeletonCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.BodyDataReady += kinectStreamer_BodyDataReady;
            kinectStreamer.KinectStreamerConfig.ProvideBodyData = true;
        }

        private void SkeletonCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectStreamer.BodyDataReady -= kinectStreamer_BodyDataReady;
            kinectStreamer.KinectStreamerConfig.ProvideBodyData = false;
        }
    }
}
