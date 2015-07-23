using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectDemoCommon.Model
{
    [Serializable]
    public class NullablePoint3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public NullablePoint3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
