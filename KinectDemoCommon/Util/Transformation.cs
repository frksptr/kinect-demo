using MathNet.Numerics.LinearAlgebra;

namespace KinectDemoCommon.Util
{
    public class Transformation
    {
        public Vector<double> T { get; set; }
        public Matrix<double> R { get; set; }
        public Transformation(Vector<double> t, Matrix<double> r)
        {
            T = t;
            R = r;
        }
    }
}
