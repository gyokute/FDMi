
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
        public FDMiFloat Alpha, Beta, IAS, TAS, Mach, Rho, SonicSpeed, IASAcc, TASAcc, VerticalSpeed;
        private Vector3[] airSpeed, wind, vel;
        private Quaternion[] rot;
        private float[] alpha, beta, ias, tas, mach, sat, rho, sonic, iasAcc, tasAcc;
        private float omega;
        [SerializeField] float a = 1.0f;
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
            iasAcc = IASAcc.data;
            tasAcc = TASAcc.data;
            omega = 2 * Mathf.PI * a;
            omega = omega / (omega + 1);
        }

        void FixedUpdate()
        {
            if (!isOwner) return;
            airSpeed[0] = body.velocity - Quaternion.Inverse(rot[0]) * wind[0];
        }
        float pIAS, pTAS;
        void Update()
        {
            if (!isOwner)
            {
                airSpeed[0] = Quaternion.Inverse(rot[0]) * (vel[0] - wind[0]);
            }
            pIAS = ias[0];
            pTAS = tas[0];
            Alpha.Data = -Vector3.SignedAngle(Vector3.ProjectOnPlane(airSpeed[0], Vector3.right), Vector3.forward, Vector3.right);
            Beta.Data = Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(airSpeed[0], Vector3.up), Vector3.up);

            TAS.Data = airSpeed[0].z;
            IAS.Data = tas[0] * Mathf.Sqrt(rho[0] / 1.2f);
            Mach.Data = tas[0] / Mathf.Max(sonic[0], 0.0001f);
            // tat[0] = (1f + 0.2f * mach[0] * mach[0]) / sat[0];
            IASAcc.Data = Mathf.Lerp((ias[0] - pIAS) / Time.deltaTime, iasAcc[0], omega);
            TASAcc.Data = Mathf.Lerp((tas[0] - pIAS) / Time.deltaTime, tasAcc[0], omega);
            VerticalSpeed.Data = vel[0].y;
        }
    }
}