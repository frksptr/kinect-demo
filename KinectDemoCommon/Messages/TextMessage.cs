using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectDemoCommon.Messages
{
    [Serializable]
    public class TextMessage : KinectDemoMessage
    {
        public string Text { get; set; }
    }
}
