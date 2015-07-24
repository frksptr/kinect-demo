using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectDemoCommon.Util;

namespace KinectDemoCommon.Model
{
    [Serializable]
    public class SerializableBody
    {
        //
        // Summary:
        //     Gets the confidence of the body's left hand state.
        public TrackingConfidence HandLeftConfidence { get; set; }
        //
        // Summary:
        //     Gets the status of the body's left hand state.
        public HandState HandLeftState { get; set; }
        //
        // Summary:
        //     Gets the confidence of the body's right hand state.
        public TrackingConfidence HandRightConfidence { get; set; }
        //
        // Summary:
        //     Gets the status of the body's right hand state.
        public HandState HandRightState { get; set; }
        //
        // Summary:
        //     Gets whether or not the body is restricted.
        public bool IsRestricted { get; set; }
        //
        // Summary:
        //     Gets whether or not the body is tracked.
        public bool IsTracked { get; set; }
        //
        // Summary:
        //     Gets the joint orientations of the body.
        public SerializableDictionary<JointType, JointOrientation> JointOrientations { get; set; }
        //
        // Summary:
        //     Gets the joint positions of the body.
        public SerializableDictionary<JointType, Joint> Joints { get; set; }
        //
        // Summary:
        //     Gets the lean vector of the body.
        public PointF Lean { get; set; }
        //
        // Summary:
        //     Gets the tracking state for the body lean.
        public TrackingState LeanTrackingState { get; set; }
        //
        // Summary:
        //     Gets the tracking ID for the body.
        public ulong TrackingId { get; set; }

        public SerializableBody(Body body) {
            HandLeftConfidence = body.HandLeftConfidence;
            HandLeftState = body.HandLeftState;
            HandRightConfidence = body.HandRightConfidence;
            HandRightState = body.HandRightState;
            IsRestricted = body.IsRestricted;
            IsTracked = body.IsTracked;
            JointOrientations = new SerializableDictionaryBuilder<JointType,JointOrientation>().Build(body.JointOrientations);
            Joints = new SerializableDictionaryBuilder<JointType, Joint>().Build(body.Joints);
            Lean = body.Lean;
            LeanTrackingState = body.LeanTrackingState;
            TrackingId = body.TrackingId;
        }


    }
}
