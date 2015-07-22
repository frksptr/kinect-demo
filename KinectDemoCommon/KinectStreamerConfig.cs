namespace KinectDemoCommon
{
    public class KinectStreamerConfig
    {
        public bool ProvideDepthData { get; set; }
        public bool ProvideBodyData { get; set; }
        public bool ProvideColorData { get; set; }
        public bool ProvidePointCloudData { get; set; }

        public KinectStreamerConfig()
        {
            ProvideDepthData = false;

            ProvideColorData = false;

            ProvideBodyData = false;

            ProvidePointCloudData = false;
        }

    }
}
