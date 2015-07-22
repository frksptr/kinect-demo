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
using System.Collections.Generic;
using System.Text;
using KinectDemoCommon.Messages.KinectServerMessages;

namespace KinectDemoCommon
{
    public delegate void KinectServerDataArrived(KinectDemoMessage message);

    // Singleton
    class KinectServer
    {

        private readonly string ip = NetworkHelper.LocalIPAddress();
        private Socket socket;
        private byte[] buffer;

        private CoordinateMapper coordinateMapper;

        public KinectServerDataArrived DepthDataArrived;
        public KinectServerDataArrived ColorDataArrived;
        public KinectServerDataArrived BodyDataArrived;
        public KinectServerDataArrived PointCloudDataArrived;

        public KinectServerDataArrived WorkspaceUpdated;

        private static KinectServer kinectServer;

        private byte[] endOfObjectMark = Encoding.ASCII.GetBytes("<EOO>");
        
        public static KinectServer Instance
        {
            get { return kinectServer ?? (kinectServer = new KinectServer()); }
        }

        private KinectServer()
        {
            StartServer();
        }

        public class StateObject
        {
            public Socket WorkSocket = null;
            public const int BufferSize = 9000000;
            public byte[] Buffer = new byte[BufferSize];

            public List<byte> PrevBytes = new List<byte>();
        }

        private void StartServer()
        {
            try
            {
                //  TODO: better solution for large buffer size?
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
                StateObject state = new StateObject();
                state.WorkSocket = socket.EndAccept(ar);

                state.WorkSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private bool ArrayEquals(byte[] array, byte[] pattern)
        {
            bool equals = false;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != pattern[i])
                {
                    return false;
                }
                equals = true;
            }
            return equals;
        }
        

        private void ProcessBuffer(StateObject state)
        {
            byte[] prevBytesArray = state.PrevBytes.ToArray();
            int i = 0;
            while (i < state.PrevBytes.Count)
            {
                if (i + endOfObjectMark.Length > state.PrevBytes.Count)
                {
                    return;
                }
                if (ArrayEquals(state.PrevBytes.GetRange(i, endOfObjectMark.Length).ToArray(), endOfObjectMark))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    MemoryStream stream = new MemoryStream(state.PrevBytes.GetRange(0, i + 1).ToArray());

                    object obj = formatter.Deserialize(stream);

                    ObjectArrived(obj);

                    state.PrevBytes.RemoveRange(0, i + endOfObjectMark.Length);
                    i = 0;
                }
                i++;
            }

        }

        private void ObjectArrived(object obj)
        {
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
        }

                        


        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.WorkSocket;
                int received = handler.EndReceive(ar);

                if (received > 0)
                {
                    Array.Resize(ref state.Buffer, received);
                    state.PrevBytes.AddRange(state.Buffer);

                    StringBuilder sb = new StringBuilder();
                    sb.Append(Encoding.ASCII.GetString(
                    state.Buffer, 0, received));

                    // Check for end-of-file tag. If it is not there, read 
                    // more data.
                    String content = sb.ToString();


                    ProcessBuffer(state);

                    Array.Resize(ref state.Buffer, 9000000);
                }

                handler.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);

                SerializeAndSendMessage(new KinectServerReadyMessage{Ready = true}, handler);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void AddWorkspace(Workspace workspace, Socket clientSocket)
        {
            WorkspaceMessage message = new WorkspaceMessage()
            {
                ID = workspace.ID,
                Name = workspace.Name,
                Vertices = workspace.Vertices.ToArray()
            };
            SerializeAndSendMessage(message, clientSocket);
        }

        private void SerializeAndSendMessage(KinectDemoMessage msg, Socket clientSocket)
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
