
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
        public FDMiVector3 Position, KmPosition, Velocity;
        public FDMiQuaternion Rotation;
        Vector3[] pos, kmPos, vel, attitude;
        Quaternion[] rot;
        void Start()
        {
            pos = Position.data;
            kmPos = KmPosition.data;
            vel = Velocity.data;
            rot = Rotation.data;
        }

        void Update()
        {
            pos[0] = refPoint._position;
            kmPos[0] = refPoint.syncedKmPos;
            vel[0] = refPoint._velocity;
            rot[0] = refPoint._rotation;
        }
    }
}