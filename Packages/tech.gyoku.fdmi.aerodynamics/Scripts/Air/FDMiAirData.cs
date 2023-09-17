
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.aerodynamics
{

    public class FDMiAirData : FDMiAttribute
    {
        public FDMiVector3 AirSpeed, Wind, Velocity;
        public FDMiQuaternion Rotation;
        public FDMiFloat Alpha, Beta, IAS, TAS, Mach, Rho, SonicSpeed;
        private Vector3[] airSpeed, wind, vel;
        private Quaternion[] rot;
        private float[] alpha, beta, ias, tas, mach, sat, rho, sonic;

        void Start()
        {
            airSpeed = AirSpeed.data;
            wind = Wind.data;
            vel = Velocity.data;
            rot = Rotation.data;
            alpha = Alpha.data;
            beta = Beta.data;
            ias = IAS.data;
            tas = TAS.data;
            mach = Mach.data;
            rho = Rho.data;
            sonic = SonicSpeed.data;
        }

        void FixedUpdate()
        {
            if (!isOwner) return;
            airSpeed[0] = Quaternion.Inverse(rot[0]) * (body.velocity - wind[0]);
        }
        void Update()
        {
            if (!isOwner)
            {
                airSpeed[0] = Quaternion.Inverse(rot[0]) * (vel[0] - wind[0]);
            }
            alpha[0] = -Vector3.SignedAngle(Vector3.ProjectOnPlane(airSpeed[0], Vector3.right), Vector3.forward, Vector3.right);
            beta[0] = Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(airSpeed[0], Vector3.up), Vector3.up);

            tas[0] = airSpeed[0].z;
            ias[0] = tas[0] * Mathf.Sqrt(rho[0] / 1.2f);
            mach[0] = tas[0] / sonic[0];
            // tat[0] = (1f + 0.2f * mach[0] * mach[0]) / sat[0];
        }
    }
}