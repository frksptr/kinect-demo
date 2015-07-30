using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using KinectDemoCommon.Model;
using Microsoft.Kinect;

namespace KinectDemoCommon.Util
{
    public class Converter
    {
        public static List<CameraSpacePoint> Point3DsToCameraSpacePoints(Point3D[] points)
        {
            if (points == null)
            {
                return null;
            }
            if (points.Length == 0)
            {
                return null;
            }
            List<CameraSpacePoint> cspList = new List<CameraSpacePoint>();

            foreach (Point3D point in points)
            {
                cspList.Add(Point3DToCameraSpacePoint(point));
            }

            return cspList;
        }

        public static List<Point3D> CameraSpacePointsToPoint3Ds(CameraSpacePoint[] cameraSpacePoints)
        {
            if (cameraSpacePoints == null)
            {
                return null;
            }
            if (cameraSpacePoints.Length == 0)
            {
                return null;
            }
            List<Point3D> point3Ds = new List<Point3D>();

            foreach (CameraSpacePoint point in cameraSpacePoints)
            {
                if (GeometryHelper.IsValidCameraPoint(point))
                {
                    point3Ds.Add(new Point3D
                    {
                        X = point.X,
                        Y = point.Y,
                        Z = point.Z
                    });
                }
            }

            return point3Ds;
        }

        public static Point3D CameraSpacePointToPoint3D(CameraSpacePoint cameraSpacePoint)
        {
            return new Point3D
            {
                X = cameraSpacePoint.X,
                Y = cameraSpacePoint.Y,
                Z = cameraSpacePoint.Z
            };
        }

        public static CameraSpacePoint Point3DToCameraSpacePoint(Point3D point)
        {
            return new CameraSpacePoint()
            {
                X = (float)point.X,
                Y = (float)point.Y,
                Z = (float)point.Z
            };
        }


        public static double[,] Point3DToPointArrays(Point3D[] points)
        {
            double[,] vectors = new double[points.Count(), 4];
            for (int i = 0; i < points.Count(); i++)
            {
                double x = points[i].X;
                double y = points[i].Y;
                double z = points[i].Z;
                vectors[i, 0] = x;
                vectors[i, 1] = y;
                vectors[i, 2] = z;
                vectors[i, 3] = 1;
            }
            return vectors;
        }

        public static List<Point3D> NullablePoint3DsToPoint3Ds(List<NullablePoint3D> nullablePoints)
        {
            List<Point3D> points = new List<Point3D>();

            foreach (NullablePoint3D point in nullablePoints)
            {
                if (point != null)
                {
                    points.Add(new Point3D(point.X, point.Y, point.Z));
                }
            }
            return points;
        }

    }
}
