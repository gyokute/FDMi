using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    public class FDMiPositionData : FDMiBehaviour
    {
        public FDMiReferencePoint refPoint;
        public FDMiVector3 Position,
            KmPosition,
            Velocity;
        public FDMiQuaternion Rotation;
        Vector3[] pos,
            kmPos,
            vel,
            attitude;
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
            kmPos[0] = (FDMiReferencePoint.MmToM(refPoint._posMm) + refPoint._chunkPosition * FDMiReferencePoint.M_PER_CHUNK) * 0.001f;
            vel[0] = refPoint._velocity;
            rot[0] = refPoint._rotation;
        }
    }
}
