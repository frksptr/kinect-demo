using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Media.Imaging;

namespace KinectDemoSGL
{
    class KinectServer
    {

        private Socket socket, clientSocket;
        private byte[] buffer;

        public KinectServer()
        {
            StartServer();
        }
        private void StartServer()
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Parse("192.168.0.21"), 3333));
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

                object obj = formatter.Deserialize(stream);

                string text = "";

                if (obj is WriteableBitmap)
                {
                    WriteableBitmap bitmap = obj as WriteableBitmap;
                    text = "jee";

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
