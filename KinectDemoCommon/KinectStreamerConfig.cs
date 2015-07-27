namespace KinectDemoCommon
{
    public class KinectStreamerConfig
    {
        public bool ProvideDepthData { get; set; }
        public bool ProvideBodyData { get; set; }
        public bool ProvideColorData { get; set; }
        public bool ProvidePointCloudData { get; set; }
        public bool ProvideCalibrationData { get; set; }

        public bool SendInUnified { get; set; }

        public KinectStreamerConfig()
        {
            ProvideDepthData = false;

            ProvideColorData = false;

            ProvideBodyData = false;

            ProvidePointCloudData = false;

            ProvideCalibrationData = false;

            SendInUnified = false;
        }

    }
}
