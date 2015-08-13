using System;

namespace KinectDemoCommon
{
    [Serializable]
    public class KinectStreamerConfig
    {
        public bool StreamDepthData { get; set; }
        public bool StreamBodyData { get; set; }
        public bool StreamColorData { get; set; }
        public bool StreamPointCloudData { get; set; }
        public bool ProvideCalibrationData { get; set; }
        public bool SendAsOne { get; set; }

        public KinectStreamerConfig()
        {
            StreamDepthData = false;

            StreamColorData = false;

            StreamBodyData = false;

            StreamPointCloudData = false;

            ProvideCalibrationData = false;

            SendAsOne = false;
        }

    }
}
