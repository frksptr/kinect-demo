using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KinectDemoCommon.Annotations;
using KinectDemoSGL;
using KinectDemoCommon.KinectStreamerMessages;

namespace KinectDemoCommon.UIElement
{
    /// <summary>
    /// Interaction logic for CameraWorkspace.xaml
    /// </summary>
    public partial class CameraWorkspace : INotifyPropertyChanged
    {
        //readonly KinectStreamer kinectStreamer;

        WriteableBitmap depthBitmap;
        public int[] DepthFrameSize { get; set; }
        private byte[] depthPixels;
        public CameraWorkspace()
        {
            DataContext = this;
            this.depthBitmap = depthBitmap = new WriteableBitmap(512, 424, 96.0, 96.0, PixelFormats.Gray8, null);
            KinectServer kinectServer = KinectServer.Instance;
            kinectServer.DepthDataArrived += kinectServer_DepthDataReady;

            InitializeComponent();
        }

        private void kinectServer_DepthDataReady(KinectStreamerMessages.KinectStreamerMessage message)
        {
            DepthStreamMessage msg = (DepthStreamMessage)message;
            RefreshBitmap(msg.DepthPixels, msg.DepthFrameSize);
        }

        public void RefreshBitmap(byte[] depthPixels, int[] depthFrameSize)
        {
            this.depthPixels = depthPixels;
            RenderDepthPixels();
            //OnPropertyChanged("ImageSource");
        }

        private void RenderDepthPixels()
        {
            try
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    depthBitmap.WritePixels(
                        new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight),
                        depthPixels,
                        depthBitmap.PixelWidth,
                        0);
                }));
            
            
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        public ImageSource ImageSource
        {
            get
            {
                return depthBitmap;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CameraWorkspace_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //    if ((bool) e.NewValue == true)
            //    {
            //        kinectStreamer.DepthDataReady += kinectStreamer_DepthDataReady;
            //        kinectStreamer.KinectStreamerConfig.ProvideDepthData = true;
            //    }
            //    else
            //    {
            //        kinectStreamer.DepthDataReady -= kinectStreamer_DepthDataReady;
            //        kinectStreamer.KinectStreamerConfig.ProvideDepthData = false;
            //    }
        }
    }
}
