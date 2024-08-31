
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public class FDMiGroundSpeed : FDMiBehaviour
    {
        public FDMiFloat GroundSpeed;
        public FDMiVector3 Velocity;
        public FDMiQuaternion Rotation;
        Vector3[] vel;
        private Quaternion[] rot;
        float[] gs;

        void Start()
        {
            gs = GroundSpeed.data;
            vel = Velocity.data;
            rot = Rotation.data;
        }
        Vector3 v;
        void Update()
        {
            v = Quaternion.Inverse(rot[0]) * Vector3.ProjectOnPlane(vel[0], Vector3.up);
            gs[0] = v.z;
        }
    }
}