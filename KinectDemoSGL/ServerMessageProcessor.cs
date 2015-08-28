using System;
using System.Collections.ObjectModel;
using System.Windows;
using KinectDemoCommon;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectClientMessages;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;
using KinectDemoCommon.Model;

namespace KinectDemoSGL
{
    public delegate void KinectMessageArrived(KinectDemoMessage message, KinectClient kinectClient);
    // Singleton
    class ServerMessageProcessor
    {
        public KinectMessageArrived DepthMessageArrived;
        public KinectMessageArrived ColorMessageArrived;
        public KinectMessageArrived BodyMessageArrived;
        public KinectMessageArrived PointCloudMessageArrived;
        public KinectMessageArrived ColoredPointCloudMessageArrived;
        public KinectMessageArrived TextMessageArrived;
        public KinectMessageArrived WorkspaceUpdated;
        public KinectMessageArrived ConfigurationMessageArrived;
        public KinectMessageArrived CalibrationMessageArrived;
        private FrameSize depthFrameSize;
        private DataStore dataStore = DataStore.Instance;

        private static ServerMessageProcessor serverMessageProcessor;

        public static ServerMessageProcessor Instance
        {
            get { return serverMessageProcessor ?? (serverMessageProcessor = new ServerMessageProcessor()); }
        }

        private ServerMessageProcessor() { }

        public void ProcessStreamMessage(object obj, KinectClient sender)
        {
            if (obj == null)
            {
                return;
            }
            if (obj is KinectClientMessage)
            {
                if (obj is UnifiedStreamerMessage)
                {
                    UnifiedStreamerMessage msg = (UnifiedStreamerMessage)obj;
                    if (msg.BodyStreamMessage != null)
                    {
                        ProcessBodyStreamMessage(msg.BodyStreamMessage, sender);
                    }
                    if (msg.ColorStreamMessage != null)
                    {
                        ProcessColorStreamMessage(msg.ColorStreamMessage, sender);
                    }
                    if (msg.DepthStreamMessage != null)
                    {
                        ProcessDepthStreamMessage(msg.DepthStreamMessage, sender);
                    }
                    if (msg.PointCloudStreamMessage != null)
                    {
                        ProcessPointCloudStreamMessage(msg.PointCloudStreamMessage, sender);
                    }
                }
                else if (obj is DepthStreamMessage)
                {
                    ProcessDepthStreamMessage(obj, sender);
                }
                else if (obj is ColorStreamMessage)
                {
                    ProcessColorStreamMessage(obj, sender);
                }
                else if (obj is PointCloudStreamMessage)
                {
                    ProcessPointCloudStreamMessage(obj, sender);
                }
                else if (obj is BodyStreamMessage)
                {
                    ProcessBodyStreamMessage(obj, sender);
                }
                else if (obj is CalibrationDataMessage)
                {
                    ProcessCalibrationDataMessage(obj, sender);
                }

            }
            else if (obj is WorkspaceMessage)
            {
                ProcessWorkspaceMessage(obj, sender);
            }
            else if (obj is TextMessage)
            {
                ProcessTextMessage(obj, sender);
            }
            else if (obj is ClientConfigurationMessage)
            {
                ProcessConfigurationData(obj, sender);
            }
            else if (obj is CalibrationMessage)
            {
                ProcessCalibrationMessage(obj, sender);
            }
        }

        private void ProcessCalibrationMessage(object obj, KinectClient sender)
        {
            throw new NotImplementedException();
        }

        private void ProcessConfigurationData(object obj, KinectClient sender)
        {
            dataStore.AddOrUpdateConfiguration(sender, ((ClientConfigurationMessage) obj).Configuration);

            ClientConfigurationMessage msg = (ClientConfigurationMessage) obj;
            if (ConfigurationMessageArrived != null)
            {
                ConfigurationMessageArrived(msg, sender);
            }
        }

        private void ProcessCalibrationDataMessage(object obj, KinectClient sender)
        {
            CalibrationDataMessage msg = (CalibrationDataMessage) obj;
            dataStore.AddCalibrationBody(sender, msg.CalibrationBody);
            if (ConfigurationMessageArrived != null)
            {
                ConfigurationMessageArrived(msg, sender);
            }
        }

        private void ProcessTextMessage(object obj, KinectClient sender)
        {
            TextMessage msg = (TextMessage)obj;
            if (TextMessageArrived != null)
            {
                TextMessageArrived(msg, sender);
            }
        }

        private void ProcessWorkspaceMessage(object obj, KinectClient sender)
        {
            WorkspaceMessage msg = (WorkspaceMessage)obj;
            Workspace workspace = dataStore.GetWorkspace(msg.ID);
            workspace.Name = msg.Name;
            workspace.Vertices = new ObservableCollection<Point>(msg.Vertices);
            workspace.Vertices3D = msg.Vertices3D;
            workspace.VertexDepths = msg.VertexDepths;
            WorkspaceProcessor.SetWorkspaceCloudRealVerticesAndCenter(workspace, depthFrameSize);
            WorkspaceUpdated((WorkspaceMessage)obj, sender);
        }

        private void ProcessBodyStreamMessage(object obj, KinectClient sender)
        {
            BodyStreamMessage msg = (BodyStreamMessage)obj;
            if (BodyMessageArrived != null)
            {
                BodyMessageArrived(msg, sender);
            }
        }

        private void ProcessPointCloudStreamMessage(object obj, KinectClient client)
        {
            bool coloredPointCloud = false;
            PointCloudStreamMessage msg = (PointCloudStreamMessage)obj;
            if (obj is ColoredPointCloudStreamMessage)
            {
                coloredPointCloud = true;
            }
            
            double[] doubleArray = msg.PointCloud;
            NullablePoint3D[] pointArray = new NullablePoint3D[doubleArray.Length / 3];
            for (int i = 0; i < doubleArray.Length; i += 3)
            {
                if (double.IsNegativeInfinity(doubleArray[i]))
                {
                    pointArray[i / 3] = null;
                }
                else
                {
                    pointArray[i / 3] = new NullablePoint3D(doubleArray[i], doubleArray[i + 1], doubleArray[i + 2]);
                }
            }

            PointCloud pointCloud = new PointCloud() {Points = pointArray};
            if (coloredPointCloud)
            {
                pointCloud.ColorBytes = ((ColoredPointCloudStreamMessage) msg).ColorPixels;
            }

            dataStore.AddOrUpdatePointCloud(client, pointCloud);

            if (PointCloudMessageArrived != null)
            {
                PointCloudMessageArrived(msg, client);
            }
            if (coloredPointCloud && ColoredPointCloudMessageArrived != null)
            {
                ColoredPointCloudMessageArrived(msg, client);
            }
            
        }

        private void ProcessColorStreamMessage(object obj, KinectClient sender)
        {
            if (ColorMessageArrived != null)
            {
                ColorMessageArrived((ColorStreamMessage)obj, sender);
            }
        }

        private void ProcessDepthStreamMessage(object obj, KinectClient sender)
        {
            if (DepthMessageArrived != null)
            {
                DepthMessageArrived((DepthStreamMessage)obj, sender);
                if (depthFrameSize == null)
                {
                    depthFrameSize = ((DepthStreamMessage)obj).DepthFrameSize;
                }
            }
        }

    }
}
