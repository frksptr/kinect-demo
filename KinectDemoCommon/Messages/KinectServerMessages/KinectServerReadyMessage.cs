﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectDemoCommon.Messages.KinectServerMessages
{
    [Serializable]
    public class KinectServerReadyMessage : KinectServerMessage
    {
        public bool Ready { get; set; }
    }
}