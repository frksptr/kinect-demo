using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;

namespace KinectDemoClient
{
    class WorkspaceProcessor
    {
        public static IEnumerable<CameraSpacePoint> AllCameraSpacePoints { get; set; }

        public static Workspace ProcessWorkspace(Workspace workspace)
        {
            Workspace newWorkspace = workspace;

            //if (newWorkspace.PointCloud == null)
            //{
            //    SetWorkspaceCloudAndCenter(newWorkspace);
            //}

            //SetRealVertices(newWorkspace);

            Point[] vertices = workspace.Vertices.ToArray();

            CameraSpacePoint[] csps = { new CameraSpacePoint() };

            for (int i = 0; i < vertices.Length; i++)
            {
                Point vertex = vertices[i];

                KinectStreamer.Instance.CoordinateMapper.MapDepthPointsToCameraSpace(
                    new[] {
                        new DepthSpacePoint {
                            X = (float)vertex.X,
                            Y = (float)vertex.Y
                        }
                    },
                    new ushort[] { 1 }, csps);
                newWorkspace.Vertices3D[i] = new Point3D(csps[0].X, csps[0].Y, csps[0].Z);
            }

            return newWorkspace;
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

            CameraSpacePoint[] csps = { new CameraSpacePoint() };

            Point[] vertices = workspace.Vertices.ToArray();

            for (int i = 0; i < vertices.Length; i++)
            {
                Point vertex = vertices[i];

                KinectStreamer.Instance.CoordinateMapper.MapDepthPointsToCameraSpace(
                    new[] {
                        new DepthSpacePoint {
                            X = (float)vertex.X,
                            Y = (float)vertex.Y
                        }
                    },
                    new ushort[] { 1 }, csps);

                Vector<double> pointOnPlane = new DenseVector(new[] { projectedPoint.X, projectedPoint.Y, projectedPoint.Z });
                Vector<double> pointOnLine = new DenseVector(new double[] { csps[0].X, csps[0].Y, csps[0].Z });

                double d = (pointOnPlane.Subtract(pointOnLine)).DotProduct(planeNormal) / (pointOnLine.DotProduct(planeNormal));

                Vector<double> intersection = pointOnLine + pointOnLine.Multiply(d);

                workspace.FittedVertices[i] = new Point3D(intersection[0], intersection[1], intersection[2]);
            }

            workspace.PlaneVector = fittedPlaneVector;
        }

        public void UpdatePointCloudAndCenter()
        {
            //SetWorkspaceCloudAndCenter(ActiveWorkspace);
            //SetCameraCenterAndShowCloud(MainViewPort, ActiveWorkspace);
        }

        private static void SetWorkspaceCloudAndCenter(Workspace workspace)
        {
            //AllCameraSpacePoints = KinectStreamer.Instance.GenerateFullPointCloud();

            //double sumX = 0;
            //double sumY = 0;
            //double sumZ = 0;
            //double numberOfPoints = 0;

            //List<Point3D> cameraSpacePoints = new List<Point3D>();
            //List<DepthSpacePoint> dspList = new List<DepthSpacePoint>();
            //foreach (CameraSpacePoint csp in AllCameraSpacePoints)
            //{
            //    if (GeometryHelper.IsValidCameraPoint(csp))
            //    {

            //        DepthSpacePoint dsp = KinectStreamer.Instance.CoordinateMapper.MapCameraPointToDepthSpace(csp);
            //        dspList.Add(dsp);

            //        if (GeometryHelper.InsidePolygon(workspace.Vertices.ToArray(), new Point(dsp.X, dsp.Y)))
            //        {
            //            double x = csp.X;
            //            double y = csp.Y;
            //            double z = csp.Z;

            //            sumX += x;
            //            sumY += y;
            //            sumZ += z;

            //            numberOfPoints += 1;

            //            cameraSpacePoints.Add(new Point3D(csp.X, csp.Y, csp.Z));

            //        }
            //    }
            //}

            //workspace.Center = new Point3D(sumX / numberOfPoints, sumY / numberOfPoints, sumZ / numberOfPoints);

            //workspace.PointCloud = cameraSpacePoints.ToArray();
        }
    }
}
