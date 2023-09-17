
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.aerodynamics
{
    public class FDMiWing : FDMiAttribute
    {
        public AnimationCurve Cl_Alpha;
        public AnimationCurve Cd_Alpha;
        public AnimationCurve Cl_Mach;
        public AnimationCurve Cd_Mach;
        public FDMiWingSection SectionL, SectionR;
        public FDMiVector3 AirSpeed;
        public FDMiFloat Rho, Mach;
        public FDMiWing[] affectWing;
        public float Lift, Drag, Gamma;
        [HideInInspector] public float[] rho, mach;
        [HideInInspector] public Vector3[] airspeed, Qij;
        [HideInInspector] public Vector3 spL, spR, spRL;
        [HideInInspector] public Vector3 cpAirVec, Force, Moment;

        [HideInInspector] public Vector3 spanNormal, planfNormal, chordNormal, controlPoint, Vni;
        [HideInInspector] public float cpChordLength, cpSpanLength, cpArea, alpha, chordAirSpeed;
        private Quaternion invRot;
        private int gammaLength = 0;

        void Start()
        {
            airspeed = AirSpeed.data;
            rho = Rho.data;
            mach = Mach.data;

            Transform bt = body.transform;
        }
        void FixedUpdate()
        {
            Vni = Vector3.zero;
            invRot = Quaternion.Inverse(body.rotation);
            Quaternion rot = body.rotation * transform.rotation;
            // spanNormal = rot * Vector3.right;
            chordNormal = rot * -Vector3.forward;

            cpAirVec = -airspeed[0] - Vector3.Cross(invRot * body.angularVelocity, controlPoint);

            for (int j = 0; j < affectWing.Length; j++)
            {
                Vni += Qij[j] * affectWing[j].Gamma;
            }
            cpAirVec += Vni;
            cpAirVec = Vector3.ProjectOnPlane(cpAirVec, spanNormal);

            alpha = Vector3.SignedAngle(cpAirVec, chordNormal, spanNormal);
            float chordAirSpeed = cpAirVec.magnitude;

            float C = 0.5f * chordAirSpeed;
            float Cl = Cl_Alpha.Evaluate(alpha) * Cl_Mach.Evaluate(mach[0]);
            float nGamma = C * Cl * cpChordLength;
            C *= rho[0] * chordAirSpeed * cpChordLength * cpSpanLength * Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * alpha));

            Lift = C * Cl;
            Drag = C * Cd_Alpha.Evaluate(alpha) * Cd_Mach.Evaluate(mach[0]);

            Vector3 DNormal = Vector3.Normalize(cpAirVec);
            Vector3 LNormal = Vector3.Cross(DNormal, spanNormal);
            Force = Lift * LNormal + Drag * DNormal + rho[0] * nGamma * cpSpanLength * Vni;
            Moment = Vector3.Cross(controlPoint - body.centerOfMass, Force);
            Gamma = Mathf.Lerp(Gamma, nGamma, Time.fixedDeltaTime * 10f);
            body.AddRelativeForce(Force);
            body.AddRelativeTorque(Moment);
            DebugForce();
        }

        void DebugForce()
        {
            Transform bt = body.transform;
            Vector3 worldcp = bt.TransformPoint(controlPoint);
            Vector3 DNormal = bt.TransformDirection(Vector3.Normalize(cpAirVec));
            Vector3 LNormal = bt.TransformDirection(Vector3.Cross(DNormal, spanNormal));
            Debug.DrawRay(worldcp, bt.TransformDirection(Vni / 10), Color.cyan, 0f, false);
            Debug.DrawRay(worldcp, bt.TransformDirection(cpAirVec / 10), Color.yellow, 0f, false);
            Debug.DrawRay(worldcp, Lift / 0.098f / body.mass * LNormal, Color.green, 0f, false);
            Debug.DrawRay(worldcp, Drag / 0.098f / body.mass * DNormal, Color.red, 0f, false);
            // Debug.DrawRay(worldcp, Vni, Color.yellow, 0f, false);
            // Debug.DrawRay(worldcp, transform.right * alpha[i], Color.magenta, 0f, false);
        }
    }
}
