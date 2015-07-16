using System;
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

namespace KinectDemoCommon
{
    public delegate void KinectServerDataArrived(KinectClientMessage message);

    // Singleton
    class KinectServer
    {

        private string ip = NetworkHelper.LocalIPAddress();
        private Socket socket, clientSocket;
        private byte[] buffer;
        

        public KinectServerDataArrived DepthDataArrived;
        public KinectServerDataArrived ColorDataArrived;
        public KinectServerDataArrived BodyDataArrived;

        private static KinectServer kinectServer;

        public static KinectServer Instance
        {
            get { return kinectServer ?? (kinectServer = new KinectServer()); }
        }

        private KinectServer()
        {
            StartServer();
        }
        private void StartServer()
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //  TODO: better solution for large buffer size?
                socket.ReceiveBufferSize = 9000000; 
                socket.Bind(new IPEndPoint(IPAddress.Parse(ip), 3333));
                socket.Listen(0);
                socket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                clientSocket = socket.EndAccept(ar);
                buffer = new byte[clientSocket.ReceiveBufferSize];
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                catch (Exception ex )
                {
                    MessageBox.Show(ex.Message);

                }
                
                if (obj is KinectClientMessage)
                {
                    if (obj is DepthStreamMessage)
                    {
                        if (DepthDataArrived != null)
                        {
                            DepthDataArrived((DepthStreamMessage)obj);
                        }
                    }
                    if (obj is ColorStreamMessage)
                    {
                        if (ColorDataArrived != null)
                        {
                            ColorDataArrived((ColorStreamMessage)obj);
                        }
                    }
                }
                if (obj is WorkspaceMessage)
                {
                    WorkspaceMessage msg = (WorkspaceMessage) obj;

                }
                
                Array.Resize(ref buffer, clientSocket.ReceiveBufferSize);

                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void AddWorkspace(Workspace workspace)
        {
            WorkspaceMessage message = new WorkspaceMessage()
            {
                Vertices = workspace.Vertices.ToArray()
            };
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

        private void SendCallback(IAsyncResult ar)
        {
            
        }
    }
}
