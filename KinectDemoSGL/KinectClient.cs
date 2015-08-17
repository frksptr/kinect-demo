using System.Net;

namespace KinectDemoSGL
{
    public class KinectClient
    {
        private static int i = 0;
        public string Name { get; set; }

        public string IP { get; set; }

        public bool Connected { get; set; }

        public KinectClient(EndPoint endPoint)
        {
            Name = "Client_" + i;
            IP = endPoint.ToString().Split(':')[0];
            Connected = true;
            i++;
        }
    }
}
