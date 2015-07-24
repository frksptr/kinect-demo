using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KinectDemoCommon.Annotations;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;
using System.Net.Sockets;

namespace KinectDemoCommon.UIElement
{
    /// <summary>
    /// Interaction logic for CameraWorkspace.xaml
    /// </summary>
    public partial class CameraWorkspace : INotifyPropertyChanged
    {
        //readonly KinectStreamer kinectStreamer;

        WriteableBitmap depthBitmap = null;
        public int[] DepthFrameSize { get; set; }
        private byte[] depthPixels;
        private MessageProcessor messageProcessor;
        private KinectServer kinectServer;

        public CameraWorkspace()
        {
            DataContext = this;
            //  TODO: set width, height from framedescription data provided by client
            depthBitmap = new WriteableBitmap(512, 424, 96.0, 96.0, PixelFormats.Gray8, null);
            kinectServer = KinectServer.Instance;
            messageProcessor = kinectServer.MessageProcessor;

            InitializeComponent();
        }

        private void kinectServer_DepthDataReady(KinectDemoMessage message, KinectClient client)
        {
            DepthStreamMessage msg = (DepthStreamMessage)message;
            RefreshBitmap(msg.DepthPixels);
        }

        public void RefreshBitmap(byte[] depthPixels)
        {
            this.depthPixels = depthPixels;
            RenderDepthPixels();
            //OnPropertyChanged("ImageSource");
        }

        private void RenderDepthPixels()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    depthBitmap.WritePixels(
                        new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight),
                        depthPixels,
                        depthBitmap.PixelWidth,
                        0);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
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
            if ((bool)e.NewValue == true)
            {
                messageProcessor.DepthDataArrived += kinectServer_DepthDataReady;
            }
            else
            {
                messageProcessor.DepthDataArrived -= kinectServer_DepthDataReady;
            }
        }
    }
}
