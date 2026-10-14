using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public class FDMiVerticalSpeed : FDMiBehaviour
    {
        public string VerticalSpeedPath = "VerticalSpeed";

        [FDMiDataPath(nameof(VerticalSpeedPath))]
        public FDMiFloat VerticalSpeed;

        public string VelocityPath = "Velocity";

        [FDMiDataPath(nameof(VelocityPath))]
        public FDMiVector3 Velocity;

        void Start()
        {
            _vel = Velocity.data;
        }

        void Update()
        {
            VerticalSpeed.Data = _vel[0].y;
        }

        Vector3[] _vel;
    }
}
