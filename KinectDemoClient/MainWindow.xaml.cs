using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Media.Imaging;
using KinectDemoCommon.KinectStreamerMessages;

namespace KinectDemoClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Socket clientSocket;
        public int[] DepthFrameSize { get; set; }
        WriteableBitmap depthBitmap;
        string ip = "192.168.32.1";

        private byte[] depthPixels;

        private DepthStreamMessage msg;

        public MainWindow()
        {
            InitializeComponent();

            KinectStreamer kinectStreamer = KinectStreamer.Instance;

            kinectStreamer.DepthDataReady += kinectStreamer_DepthDataReady;
            kinectStreamer.KinectStreamerConfig.ProvideDepthData = true;

            DepthFrameSize = new[] { 
                kinectStreamer.DepthFrameDescription.Width,
                kinectStreamer.DepthFrameDescription.Height
            };
        }

        private void kinectStreamer_DepthDataReady(KinectStreamerMessage message)
        {
            msg = ((DepthStreamMessage) message);
            if (clientSocket != null)
            {
                if (clientSocket.Connected)
                {
                    SendDepthData();
                }
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndConnect(ar);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                MessageBox.Show(ex.Message);
                throw;
            }
        }


        private void ConnectToServer(object sender, RoutedEventArgs e)
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), 3333), new AsyncCallback(ConnectCallback),
                    null);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }

        }

        private void SendDepthDataButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SendDepthData();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void SendDepthData()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, msg);
            byte[] buffer = stream.ToArray();

            clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);
        }
    }
}
