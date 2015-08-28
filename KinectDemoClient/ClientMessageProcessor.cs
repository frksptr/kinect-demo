using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Messages.KinectServerMessages;
using KinectDemoCommon.Model;

namespace KinectDemoClient
{
    public delegate void KinectMessageArrived(KinectDemoMessage message);
    class ClientMessageProcessor
    {
        public KinectMessageArrived ServerReadyMessageArrived;
        public KinectMessageArrived WorkspaceMessageArrived;
        public KinectMessageArrived ConfigurationMessageArrived;
        public KinectMessageArrived CalibrationMessageArrived;

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
                    ServerReadyMessageArrived((KinectServerReadyMessage)obj);
                    
                }
                else if (obj is ClientConfigurationMessage)
                {
                    ConfigurationMessageArrived((ClientConfigurationMessage) obj);
                }
                else if (obj is CalibrationMessage)
                {
                    CalibrationMessageArrived((CalibrationMessage) obj);
                }
            }
        }

        private void ProcessWorkspaceMessage(object obj)
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

            WorkspaceMessageArrived(updatedMessage);
        }
    }
}
