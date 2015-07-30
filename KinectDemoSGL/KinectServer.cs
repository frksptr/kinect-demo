using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using KinectDemoCommon;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectServerMessages;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using System.Collections.Generic;
using KinectDemoCommon.Messages.KinectServerMessages;
using KinectDemoSGL;
using KinectDemoCommon;

namespace KinectDemoSGL
{
    public delegate void KinectServerDataArrived(KinectDemoMessage message, KinectClient kinectClient);

    // Singleton
    class KinectServer
    {

        //private readonly string ip = NetworkHelper.LocalIPAddress();
        private readonly string ip = "192.168.0.107";

        private Socket socket;
        //private byte[] buffer;

        private static KinectServer kinectServer;

        private FrameSize depthFrameSize;

        //  TODO: create two way dictionary
        private Dictionary<KinectClient, StateObject> clientStateObjectDictionary = new Dictionary<KinectClient, StateObject>();
        private Dictionary<StateObject, KinectClient> stateObjectClientDictionary = new Dictionary<StateObject, KinectClient>();
        public MessageProcessor MessageProcessor { get; set; }

        public static KinectServer Instance
        {
            get { return kinectServer ?? (kinectServer = new KinectServer()); }
        }

        private KinectServer()
        {
            MessageProcessor = new MessageProcessor();
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
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Parse(ip), 3333));
                socket.Listen(10);
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
                KinectClient client = new KinectClient();
                stateObjectClientDictionary.Add(state, client);
                clientStateObjectDictionary.Add(client, state);
                DataStore.Instance.AddClient(stateObjectClientDictionary[state]);

                state.WorkSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, ReceiveCallback, state);

                socket.BeginAccept(AcceptCallback, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }
        private void ObjectArrived(object obj, KinectClient sender)
        {
            if (obj != null)
            {
                Debug.WriteLine("Object from " + sender.Name + " deserialized: " + obj.GetType());
            }
            MessageProcessor.ProcessStreamMessage(obj, sender);
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

                    //state.PrevBytes.AddRange(state.Buffer);
                    //ProcessBuffer(state);

                    BinaryFormatter formatter = new BinaryFormatter();
                    MemoryStream stream = new MemoryStream(state.Buffer);

                    object obj = null;
                    stream.Position = 0;
                    try
                    {
                        var watch = Stopwatch.StartNew();
                        watch.Start();
                        obj = formatter.Deserialize(stream);
                        watch.Stop();
                        Debug.WriteLine("Deserialized in " + watch.ElapsedMilliseconds + " ms.");

                        watch = Stopwatch.StartNew();
                        watch.Start();

                        ObjectArrived(obj, stateObjectClientDictionary[state]);

                        watch.Stop();
                        Debug.WriteLine("Processed in " + watch.ElapsedMilliseconds + " ms.");

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                    }

                    Array.Resize(ref state.Buffer, 9000000);
                }

                handler.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, ReceiveCallback, state);

                SerializeAndSendMessage(new KinectServerReadyMessage { Ready = true }, handler);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void AddWorkspace(Workspace workspace, KinectClient client)
        {
            StateObject state = clientStateObjectDictionary[client];
            if (state == null)
            {
                state = stateObjectClientDictionary.Keys.First();
            }
            WorkspaceMessage message = new WorkspaceMessage()
            {
                ID = workspace.ID,
                Name = workspace.Name,
                Vertices = workspace.Vertices.ToArray()
            };
            SerializeAndSendMessage(message, state.WorkSocket);
        }

        private void SerializeAndSendMessage(KinectDemoMessage msg, Socket socket)
        {

            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, msg);
            byte[] buffer = stream.ToArray();

            if (socket != null)
            {
                if (socket.Connected)
                {
                    socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
                }
            }
        }

        private void SendCallback(IAsyncResult ar)
        {

        }
    }
}
