using KinectDemoCommon.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace KinectDemoCommon.Util
{
    public class FileHelper
    {
        private static string pcdHeader =
            @"# .PCD v.7 - Point Cloud Data file format
VERSION 0.7
FIELDS x y z
SIZE 4 4 4
TYPE F F F
COUNT 1 1 1
WIDTH numberofpoints
HEIGHT 1
VIEWPOINT 0 0 0 1 0 0 0
POINTS numberofpoints
DATA ascii";

        public static void WritePCD(List<Point3D> points, string path)
        {
            string header = pcdHeader.Replace("numberofpoints", points.Count.ToString());

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write(header);
                sw.Write('\n');
                foreach (Point3D point in points)
                {
                    sw.WriteLine((point.X + " " + point.Y + " " + point.Z).Replace(",","."));
                }
            }
        }
    }
}
