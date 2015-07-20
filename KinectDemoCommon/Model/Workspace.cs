using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Media3D;
using MathNet.Numerics.LinearAlgebra;

namespace KinectDemoCommon.Model
{
    [Serializable]
    public class Workspace : INotifyPropertyChanged
    {
        private string id;

        public string ID { get { return id; } }

        private string name;

        // 2D Vertices defined by user in DepthSpace with coordinates normed to (0,1)
        private ObservableCollection<Point> vertices;

        // Vertices in 3D
        public Point3D[] Vertices3D { get; set; }

        // Vertices adjusted to the fitted plane
        public Point3D[] FittedVertices { get; set; }

        public Vector<double> PlaneVector { get; set; }

        public Point3D Center { get; set; }

        private Point3D[] pointCloud;

        public bool Active { get; set; }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        public ObservableCollection<Point> Vertices
        {
            get
            {
                return vertices;
            }
            set
            {
                vertices = value;
                OnPropertyChanged("Vertices");
            }
        }

        public Point3D[] PointCloud
        {
            get
            {
                return pointCloud;
            }
            set
            {
                pointCloud = value;
                OnPropertyChanged("PointCloud");
            }
        }
        public Workspace()
        {
            id = Guid.NewGuid().ToString();
            vertices = new ObservableCollection<Point> { new Point(), new Point(), new Point(), new Point() };
            Vertices3D = new[] { new Point3D(), new Point3D(), new Point3D(), new Point3D() };
            FittedVertices = new[]{ new Point3D(), new Point3D(), new Point3D(), new Point3D() };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
