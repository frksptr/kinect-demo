using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Media3D;
namespace KinectDemo
{
    public class Workspace : INotifyPropertyChanged
    {
        private string name;

        // 2D Vertices defined by user in DepthSpace
        private ObservableCollection<Point> vertices;

        // Vertices in 3D
        public Point3D[] Vertices3D { get; set; }

        // Vertices adjusted to the fitted plane
        public Point3D[] FittedVertices { get; set; }

        public Point3D Center { get; set; }

        private ObservableCollection<Point3D> pointCloud;

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
                this.OnPropertyChanged("Name");
            }
        }

        public ObservableCollection<Point> Vertices
        {
            get
            {
                return this.vertices;
            }
            set
            {
                this.vertices = value;
                this.OnPropertyChanged("Vertices");
            }
        }

        public ObservableCollection<Point3D> PointCloud
        {
            get
            {
                return this.pointCloud;
            }
            set
            {
                this.pointCloud = value;
                this.OnPropertyChanged("PointCloud");
            }
        }

        public Workspace()
        {
            vertices = new ObservableCollection<Point> { new Point(), new Point(), new Point(), new Point() };
            Vertices3D = new Point3D[] { new Point3D(), new Point3D(), new Point3D(), new Point3D() };
            FittedVertices = new Point3D[] { new Point3D(), new Point3D(), new Point3D(), new Point3D() };
        }

        public Workspace(string name, ObservableCollection<Point> points)
        {
            Name = name;
            Vertices = points;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
