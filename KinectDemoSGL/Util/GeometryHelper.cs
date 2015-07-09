using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace KinectDemo.Util
{
    class GeometryHelper
    {
        public static bool isNumber(float num)
        {
            return !float.IsNaN(num) && !float.IsInfinity(num);
        }

        public static bool isValidCameraPoint(CameraSpacePoint point)
        {
            return isNumber((float)point.X) && isNumber((float)point.Y) && isNumber((float)point.Z);
        }

        public static double[] normalize(double[] points)
        {
            double squareSum = 0;
            double[] normalizedPoints = new double[points.Length];
            for (int i = 0; i < points.Length; ++i)
            {
                squareSum += points[i] * points[i];
            }
            double length = Math.Sqrt(squareSum);

            for (int i = 0; i < points.Length; ++i)
            {
                normalizedPoints[i] = points[i] / length;
            }
            return normalizedPoints;
        }

        public static double[,] point3DToPointArrays(Point3D[] points)
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

        public static Vector<double> fitPlaneToPoints(Point3D[] points)
        {
            double[,] pointArrays = point3DToPointArrays(points);

            MathNet.Numerics.LinearAlgebra.Matrix<double> PointMatrix = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.DenseOfArray(pointArrays);

            var SVDDecomp = PointMatrix.Svd(true);

            var VT = SVDDecomp.VT;

            var solution = VT.Row(VT.RowCount - 1);

            return solution;

        }


        public static Point3D[] projectPoints3DToPlane(Point3D[] points, Vector<double> planeVectors)
        {
            Point3D[] projectedPoints = new Point3D[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                projectedPoints[i] = projectPoint3DToPlane(points[i], planeVectors);
            }

            return projectedPoints;
        }

        public static Point3D projectPoint3DToPlane(Point3D point, Vector<double> planeVectors)
        {

            double distance = calculatePointPlaneDistance(point, planeVectors);

            double a = planeVectors[0];
            double b = planeVectors[1];
            double c = planeVectors[2];

            Vector<double> planeNormal = new DenseVector(new double[] { a, b, c });

            double x = point.X;
            double y = point.Y;
            double z = point.Z;

            Vector<double> pointVector = new DenseVector(new double[] { x, y, z });

            pointVector.Subtract(planeNormal.Multiply(distance));

            return new Point3D()
            {
                X = pointVector[0],
                Y = pointVector[1],
                Z = pointVector[2]
            };
        }

        public static double calculatePointPlaneDistance(Point3D point, Vector<double> planeVector)
        {
            // ax + by + cz + d = 0
            double a = planeVector[0];
            double b = planeVector[1];
            double c = planeVector[2];
            double d = planeVector[3];

            Vector<double> planeNormal = new DenseVector(new double[] { a, b, c });

            double x = point.X;
            double y = point.Y;
            double z = point.Z;

            Vector<double> pointVector = new DenseVector(new double[] { x, y, z });

            double distance = (a * x + b * y + c * z + d) / Math.Sqrt(a * a + b * b + c * c);
            return distance;
        }

        public static bool insidePolygon(Polygon polygon, Point point)
        {
            int i, j;
            bool c = false;
            int nvert = polygon.Points.Count;
            Point[] polyPoints = new Point[nvert];
            polygon.Points.CopyTo(polyPoints, 0);
            for (i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                if (((polyPoints[i].Y > point.Y) != (polyPoints[j].Y > point.Y)) &&
                 (point.X < (polyPoints[j].X - polyPoints[i].X) * (point.Y - polyPoints[i].Y) / (polyPoints[j].Y - polyPoints[i].Y) + polyPoints[i].X))
                    c = !c;
            }
            return c;
        }

        public static bool insidePolygon3D(Point3D[] polyVertices, Point3D projectedPoint)
        {
            PointCollection points = new PointCollection();
            foreach (Point3D point in polyVertices)
            {
                points.Add(new Point(point.X, point.Y));
            }
            return insidePolygon(new Polygon() { Points = points }, new Point(projectedPoint.X, projectedPoint.Y));
        }


        public static Model3DGroup CreateTriangleModel(Point3D p0, Point3D p1, Point3D p2, Color color)
        {
            MeshGeometry3D mymesh = new MeshGeometry3D();
            mymesh.Positions.Add(p0);
            mymesh.Positions.Add(p1);
            mymesh.Positions.Add(p2);
            mymesh.TriangleIndices.Add(0);
            mymesh.TriangleIndices.Add(1);
            mymesh.TriangleIndices.Add(2);
            Vector3D Normal = CalculateTraingleNormal(p0, p1, p2);
            mymesh.Normals.Add(Normal);
            mymesh.Normals.Add(Normal);
            mymesh.Normals.Add(Normal);
            Material Material = new DiffuseMaterial(
                new SolidColorBrush(color) { Opacity = 0.5 });
            GeometryModel3D model = new GeometryModel3D(
                mymesh, Material);
            Model3DGroup Group = new Model3DGroup();
            Group.Children.Add(model);
            return Group;
        }

        public static Vector3D CalculateTraingleNormal(Point3D p0, Point3D p1, Point3D p2)
        {
            Vector3D v0 = new Vector3D(
                p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3D v1 = new Vector3D(
                p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            return Vector3D.CrossProduct(v0, v1);
        }

        public static Point3D cameraSpacePointToPoint3D(CameraSpacePoint cameraSpacePoint)
        {
            return new Point3D()
            {
                X = cameraSpacePoint.X,
                Y = cameraSpacePoint.Y,
                Z = cameraSpacePoint.Z
            };
        }
        public static List<Point3D> cameraSpacePointsToPoint3Ds(CameraSpacePoint[] cameraSpacePoints)
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
                if (isValidCameraPoint(point))
                {
                    point3Ds.Add(new Point3D()
                    {
                        X = point.X,
                        Y = point.Y,
                        Z = point.Z
                    });
                }
            }

            return point3Ds;
        }

        public static Point3D calculateCenterPoint(List<Point3D> pointCloud)
        {
            Point3D center = new Point3D();
            double sumX = 0;
            double sumY = 0;
            double sumZ = 0;
            int size = pointCloud.Count;
            foreach (Point3D point in pointCloud)
            {
                sumX += point.X;
                sumY += point.Y;
                sumZ += point.Z;
            }
            center.X = sumX / size;
            center.Y = sumY / size;
            center.Z = sumZ / size;
            return center;
        }

        public static Point3D sphericalToCartesian(Point3D sphericalPoint)
        {
            double radius = sphericalPoint.X;
            double theta = sphericalPoint.Y;
            double phi = sphericalPoint.Z;
            return new Point3D(
                radius*Math.Sin(theta)*Math.Cos(phi),
                radius*Math.Sin(theta)*Math.Sin(phi),
                radius*Math.Cos(theta)
                );
        }

    }
}
