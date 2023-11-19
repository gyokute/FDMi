
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public class FDMiGroundSpeed : FDMiBehaviour
    {
        [SerializeField] FDMiFloat GroundSpeed;
        [SerializeField] FDMiVector3 Velocity;
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
            v = Quaternion.Inverse(rot[0]) * (vel[0]);
            gs[0] = v.z * 1.94384f;
        }
    }
}