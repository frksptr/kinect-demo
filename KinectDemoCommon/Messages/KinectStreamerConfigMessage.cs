using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectDemoCommon.Messages
{
    class KinectStreamerConfigMessage
    {
        [Serializable]
        public class TextMessage : KinectDemoMessage
        {
            public KinectStreamerConfig KinectStreamerConfig { get; set; }
        }
    }
}
