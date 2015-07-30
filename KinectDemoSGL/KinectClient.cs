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
