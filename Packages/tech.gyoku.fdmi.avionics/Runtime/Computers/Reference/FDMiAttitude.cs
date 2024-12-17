
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public class FDMiAttitude : FDMiBehaviour
    {
        public FDMiFloat Pitch, Roll, HDG;
        public FDMiQuaternion Rotation;
        public FDMiFloat MagneticDeclination;
        Quaternion[] rot;
        float[] hdg, pitch, roll, mag;
        void Start()
        {
            rot = Rotation.data;
            hdg = HDG.data;
            pitch = Pitch.data;
            roll = Roll.data;
            mag = MagneticDeclination.data;
        }
        Vector3 fwd, up, right;
        void Update()
        {
            fwd = rot[0] * Vector3.forward;
            right = rot[0] * Vector3.right;
            up = rot[0] * Vector3.up;
            hdg[0] = Mathf.Repeat(Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(fwd, Vector3.up), Vector3.up) + mag[0], 360f);
            pitch[0] = -Vector3.SignedAngle(Vector3.Cross(right, Vector3.up), fwd, right);
            roll[0] = -Vector3.SignedAngle(Vector3.Cross(Vector3.up, fwd), right, fwd);
        }
    }
}