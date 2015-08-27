using System.Collections.Generic;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;

namespace KinectDemoSGL
{
    // Singleton
    class CalibrationProcessor
    {
        private Matrix<double> rotation;
        private Vector<double> translation;
        public GeometryHelper.Transformation Transformation { get; set; }

        private static CalibrationProcessor calibrationProcessor;

        public static CalibrationProcessor Instance
        {
            get { return calibrationProcessor ?? (calibrationProcessor = new CalibrationProcessor()); }
        }
        private CalibrationProcessor() { }

        public GeometryHelper.Transformation CalculateTransformationFromAtoB(List<SerializableBody> datasetA, List<SerializableBody> datasetB)
        {
            var setA = GetPointsFromBodies(datasetA);
            var setB = GetPointsFromBodies(datasetB);

            Transformation = GeometryHelper.GetTransformation(setA, setB);

            rotation = Transformation.R;
            translation = Transformation.T;
            return Transformation;
        }

        public List<NullablePoint3D> GetCloudATransformedToCloudB(List<NullablePoint3D> cloudA, List<NullablePoint3D> cloudB)
        {
            DataStore dataStore = DataStore.Instance;
            List<NullablePoint3D> transformedPointCloudList = new List<NullablePoint3D>();
            foreach (NullablePoint3D point in cloudA)
            {
                if (point != null)
                {
                    var pointVector = DenseVector.OfArray(new[] { point.X, point.Y, point.Z });
                    var rottranv = (rotation * pointVector) + translation;
                    transformedPointCloudList.Add(new NullablePoint3D(rottranv[0], rottranv[1], rottranv[2]));
                }
            }

            return transformedPointCloudList;
        }

        public List<NullablePoint3D> MergeClouds(List<NullablePoint3D> cloudA, List<NullablePoint3D> cloudB)
        {
            var transformedPointCloud = GetCloudATransformedToCloudB(cloudA, cloudB);
            var mergedCloud = new List<NullablePoint3D>();
            mergedCloud.AddRange(transformedPointCloud);
            mergedCloud.AddRange(cloudB);
            return mergedCloud;
        }

        private List<NullablePoint3D> GetPointsFromBodies(List<SerializableBody> bodies)
        {
            List<NullablePoint3D> points = new List<NullablePoint3D>();
            foreach (SerializableBody body in bodies)
            {
                foreach (var dictionaryItem in body.Joints.Items)
                {
                    CameraSpacePoint position = dictionaryItem.Value.Position;
                    points.Add(new NullablePoint3D(position.X, position.Y, position.Z));
                }
            }
            return points;
        }
    }
}
