using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using KinectDemoCommon.Messages;
using KinectDemoCommon.Model;
using KinectDemoCommon.Util;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;

namespace KinectDemoClient
{

    public delegate void WorkspaceActivatedEventHandler(WorkspaceMessage message);

    class WorkspaceChecker
    {
        public List<Workspace> WorkspaceList { get; set; }

        private const double DistanceTolerance = 0.5;

        public event WorkspaceActivatedEventHandler WorkspaceActivated;

        public void CheckActiveWorkspace(CameraSpacePoint[] handPositions)
        {
            foreach (Workspace workspace in WorkspaceList)
            {
                Point3D[] vertices = workspace.FittedVertices;

                Polygon poly = new Polygon();
                poly.Points = new PointCollection
                { 
                    new Point(vertices[0].X, vertices[0].Y),
                    new Point(vertices[1].X, vertices[1].Y),
                    new Point(vertices[2].X, vertices[2].Y),
                    new Point(vertices[3].X, vertices[3].Y) };

                bool isActive = false;
                foreach (CameraSpacePoint handPosition in handPositions)
                {
                    Vector<double> handVector = new DenseVector(new double[] {
                        handPosition.X,
                        handPosition.Y,
                        handPosition.Z
                    });

                    if (GeometryHelper.InsidePolygon3D(vertices.ToArray(), GeometryHelper.ProjectPoint3DToPlane(GeometryHelper.CameraSpacePointToPoint3D(handPosition), workspace.PlaneVector)))
                    {
                        double distance = GeometryHelper.CalculatePointPlaneDistance(GeometryHelper.CameraSpacePointToPoint3D(handPosition), workspace.PlaneVector);

                        if (Math.Abs(distance) <= DistanceTolerance)
                        {
                            isActive = true;
                            if (WorkspaceActivated != null)
                            {
                                WorkspaceActivated(new WorkspaceMessage()
                                {
                                    Vertices = workspace.Vertices.ToArray()
                                });
                            }
                        }
                    }
                }
                workspace.Active = isActive;
            }
        }

        //void kinectStreamer_BodyDataReady(object sender, KinectStreamerEventArgs e)
        //{
        //Bodies = e.Bodies;

        //using (DrawingContext dc = drawingGroup.Open())
        //{
        //    dc.DrawImage(ColorImageSource, new Rect(0.0, 0.0, displayWidth, displayHeight));


        //    int penIndex = 0;
        //    foreach (Body body in Bodies)
        //    {
        //        Pen drawPen = bodyColors[penIndex++];

        //        if (body.IsTracked)
        //        {
        //            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

        //            // convert the joint points to depth (display) space
        //            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

        //            foreach (JointType jointType in joints.Keys)
        //            {
        //                // sometimes the depth(Z) of an inferred joint may show as negative
        //                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
        //                CameraSpacePoint position = joints[jointType].Position;
        //                if (position.Z < 0)
        //                {
        //                    position.Z = InferredZPositionClamp;
        //                }
        //                ColorSpacePoint colorSpacePoint = kinectStreamer.CoordinateMapper.MapCameraPointToColorSpace(position);
        //                jointPoints[jointType] = new Point(colorSpacePoint.X, colorSpacePoint.Y);
        //            }

        //            DrawBody(joints, jointPoints, dc, drawPen);

        //            DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
        //            DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);

        //            CheckActiveWorkspace(new CameraSpacePoint[]{
        //                body.Joints[JointType.HandRight].Position,
        //                body.Joints[JointType.HandLeft].Position});
        //        }
        //    }
        //    // prevent drawing outside of our render area
        //    drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, displayWidth, displayHeight));
        //    DrawWorksapces(dc);
        //    OnPropertyChanged("ColorImageSource");
        //    OnPropertyChanged("ImageSource");
        //}
        //}
    }
}
