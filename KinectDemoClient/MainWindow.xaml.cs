using System;
using System.Collections.ObjectModel;
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

namespace KinectDemoClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Socket clientSocket;
        public int[] DepthFrameSize { get; set; }
        string ip = NetworkHelper.LocalIPAddress();
        readonly KinectStreamer kinectStreamer;
        private byte[] buffer;
        private bool pointCloudSent = false;

        public MainWindow()
        {
            InitializeComponent();

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

            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, msg);
            byte[] buffer = stream.ToArray();

            if (clientSocket != null)
            {
                if (clientSocket.Connected)
                {
                    clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
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
                
                kinectStreamer.DepthDataReady += kinectStreamer_DepthDataReady;
                kinectStreamer.KinectStreamerConfig.ProvideDepthData = true;

                //kinectStreamer.WorkspaceChecker.WorkspaceActivated += kinectStreamer_WorkspaceActivated;

                //kinectStreamer.ColorDataReady += kinectStreamer_ColorDataReady;
                //kinectStreamer.KinectStreamerConfig.ProvideColorData = true;
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
                                Vertices3D = workspace.Vertices3D.ToArray(),
                                Vertices = workspace.Vertices.ToArray(),
                            };
                            SerializeAndSendMessage(updatedMessage);
                        });
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                throw;
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
                    clientSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), 3333), ConnectCallback,
                        null);
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }

        }

        private void SendDepthDataButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
