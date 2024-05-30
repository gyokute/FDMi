
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiInputAddon : FDMiBehaviour
    {
        [HideInInspector] public bool isActive = false;
        // public InputButton SelectInputType = InputButton.Grab;
        protected KeyCode triggeredKey = KeyCode.None;
        // protected float[] input = new float[(int)InputButton.Length];
        protected Vector3 handPos, handPrevPos, handStartPos;
        protected Quaternion handAxis, handPrevAxis, handStartAxis;
        protected Vector3 fingerPos, fingerPrevPos, fingerStartPos;
        protected Quaternion fingerAxis, fingerPrevAxis, fingerStartAxis;
        protected FDMiFingerTracker touchFinger, grabFinger;
        protected float[] touchAxis, grabAxis = null;

        public virtual void whileTouch()
        {
            if (!touchFinger) return;
            fingerPrevPos = fingerPos;
            fingerPrevAxis = fingerAxis;
            fingerPos = transform.InverseTransformPoint(touchFinger.fingerPos);
            fingerAxis = Quaternion.Inverse(transform.rotation) * touchFinger.fingerAxis;
        }

        public virtual void whileGrab()
        {
            if (!grabFinger) return;
            handPrevPos = handPos;
            handPrevAxis = handAxis;
            handPos = transform.InverseTransformPoint(grabFinger.handPos);
            handAxis = Quaternion.Inverse(transform.rotation) * grabFinger.handAxis;
        }

        public virtual void OnFingerEnter(FDMiFingerTracker finger)
        {
            touchFinger = finger;
            touchAxis = finger.axisInput;
            fingerStartPos = transform.InverseTransformPoint(finger.fingerPos);
            fingerStartAxis = Quaternion.Inverse(transform.rotation) * finger.fingerAxis;
            fingerPos = fingerStartPos;
            fingerAxis = fingerStartAxis;
        }
        public virtual void OnFingerLeave(FDMiFingerTracker finger)
        {
            if (touchFinger != finger) return;
            touchFinger = null;
            touchAxis = null;
        }
        public virtual void OnGrab(FDMiFingerTracker finger)
        {
            grabFinger = finger;
            grabAxis = finger.axisInput;
            handStartPos = transform.InverseTransformPoint(finger.handPos);
            handStartAxis = Quaternion.Inverse(transform.rotation) * finger.handAxis;
            handPos = handStartPos;
            handAxis = handStartAxis;
        }
        public virtual void OnRelease(FDMiFingerTracker finger)
        {
            grabAxis = null;
        }
    }
}
