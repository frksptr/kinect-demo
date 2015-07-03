using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

            return null;
        }

        public static Point3D projectPoint3DToPlane(Point3D point, Vector<double> planeVectors)
        {
            // ax + by + cz + d = 0
            double a = planeVectors[0];
            double b = planeVectors[1];
            double c = planeVectors[2];
            double d = planeVectors[3];

            Vector<double> planeNormal = new DenseVector(new double[] { a, b, c });

            double x = point.X;
            double y = point.Y;
            double z = point.Z;

            Vector<double> pointVector = new DenseVector(new double[] { x, y, z });

            double distance = (a * x + b * y + z * 0 + d) / Math.Sqrt(a * a + b * b + c * c);

            pointVector.Subtract(planeNormal.Multiply(distance));

            return new Point3D()
            {
                X = pointVector[0],
                Y = pointVector[1],
                Z = pointVector[2]
            };
        }
    }
}
