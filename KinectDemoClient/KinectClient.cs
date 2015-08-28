using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectServerMessages;
using KinectDemoCommon.Util;

namespace KinectDemoClient
{
    public delegate void ClientEvent(object obj);

    // Singleton
    class KinectClient
    {
        public ClientEvent ConnectedEvent;
        public ClientEvent DisconnectedEvent;

        private Socket clientSocket;
        public string IP { get; set; }
        private byte[] buffer;
        private bool pointCloudSent = false;
        private bool serverReady = true;
        private bool calibrationDataSent = false;
        private bool autoReconnect = true;

        private readonly ClientMessageProcessor clientMessageProcessor = ClientMessageProcessor.Instance;
        private readonly KinectStreamer kinectStreamer = KinectStreamer.Instance;

        private static KinectClient kinectClient;

        public static KinectClient Instance
        {
            get { return kinectClient ?? (kinectClient = new KinectClient()); }
        }

        private KinectClient()
        {
            IP = NetworkHelper.LocalIPAddress();
            clientMessageProcessor.WorkspaceMessageArrived += WorkspaceMessageArrived;
            clientMessageProcessor.ServerReadyMessageArrived += ServerReadyMessageArrived;
        }

        private void ServerReadyMessageArrived(KinectDemoMessage message)
        {
            serverReady = ((KinectServerReadyMessage)message).Ready;
        }

        private void WorkspaceMessageArrived(KinectDemoMessage message)
        {
            SerializeAndSendMessage(message);
        }

        public void SerializeAndSendMessage(KinectDemoMessage msg)
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

                ConnectedEvent(null);

                SerializeAndSendMessage(new ClientConfigurationMessage()
                {
                    Configuration = kinectStreamer.KinectStreamerConfig
                });
            }
            catch (Exception ex)
            {
                if (autoReconnect)
                {
                    ConnectToServer();
                }
                else
                {
                    MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                }
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
                    ClientMessageProcessor.Instance.ProcessStreamMessage(obj);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "\n" + ex.StackTrace);

                }

                Array.Resize(ref buffer, clientSocket.ReceiveBufferSize);

                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                if (!clientSocket.Connected)
                {
                    clientSocket = null;
                    DisconnectedEvent(null);
                    if (autoReconnect)
                    {
                        ConnectToServer();
                    }
                }
                else
                {
                    MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        public void ConnectToServer()
        {
            try
            {
                if (clientSocket == null)
                {
                    clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
                if (!clientSocket.Connected)
                {
                    clientSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(IP), 3333), ConnectCallback,
                        null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
