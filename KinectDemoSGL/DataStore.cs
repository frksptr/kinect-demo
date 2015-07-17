using KinectDemoCommon.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace KinectDemoCommon
{
    // Singleton
    class DataStore
    {
        private static DataStore dataStore;

        public List<Workspace> workspaceList = new List<Workspace>();

        public Point3D[] FullPointCloud { get; set; }

        public static DataStore Instance
        {
            get { return dataStore ?? (dataStore = new DataStore()); }
        }

        public void addWorkspace(Workspace workspace)
        {
            if (!workspaceList.Contains(workspace))
            {
                workspaceList.Add(workspace);
            }
        }

        public void deleteWorkspace(Workspace workspace)
        {
            workspaceList.Remove(workspace);
        }
    }
}
