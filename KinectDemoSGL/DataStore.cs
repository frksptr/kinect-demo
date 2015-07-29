using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using KinectDemoCommon.Model;
using KinectDemoCommon.Messages.KinectClientMessages.KinectStreamerMessages;

namespace KinectDemoSGL
{
    // Singleton
    class DataStore
    {
        private static DataStore dataStore;

        public Dictionary<string, Workspace> WorkspaceDictionary = new Dictionary<string, Workspace>();

        private List<KinectClient> kinectClients = new List<KinectClient>();

        public Dictionary<KinectClient, NullablePoint3D[]> clientPointClouds = new Dictionary<KinectClient, NullablePoint3D[]>();

        public Dictionary<KinectClient, List<SerializableBody>> clientCalibrationBodies = new Dictionary<KinectClient, List<SerializableBody>>();

        public NullablePoint3D[] FullPointCloud { get; set; }

        public List<KinectClient> KinectClients
        {
            get
            {
                return kinectClients;
            }
        }

        public static DataStore Instance
        {
            get { return dataStore ?? (dataStore = new DataStore()); }
        }

        public void AddOrUpdateWorkspace(string workspaceId, Workspace workspace)
        {
            if (!WorkspaceDictionary.Keys.Contains(workspaceId))
            {
                WorkspaceDictionary.Add(workspaceId, workspace);
            }
            else
            {
                WorkspaceDictionary[workspace.ID] = workspace;
            }
        }

        public void DeleteWorkspace(Workspace workspace)
        {
            WorkspaceDictionary.Remove(workspace.ID);
        }

        public void AddCalibrationBody(KinectClient client, SerializableBody body) {
            clientCalibrationBodies[client].Add(body);
        }

        public void AddClient(KinectClient client)
        {
            if (!kinectClients.Contains(client))
            {
                kinectClients.Add(client);
            }
            if (!clientPointClouds.Keys.Contains(client))
            {
                clientPointClouds.Add(client,new NullablePoint3D[]{});
            }
            if (!clientCalibrationBodies.Keys.Contains(client))
            {
                clientCalibrationBodies.Add(client, new List<SerializableBody>());
            }
        }
    }
}
