using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectDemoSGL
{
    public class KinectClient
    {
        private static int i = 0;
        public string Name { get; set; }

        public KinectClient()
        {
            Name = "Client_" + i;
            i++;
        }
    }
}
