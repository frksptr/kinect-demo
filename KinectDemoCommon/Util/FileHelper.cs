using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Media3D;
using KinectDemoCommon.Model;

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
                sw.WriteLine(header);
                foreach (Point3D point in points)
                {
                    //  TODO: use locale specific formatting
                    sw.WriteLine((point.X + " " + point.Y + " " + point.Z).Replace(",", "."));
                }
            }
        }

        public static List<NullablePoint3D> ParsePCD(string path)
        {
            List<NullablePoint3D> pointCloud = new List<NullablePoint3D>();
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
            {
                //  TODO: equals last line of header
                while (sr.ReadLine().Equals("DATA ascii"))
                {

                }
                string coords;
                while ((coords = sr.ReadLine()) != null)
                {
                    string[] coordArray = coords.Replace(".",",").Split(' ');
                    if (coordArray.Length != 3)
                    {
                        Exception ex = new Exception("Invalid format!");
                    }
                    else
                    {
                        pointCloud.Add(new NullablePoint3D(
                            double.Parse(coordArray[0]),
                            double.Parse(coordArray[1]),
                            double.Parse(coordArray[2])
                            ));
                    }

                }
                return pointCloud;
            }
        }

        public static void WritePointCloud(List<Point3D> points, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                foreach (Point3D point in points)
                {
                    //  TODO: use locale specific formatting
                    sw.WriteLine((point.X + " " + point.Y + " " + point.Z).Replace(",", "."));
                }
            }
        }
    }
}
