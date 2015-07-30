using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using KinectDemoCommon.Model;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;
using Microsoft.Kinect;

namespace KinectDemoCommon.Util
{
    public class GeometryHelper
    {
        public static bool IsNumber(float num)
        {
            return !float.IsNaN(num) && !float.IsInfinity(num);
        }

        public static bool IsValidCameraPoint(CameraSpacePoint point)
        {
            return IsNumber(point.X) && IsNumber(point.Y) && IsNumber(point.Z);
        }

        public static double[] Normalize(double[] points)
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

        public static Vector<double> FitPlaneToPoints(Point3D[] points)
        {
            if (points.Length == 0)
            {
                return null;
            }
            double[,] pointArrays = Converter.Point3DToPointArrays(points);

            Matrix<double> pointMatrix = Matrix<double>.Build.DenseOfArray(pointArrays);

            Svd<double> svdDecomp = pointMatrix.Svd(true);

            Matrix<double> vt = svdDecomp.VT;

            Vector<double> solution = vt.Row(vt.RowCount - 1);

            return solution;

        }


        public static Point3D[] ProjectPoints3DToPlane(Point3D[] points, Vector<double> planeVectors)
        {
            Point3D[] projectedPoints = new Point3D[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                projectedPoints[i] = ProjectPoint3DToPlane(points[i], planeVectors);
            }

            return projectedPoints;
        }

        public static Point3D ProjectPoint3DToPlane(Point3D point, Vector<double> planeVectors)
        {

            double distance = CalculatePointPlaneDistance(point, planeVectors);

            double a = planeVectors[0];
            double b = planeVectors[1];
            double c = planeVectors[2];

            Vector<double> planeNormal = new DenseVector(new[] { a, b, c });

            double x = point.X;
            double y = point.Y;
            double z = point.Z;

            Vector<double> pointVector = new DenseVector(new[] { x, y, z });

            pointVector.Subtract(planeNormal.Multiply(distance));

            return new Point3D
            {
                X = pointVector[0],
                Y = pointVector[1],
                Z = pointVector[2]
            };
        }

        public static double CalculatePointPlaneDistance(Point3D point, Vector<double> planeVector)
        {
            // ax + by + cz + d = 0
            double a = planeVector[0];
            double b = planeVector[1];
            double c = planeVector[2];
            double d = planeVector[3];

            double x = point.X;
            double y = point.Y;
            double z = point.Z;

            double distance = (a * x + b * y + c * z + d) / Math.Sqrt(a * a + b * b + c * c);
            return distance;
        }

        public static bool InsidePolygon(Point[] polyVertices, Point point)
        {
            int i, j;
            bool c = false;
            int nvert = polyVertices.Length;
            for (i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                if (((polyVertices[i].Y > point.Y) != (polyVertices[j].Y > point.Y)) &&
                 (point.X < (polyVertices[j].X - polyVertices[i].X) * (point.Y - polyVertices[i].Y) / (polyVertices[j].Y - polyVertices[i].Y) + polyVertices[i].X))
                    c = !c;
            }
            return c;
        }

        // Checks if 3D points projected to the plane of the polygon are inside the polygon
        public static bool InsidePolygon3D(Point3D[] polyVertices, Point3D projectedPoint)
        {
            PointCollection points = new PointCollection();
            foreach (Point3D point in polyVertices)
            {
                points.Add(new Point(point.X, point.Y));
            }
            return InsidePolygon(points.ToArray(), new Point(projectedPoint.X, projectedPoint.Y));
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
            Vector3D normal = CalculateTraingleNormal(p0, p1, p2);
            mymesh.Normals.Add(normal);
            mymesh.Normals.Add(normal);
            mymesh.Normals.Add(normal);
            Material material = new DiffuseMaterial(
                new SolidColorBrush(color) { Opacity = 0.5 });
            GeometryModel3D model = new GeometryModel3D(
                mymesh, material);
            Model3DGroup @group = new Model3DGroup();
            @group.Children.Add(model);
            return @group;
        }

        public static Vector3D CalculateTraingleNormal(Point3D p0, Point3D p1, Point3D p2)
        {
            Vector3D v0 = new Vector3D(
                p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3D v1 = new Vector3D(
                p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            return Vector3D.CrossProduct(v0, v1);
        }

        public static Point3D CalculateCenterPoint(NullablePoint3D[] pointCloud)
        {
            List<Point3D> points = new List<Point3D>();
            foreach (NullablePoint3D point in pointCloud)
            {
                points.Add(new Point3D(point.X, point.Y, point.Z));
            }
            return CalculateCenterPoint(points);
        }

        public static Point3D CalculateCenterPoint(List<Point3D> pointCloud)
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

        public static Point3D SphericalToCartesian(Point3D sphericalPoint)
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

        public static TransformationAndRotation GetTransformationAndRotation(NullablePoint3D[] pointCloud1, NullablePoint3D[] pointCloud2)
        {
            var center1 = CalculateCenterPoint(pointCloud1);
            var center2 = CalculateCenterPoint(pointCloud2);
            Vector<double> centerV1 = DenseVector.OfArray(new[] { center1.X, center1.Y, center1.Z });
            Vector<double> centerV2 = DenseVector.OfArray(new[] { center2.X, center2.Y, center2.Z });

            Matrix<double> H = Matrix<double>.Build.Dense(3, 3, 0);
            for (int i = 0; i < pointCloud1.Length; i++)
            {
                var p1 = pointCloud1[i];
                var p2 = pointCloud2[i];
                Vector<double> vec1 = DenseVector.OfArray(new[] { p1.X, p1.Y, p1.Z });
                Vector<double> vec2 = DenseVector.OfArray(new[] { p2.X, p2.Y, p2.Z });
                var a = DenseVector.OuterProduct(vec1.Subtract(centerV1), vec2.Subtract(centerV2));
                H += a;
            }

            Svd<double> svdDecomp = H.Svd(true);
            var V = svdDecomp.VT.Transpose();
            var U = svdDecomp.U;
            var R = V.Multiply(U.Transpose());


            if (R.Determinant() < 0)
            {
                R.Column(2).Multiply(-1);
            }

            var t = -R * centerV1 + centerV2;
            return new TransformationAndRotation(t, R);

        }

        public class TransformationAndRotation
        {
            public Vector<double> T {get;set;}
            public Matrix<double> R { get; set; }
            public TransformationAndRotation(Vector<double> t, Matrix<double> r)
            {
                T = t;
                R = r;
            }
        }

    }
}
