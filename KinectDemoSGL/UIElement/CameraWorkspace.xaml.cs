using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KinectDemoCommon.Annotations;
using Microsoft.Kinect;

namespace KinectDemoCommon.UIElement
{
    /// <summary>
    /// Interaction logic for CameraWorkspace.xaml
    /// </summary>
    public partial class CameraWorkspace : INotifyPropertyChanged
    {
    //    //readonly KinectStreamer kinectStreamer;

    //    WriteableBitmap depthBitmap;
    //    public int[] DepthFrameSize {get; set;}
    //    public CameraWorkspace()
    //    {
    //        DataContext = this;
    //        kinectStreamer = KinectStreamer.Instance;

    //        DepthFrameSize = new[] { 
    //            kinectStreamer.DepthFrameDescription.Width,
    //            kinectStreamer.DepthFrameDescription.Height
    //        };

    //        InitializeComponent();
    //    }

    //    void kinectStreamer_DepthDataReady(object sender, KinectStreamerEventArgs e)
    //    {
    //        depthBitmap = e.DepthBitmap;
    //        OnPropertyChanged("ImageSource");
    //    }

    //    public ImageSource ImageSource
    //    {
    //        get
    //        {
    //            return depthBitmap;
    //        }
    //    }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            //PropertyChangedEventHandler handler = PropertyChanged;
            //if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
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
