
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
        float[] mag;
        void Start()
        {
            rot = Rotation.data;
            mag = MagneticDeclination.data;
        }
        Vector3 fwd, up, right;
        void Update()
        {
            fwd = rot[0] * Vector3.forward;
            right = rot[0] * Vector3.right;
            up = rot[0] * Vector3.up;
            HDG.Data = Mathf.Repeat(Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(fwd, Vector3.up), Vector3.up) + mag[0], 360f);
            Pitch.Data = -Vector3.SignedAngle(Vector3.Cross(right, Vector3.up), fwd, right);
            Roll.Data = -Vector3.SignedAngle(Vector3.Cross(Vector3.up, fwd), right, fwd);
        }
    }
}