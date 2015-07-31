using System;
using System.Collections.Generic;
using System.Linq;
using KinectDemoCommon.Model;

namespace KinectDemoSGL
{
    // Singleton
    class DataStore
    {
        private static DataStore dataStore;

        //  TODO:   bind dictionaries to a workspacelist, modify only said list
        public Dictionary<string, Workspace> WorkspaceDictionary { get;set; }

        public Dictionary<string, KinectClient> WorkspaceClientDictionary { get; set; }


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

        private DataStore()
        {
            WorkspaceDictionary = new Dictionary<string, Workspace>();
            WorkspaceClientDictionary = new Dictionary<string, KinectClient>();
        }

        public void AddOrUpdateWorkspace(string workspaceId, Workspace workspace, KinectClient client)
        {
            if (!WorkspaceDictionary.Keys.Contains(workspaceId))
            {
                WorkspaceDictionary.Add(workspaceId, workspace);
                WorkspaceClientDictionary.Add(workspaceId, client);
            }
            else
            {
                WorkspaceDictionary[workspace.ID] = workspace;
            }
        }

        public void DeleteWorkspace(Workspace workspace)
        {
            WorkspaceDictionary.Remove(workspace.ID);
            WorkspaceClientDictionary.Remove(workspace.ID);
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

        // Returns with the next kinect client to given currentClient in the clients list.
        // If currentClients is null, returns with first client.
        public KinectClient GetNextClient(KinectClient currentClient)
        {
            KinectClient nextClient = null;
            if (kinectClients.Count == 0)
            {
                return null;
            }
            try
            {
                if (currentClient == null)
                {
                    nextClient = kinectClients[0];
                }
                else
                {
                    nextClient = kinectClients[kinectClients.IndexOf(currentClient) + 1];    
                }
            }
            catch (Exception)
            {
                nextClient = kinectClients[0];
            }
            return nextClient;
        }
    }
}
