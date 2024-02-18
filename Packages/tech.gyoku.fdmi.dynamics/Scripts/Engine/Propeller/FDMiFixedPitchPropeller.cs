
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.dynamics
{
    public class FDMiFixedPitchPropeller : FDMiAttribute
    {
        public AnimationCurve Cl_Alpha;
        public AnimationCurve Cd_Alpha;
        public FDMiVector3 AirSpeed;
        public FDMiFloat Rho;
        public FDMiFloat PowerTrainRPM;
        public FDMiFloat OutputTorque;
        public int bladeNum = 2;
        public float bladeRadius = 1.0f;
        public float bladeChord = 0.2f;
        public float bladePitch = 12.0f;
        public float gearRatio = 10f;

        private Vector3[] airSpeed;
        private float[] rho, rpm, torqueOut;
        float r;
        void Start()
        {
            airSpeed = AirSpeed.data;
            rho = Rho.data;
            rpm = PowerTrainRPM.data;
            torqueOut = OutputTorque.data;
            r = bladeRadius * 0.7f;
        }

        private float j;
        private float thrust, torque;
        public float n, alpha, phi;
        public Vector3 Vr, LiftVec, Force;
        void FixedUpdate()
        {
            n = rpm[0] / gearRatio / 60;
            Vr.y = 0f;
            Vr.z = Vector3.Dot(airSpeed[0], transform.forward);
            Vr.x = r * 6.2831853f * n;
            phi = Vector3.SignedAngle(Vr, Vector3.right, Vector3.up);
            alpha = bladePitch - Vector3.SignedAngle(Vr, Vector3.right, Vector3.up);

            LiftVec.z = Vr.x;
            LiftVec.x = -Vr.z;
            LiftVec = LiftVec.normalized;
            // Lift
            Force = Cl_Alpha.Evaluate(alpha) * LiftVec;
            // drag
            Force -= Cd_Alpha.Evaluate(alpha) * Vr.normalized;
            Force *= bladeNum * 0.5f * rho[0] * Vr.magnitude * Vr.magnitude * bladeChord * bladeRadius;
            // thrust output
            thrust = Vector3.Dot(Force, Vector3.forward);
            body.AddRelativeForce(Vector3.Project(Force, Vector3.forward));
            // Torque output
            torque = Vector3.Dot(Force, Vector3.right) * gearRatio;
            torqueOut[0] = Mathf.Lerp(torqueOut[0], torque, 0.5f);
        }

    }
}