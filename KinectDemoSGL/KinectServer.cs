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
using Microsoft.Kinect;

namespace KinectDemoCommon
{
    public delegate void KinectServerDataArrived(KinectDemoMessage message);

    // Singleton
    class KinectServer
    {

        private readonly string ip = NetworkHelper.LocalIPAddress();
        private Socket socket, clientSocket;
        private byte[] buffer;

        private CoordinateMapper coordinateMapper;

        public KinectServerDataArrived DepthDataArrived;
        public KinectServerDataArrived ColorDataArrived;
        public KinectServerDataArrived BodyDataArrived;
        public KinectServerDataArrived PointCloudDataArrived;

        public KinectServerDataArrived WorkspaceUpdated;

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
                //  TODO: better solution for large buffer size?
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveBufferSize = 10000000
                };

                socket.Bind(new IPEndPoint(IPAddress.Parse(ip), 3333));
                socket.Listen(0);
                socket.BeginAccept(AcceptCallback, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }

        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                clientSocket = socket.EndAccept(ar);
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
                
                
                Debug.WriteLine("Received message of length " + received);
                Debug.WriteLine("Buffer size: " + buffer.Length);
                Array.Resize(ref buffer, received);
                BinaryFormatter formatter = new BinaryFormatter();
                
                
                MemoryStream stream = new MemoryStream(buffer);
                
                object obj = null;
                stream.Position = 0;
                Debug.WriteLine("Deserializing object...");
                try
                {
                    obj = formatter.Deserialize(stream);
                }
                catch (Exception ex )
                {
                    Debug.WriteLine("Message could not be deserialized!");
                    //MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                }
                if (obj != null)
                {
                    Debug.WriteLine("Object deserialized: " + obj.GetType());
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
                    if (obj is PointCloudStreamMessage)
                    {
                        DataStore.Instance.FullPointCloud = ((PointCloudStreamMessage)obj).FullPointCloud;

                        if (PointCloudDataArrived != null)
                        {
                            //PointCloudDataArrived((PointCloudStreamMessage)obj);
                        }
                    }
                }
                if (obj is WorkspaceMessage)
                {
                    WorkspaceMessage msg = (WorkspaceMessage)obj;
                    Workspace workspace = DataStore.Instance.WorkspaceDictionary[msg.ID];
                    workspace.Name = msg.Name;
                    workspace.Vertices = new ObservableCollection<Point>(msg.Vertices);
                    workspace.Vertices3D = msg.Vertices3D;
                    workspace.VertexDepths = msg.VertexDepths;
                    WorkspaceProcessor.SetWorkspaceCloudRealVerticesAndCenter(workspace);
                    WorkspaceUpdated((WorkspaceMessage)obj);
                }
                
                Array.Resize(ref buffer, clientSocket.ReceiveBufferSize);
                Debug.WriteLine("Listening for new messages with buffer size " + buffer.Length);
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void AddWorkspace(Workspace workspace)
        {
            WorkspaceMessage message = new WorkspaceMessage()
            {
                ID = workspace.ID,
                Name = workspace.Name,
                Vertices = workspace.Vertices.ToArray()
            };
            SerializeAndSendMessage(message);
        }

        private void SerializeAndSendMessage(KinectDemoMessage msg)
        {

            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, msg);
            buffer = stream.ToArray();

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
