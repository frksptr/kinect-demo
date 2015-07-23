using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using KinectDemoCommon.Model;

namespace KinectDemoCommon
{
    // Singleton
    class DataStore
    {
        private static DataStore dataStore;

        public Dictionary<string, Workspace> WorkspaceDictionary = new Dictionary<string, Workspace>();

        public List<KinectClient> kinectClients = new List<KinectClient>();

        public Dictionary<KinectClient, NullablePoint3D[]> clientPointClouds = new Dictionary<KinectClient, NullablePoint3D[]>();

        public NullablePoint3D[] FullPointCloud { get; set; }

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
    }
}
