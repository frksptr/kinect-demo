using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using KinectDemoCommon.KinectStreamerMessages;
using KinectDemoCommon.UIElement;

namespace KinectDemoSGL
{
    public delegate void KinectServerDataArrived(KinectStreamerMessage message);

    // Singleton
    class KinectServer
    {


        string ip = "192.168.32.1";
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
                socket.ReceiveBufferSize = 300000;
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
                
                if (obj is KinectStreamerMessage)
                {
                    if (obj is DepthStreamMessage)
                    {
                        DepthDataArrived((DepthStreamMessage)obj);
                    }
                    if (obj is ColorStreamMessage)
                    {

                    }
                }
                
                //if (obj is Person)
                //{
                //    Person person = obj as Person;
                //    text = "Person: \n Name " + person.Name + ": \n Age: " + person.Age;
                //}


                Array.Resize(ref buffer, clientSocket.ReceiveBufferSize);

                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
