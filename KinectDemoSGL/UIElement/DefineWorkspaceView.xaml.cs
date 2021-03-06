﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;
using KinectDemoSGL.Annotations;

namespace KinectDemoSGL.UIElement
{
    /// <summary>
    /// Interaction logic for DefineWorkspaceView.xaml
    /// </summary>
    public partial class DefineWorkspaceView : INotifyPropertyChanged
    {
        //readonly KinectStreamer kinectStreamer;

        WriteableBitmap depthBitmap = null;
        public int[] DepthFrameSize { get; set; }
        private byte[] depthPixels;
        public KinectClient ActiveClient { get; set; }

        private ServerMessageProcessor serverMessageProcessor;
        private KinectServer kinectServer;

        public DefineWorkspaceView()
        {
            DataContext = this;
            //  TODO: set width, height from framedescription data provided by client
            depthBitmap = new WriteableBitmap(512, 424, 96.0, 96.0, PixelFormats.Gray8, null);
            kinectServer = KinectServer.Instance;
            serverMessageProcessor = ServerMessageProcessor.Instance;

            InitializeComponent();
        }

        private void kinectServer_DepthDataReady(KinectDemoMessage message, KinectClient client)
        {
            DepthStreamMessage msg = (DepthStreamMessage)message;
            ActiveClient = client;
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
                serverMessageProcessor.DepthMessageArrived += kinectServer_DepthDataReady;
            }
            else
            {
                serverMessageProcessor.DepthMessageArrived -= kinectServer_DepthDataReady;
            }
        }

        private void ChangeClientButton_OnClick(object sender, RoutedEventArgs e)
        {
            ObservableCollection<KinectClient> clients = DataStore.Instance.GetClients();
            var index = clients.IndexOf(ActiveClient);
            try
            {
                ActiveClient = clients[index + 1];
            }
            catch (Exception ex)
            {
                ActiveClient = clients[0];
            }
        }
    }
}
