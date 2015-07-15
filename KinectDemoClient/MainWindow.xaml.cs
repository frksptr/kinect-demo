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
        string ip = "192.168.32.1";
        KinectStreamer kinectStreamer;

        public MainWindow()
        {
            InitializeComponent();

            kinectStreamer = KinectStreamer.Instance;
        }

        void kinectStreamer_BodyDataReady(KinectStreamerMessage message)
        {
            SerializeAndSendMessage((BodyStreamMessage)message);
        }

        private void kinectStreamer_ColorDataReady(KinectStreamerMessage message)
        {
            SerializeAndSendMessage((ColorStreamMessage)message);
        }

        private void kinectStreamer_DepthDataReady(KinectStreamerMessage message)
        {
            SerializeAndSendMessage((DepthStreamMessage)message);
        }

        private void SerializeAndSendMessage(KinectStreamerMessage msg)
        {

            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, msg);
            byte[] buffer = stream.ToArray();

            if (clientSocket != null)
            {
                if (clientSocket.Connected)
                {
                    clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);
                }
            }
        }



        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndConnect(ar);
                
                kinectStreamer.DepthDataReady += kinectStreamer_DepthDataReady;
                kinectStreamer.KinectStreamerConfig.ProvideDepthData = true;

                //kinectStreamer.ColorDataReady += kinectStreamer_ColorDataReady;
                //kinectStreamer.KinectStreamerConfig.ProvideColorData = true;
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
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
    }
}
