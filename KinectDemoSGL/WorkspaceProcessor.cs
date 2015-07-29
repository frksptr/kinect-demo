 using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using KinectDemoCommon;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;

namespace KinectDemoSGL
{
    public class WorkspaceProcessor
    {
        
        public static void SetWorkspaceCloudRealVerticesAndCenter(Workspace workspace, FrameSize depthFrameSize)
        {
            double sumX = 0;
            double sumY = 0;
            double sumZ = 0;
            double numberOfPoints = 0;
            Point[] workspaceVertices = {
                new Point(workspace.Vertices[0].X, workspace.Vertices[0].Y), 
                new Point(workspace.Vertices[1].X, workspace.Vertices[1].Y), 
                new Point(workspace.Vertices[2].X, workspace.Vertices[2].Y), 
                new Point(workspace.Vertices[3].X, workspace.Vertices[3].Y),
            };

            List<Point3D> pointCloud = new List<Point3D>();
            for (int i = 0; i < DataStore.Instance.FullPointCloud.Count(); i++)
            {
                if (GeometryHelper.InsidePolygon(workspaceVertices, new Point(i%depthFrameSize.Width, i/depthFrameSize.Width)))
                {
                    NullablePoint3D point = DataStore.Instance.FullPointCloud[i];
                    if (point != null)
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
            }
            workspace.Center = new Point3D(sumX / numberOfPoints, sumY / numberOfPoints, sumZ / numberOfPoints);

            workspace.PointCloud = pointCloud.ToArray();

            SetRealVertices(workspace);

        }

        private static void SetRealVertices(Workspace workspace)
        {
            Vector<double> fittedPlaneVector = GeometryHelper.FitPlaneToPoints(workspace.PointCloud.ToArray());

            if (fittedPlaneVector == null)
            {
                return;
            }

            Point3D projectedPoint = GeometryHelper.ProjectPoint3DToPlane(workspace.PointCloud.First(), fittedPlaneVector);

            Vector<double> planeNormal = new DenseVector(new[] { fittedPlaneVector[0], fittedPlaneVector[1], fittedPlaneVector[2] });

            Point3D[] vertices3D = workspace.Vertices3D;

            Point[] vertices = workspace.Vertices.ToArray();

            for (int i = 0; i < vertices.Length; i++)
            {

                Vector<double> pointOnPlane = new DenseVector(new[] { projectedPoint.X, projectedPoint.Y, projectedPoint.Z });
                Vector<double> pointOnLine = new DenseVector(new double[] { vertices3D[i].X, vertices3D[i].Y, vertices3D[i].Z });

                double d = (pointOnPlane.Subtract(pointOnLine)).DotProduct(planeNormal) / (pointOnLine.DotProduct(planeNormal));

                Vector<double> intersection = pointOnLine + pointOnLine.Multiply(d);

                workspace.FittedVertices[i] = new Point3D(intersection[0], intersection[1], intersection[2]);
            }

            workspace.PlaneVector = fittedPlaneVector;
        }
    }
}
