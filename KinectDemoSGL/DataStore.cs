using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using KinectDemoCommon;
using KinectDemoCommon.Model;

namespace KinectDemoSGL
{
    // Singleton
    class DataStore
    {
        private static DataStore dataStore;

        private Dictionary<string, Workspace> workspaceDictionary;

        private ObservableCollection<KinectClient> kinectClients;

        private Dictionary<string, KinectClient> workspaceClientDictionary;

        private Dictionary<KinectClient, PointCloud> clientPointCloudDictionary;

        private Dictionary<KinectClient, List<SerializableBody>> clientCalibrationBodies;

        private Dictionary<KinectClient, KinectStreamerConfig> clientConfigurationDictionary;

        public static DataStore Instance
        {
            get { return dataStore ?? (dataStore = new DataStore()); }
        }

        private DataStore()
        {
            kinectClients = new ObservableCollection<KinectClient>();

            workspaceDictionary = new Dictionary<string, Workspace>();

            workspaceClientDictionary = new Dictionary<string, KinectClient>();

            clientCalibrationBodies = new Dictionary<KinectClient, List<SerializableBody>>();

            clientPointCloudDictionary = new Dictionary<KinectClient, PointCloud>();

            clientConfigurationDictionary = new Dictionary<KinectClient, KinectStreamerConfig>();
        }

        public KinectClient CreateClientIfNotExists(EndPoint endPoint)
        {
            if (kinectClients.Count == 0)
            {
                return CreateClient(endPoint);
            }
            foreach (KinectClient client in kinectClients)
            {
                if (client.IP.Equals(endPoint.ToString().Split(':')[0]))
                {
                    return client;
                }
            }

            return CreateClient(endPoint);
        }

        private KinectClient CreateClient(EndPoint endPoint)
        {
            KinectClient kinectClient;
            kinectClient = new KinectClient(endPoint);
            kinectClients.Add(kinectClient);
            clientCalibrationBodies[kinectClient] = new List<SerializableBody>();
            return kinectClient;
        }

        public ObservableCollection<KinectClient> GetClients()
        {
            return kinectClients;
        }

        public void AddOrUpdateWorkspace(string workspaceId, Workspace workspace, KinectClient client)
        {
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
            clientCalibrationBodies[client].Add(body);
        }

        public List<SerializableBody> GetCalibrationBodiesForClient(KinectClient client)
        {
            return clientCalibrationBodies[client];
        }

        public Dictionary<KinectClient, List<SerializableBody>> GetAllCalibrationBodies()
        {
            Dictionary<KinectClient,List<SerializableBody>> dataDict = new Dictionary<KinectClient, List<SerializableBody>>();
            foreach (KinectClient client in kinectClients)
            {
                dataDict.Add(client, clientCalibrationBodies[client]);
            }
            return dataDict;
        }

        public void AddOrUpdatePointCloud(KinectClient client, PointCloud pointCloud)
        {
            clientPointCloudDictionary[client] = pointCloud;
        }

        public NullablePoint3D[] GetPointCloudForClient(KinectClient client)
        {
            try
            {
                return clientPointCloudDictionary[client].Points;
            }
            catch
            {
                return null;
            }
        }

        public PointCloud GetColoredPointCloudForClient(KinectClient client)
        {
            return clientPointCloudDictionary[client];
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

        public void AddOrUpdateConfiguration(KinectClient client, KinectStreamerConfig kinectStreamerConfig)
        {
            clientConfigurationDictionary[client] = kinectStreamerConfig;
        }

        public KinectStreamerConfig GetConfigurationForClient(KinectClient client)
        {
            try
            {
                return clientConfigurationDictionary[client];
            }
            catch (Exception)
            {

                return null;
            }
        }
    }
}
