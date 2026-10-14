using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public class FDMiGroundSpeed : FDMiBehaviour
    {
        public string GroundSpeedPath = "GroundSpeed";

        [FDMiDataPath(nameof(GroundSpeedPath))]
        public FDMiFloat GroundSpeed;

        public string VelocityPath = "Velocity";

        [FDMiDataPath(nameof(VelocityPath))]
        public FDMiVector3 Velocity;

        public string RotationPath = "Rotation";

        [FDMiDataPath(nameof(RotationPath))]
        public FDMiQuaternion Rotation;

        void Start()
        {
            _vel = Velocity.data;
            _rot = Rotation.data;
        }

        void Update()
        {
            GroundSpeed.Data = (Quaternion.Inverse(_rot[0]) * Vector3.ProjectOnPlane(_vel[0], Vector3.up)).z;
        }

        Vector3[] _vel;
        Quaternion[] _rot;
    }
}
