using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using KinectDemoCommon;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectServerMessages;
using KinectDemoCommon.Model;

namespace KinectDemoClient
{
    public delegate void KinectMessageArrived(KinectDemoMessage message);
    class ClientMessageProcessor
    {
        private static ClientMessageProcessor clientMessageProcessor;

        public static ClientMessageProcessor Instance
        {
            get { return clientMessageProcessor ?? (clientMessageProcessor = new ClientMessageProcessor()); }
        }

        private ClientMessageProcessor() { }

        public void ProcessStreamMessage(object obj)
        {
            if (obj is KinectDemoMessage)
            {
                if (obj is WorkspaceMessage)
                {
                    ProcessWorkspaceMessage(obj);
                }
                else if (obj is KinectServerReadyMessage)
                {
                    serverReady = ((KinectServerReadyMessage)obj).Ready;
                }
                else if (obj is ClientConfigurationMessage)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ClientConfigurationMessage msg = (ClientConfigurationMessage)obj;
                        //  TODO: bind
                        KinectStreamerConfig config = msg.Configuration;
                        kinectStreamer.KinectStreamerConfig = config;
                        DepthCheckbox.IsChecked = config.StreamDepthData;
                        ColorCheckbox.IsChecked = config.StreamColorData;
                        SkeletonCheckbox.IsChecked = config.StreamBodyData;
                        UnifiedCheckbox.IsChecked = config.SendAsOne;
                        PointCloudCheckbox.IsChecked = config.StreamPointCloudData;
                        ColoredPointCloudCheckbox.IsChecked = config.StreamColoredPointCloudData;
                        CalibrationCheckbox.IsChecked = config.ProvideCalibrationData;
                    });
                }
                else if (obj is CalibrationMessage)
                {
                    Dispatcher.Invoke(() =>
                    {
                        CalibrationMessage msg = (CalibrationMessage)obj;
                        if (msg.Message.Equals(CalibrationMessage.CalibrationMessageEnum.Start))
                        {
                            CalibrationCheckbox.IsChecked = true;
                        }
                        else
                        {
                            CalibrationCheckbox.IsChecked = false;
                        }
                    });
                }
            }
        }

        private static void ProcessWorkspaceMessage(object obj)
        {
            WorkspaceMessage msg = (WorkspaceMessage) obj;
            Workspace workspace = WorkspaceProcessor.ProcessWorkspace(
                new Workspace() {Vertices = new ObservableCollection<Point>(msg.Vertices)});
            WorkspaceMessage updatedMessage = new WorkspaceMessage()
            {
                ID = msg.ID,
                Name = msg.Name,
                VertexDepths = workspace.VertexDepths,
                Vertices3D = workspace.Vertices3D.ToArray(),
                Vertices = workspace.Vertices.ToArray(),
            };
            SerializeAndSendMessage(updatedMessage);
        }
    }
}
