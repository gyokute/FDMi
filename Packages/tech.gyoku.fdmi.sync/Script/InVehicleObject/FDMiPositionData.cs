
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiPositionData : FDMiAttribute
    {
        public FDMiReferencePoint refPoint;
        public FDMiVector3 Position, KmPosition, Velocity, Attitude;
        public FDMiQuaternion Rotation;
        Vector3[] pos, kmPos, vel, attitude;
        Quaternion[] rot;
        void Start()
        {
            pos = Position.data;
            kmPos = KmPosition.data;
            vel = Velocity.data;
            attitude = Attitude.data;
            rot = Rotation.data;
        }

        void FixedUpdate()
        {
            pos[0] = refPoint._position;
            kmPos[0] = refPoint._kmPosition;
            vel[0] = refPoint.velocity;
            rot[0] = refPoint._direction * body.rotation;
            attitude[0] = rot[0].eulerAngles;

        }
    }
}