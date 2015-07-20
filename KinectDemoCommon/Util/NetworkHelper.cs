using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace KinectDemoCommon.Util
{
    public class NetworkHelper
    {
        public static string LocalIPAddress()
        {
            string localIp = "";
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
            {
                localIp = ip.ToString();
                break;
            }
            return localIp;
        }
    }
}
