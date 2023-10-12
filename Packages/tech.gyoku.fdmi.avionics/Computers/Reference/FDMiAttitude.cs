
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public class FDMiAttitude : FDMiAttribute
    {
        [SerializeField] FDMiFloat Pitch, Roll, HDG;
        [SerializeField] FDMiQuaternion Rotation;
        float[] pitch, roll, hdg;
        Quaternion[] rot;
        void Start()
        {
            pitch = Pitch.data;
            roll = Roll.data;
            hdg = HDG.data;
            rot = Rotation.data;
        }
        Vector3 fwd, up, right;
        void Update()
        {
            fwd = rot[0] * Vector3.forward;
            right = rot[0] * Vector3.right;
            up = rot[0] * Vector3.up;
            hdg[0] = Mathf.Repeat(Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(fwd, Vector3.up), Vector3.up), 360f);
            pitch[0] = -Vector3.SignedAngle(Vector3.Cross(right, Vector3.up), fwd, right);
            roll[0] = -Vector3.SignedAngle(Vector3.Cross(Vector3.up, fwd), right, fwd);
        }
    }
}