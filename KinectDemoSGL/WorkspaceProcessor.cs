using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace KinectDemoCommon
{
    public class WorkspaceProcessor
    {
        
        public static void SetWorkspaceCloudAndCenter(Workspace workspace)
        {
            Polygon polygon = new Polygon();
            PointCollection pointCollection = new PointCollection();
            foreach (Point3D p in workspace.Vertices3D)
            {
                pointCollection.Add(new Point(p.X, p.Y));
            }

            int height = (int)polygon.ActualHeight;
            int width = (int)polygon.ActualWidth;

            double sumX = 0;
            double sumY = 0;
            double sumZ = 0;
            double numberOfPoints = 0;

            List<Point3D> pointCloud = new List<Point3D>();
            foreach (Point3D point in DataStore.Instance.FullPointCloud)
            {
                Point point2D = new Point(point.X, point.Y);

                if (GeometryHelper.InsidePolygon(polygon, new Point(point2D.X, point2D.Y)))
                {
                    double x = point.X;
                    double y = point.Y;
                    double z = point.Z;

                    sumX += x;
                    sumY += y;
                    sumZ += z;

                    numberOfPoints += 1;

                    pointCloud.Add(new Point3D(point.X, point.Y, point.Z));

                }
            }
            workspace.Center = new Point3D(sumX / numberOfPoints, sumY / numberOfPoints, sumZ / numberOfPoints);

            workspace.PointCloud = pointCloud.ToArray();
        }
    }
}
