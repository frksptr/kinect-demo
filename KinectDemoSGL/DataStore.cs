using System.Collections.Generic;
using System.Linq;
using KinectDemoCommon.Model;

namespace KinectDemoSGL
{
    // Singleton
    class DataStore
    {
        private static DataStore dataStore;

        private Dictionary<string, Workspace> workspaceDictionary;

        private List<KinectClient> kinectClients;

        private Dictionary<string, KinectClient> workspaceClientDictionary;

        private Dictionary<KinectClient, NullablePoint3D[]> clientPointCloudDictionary;

        private Dictionary<KinectClient, List<SerializableBody>> clientCalibrationBodies;

        public static DataStore Instance
        {
            get { return dataStore ?? (dataStore = new DataStore()); }
        }

        private DataStore()
        {
            kinectClients = new List<KinectClient>();

            workspaceDictionary = new Dictionary<string, Workspace>();

            workspaceClientDictionary = new Dictionary<string, KinectClient>();

            clientCalibrationBodies = new Dictionary<KinectClient, List<SerializableBody>>();
        }

        public void AddClientIfNotExists(KinectClient client)
        {
            if (!kinectClients.Contains(client))
            {
                kinectClients.Add(client);

                clientCalibrationBodies[client] = new List<SerializableBody>();
            }
        }
        public List<KinectClient> GetClients()
        {
            return kinectClients;
        }

        public void AddOrUpdateWorkspace(string workspaceId, Workspace workspace, KinectClient client)
        {
            AddClientIfNotExists(client);

            if (!workspaceDictionary.Keys.Contains(workspaceId))
            {
                workspaceDictionary.Add(workspaceId, workspace);
                workspaceClientDictionary.Add(workspaceId, client);
            }
            else
            {
                workspaceDictionary[workspace.ID] = workspace;
            }
        }

        public void DeleteWorkspace(Workspace workspace)
        {
            workspaceDictionary.Remove(workspace.ID);
            workspaceClientDictionary.Remove(workspace.ID);
        }

        public Workspace GetWorkspace(string workspaceID)
        {
            return workspaceDictionary[workspaceID];
        }

        public List<Workspace> GetAllWorkspaces()
        {
            return workspaceDictionary.Values.ToList();
        }

        public KinectClient GetClientForWorkspace(string workspaceID)
        {
            return workspaceClientDictionary[workspaceID];
        }

        public void AddCalibrationBody(KinectClient client, SerializableBody body)
        {
            AddClientIfNotExists(client);
            clientCalibrationBodies[client].Add(body);
        }

        public List<SerializableBody> GetCalibrationBodiesForClient(KinectClient client)
        {
            return clientCalibrationBodies[client];
        }

        public void AddOrUpdatePointCloud(KinectClient client, NullablePoint3D[] pointCloud)
        {
            AddClientIfNotExists(client);
            clientPointCloudDictionary[client] = pointCloud;
        }

        public NullablePoint3D[] GetPointCloudForClient(KinectClient client)
        {
            return clientPointCloudDictionary[client];
        }
    }
}
