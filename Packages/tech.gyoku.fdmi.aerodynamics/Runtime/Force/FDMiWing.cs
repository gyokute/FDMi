﻿
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
        public FDMiFloat Rho, SonicSpeed;
        public FDMiWing[] affectWing;
        public float Lift, Drag, Gamma;
        [HideInInspector] public float[] rho, sonicSpeed;
        [HideInInspector] public Vector3[] airspeed, Qij;
        [HideInInspector] public Vector3 spL, spR, spRL;
        [HideInInspector] public Vector3 cpAirVec, Force, Moment;

        [HideInInspector] public Vector3 spanNormal, planfNormal, chordNormal, controlPoint, Vni;
        [HideInInspector] public float cpChordLength, cpSpanLength, cpArea, alpha, chordAirSpeed;
        private Quaternion invRot, iniRot;
        private int gammaLength = 0;
        private float cosAlpha;
        Vector3 rNormal;

        void Start()
        {
            airspeed = AirSpeed.data;
            rho = Rho.data;
            sonicSpeed = SonicSpeed.data;
            iniRot = Quaternion.Inverse(Quaternion.Inverse(body.rotation) * transform.rotation);
        }
        void FixedUpdate()
        {
            if (!isInit) return;
            Vni = Vector3.zero;
            invRot = Quaternion.Inverse(body.rotation);
            // rNormal = iniRot * invRot * transform.rotation * chordNormal;
            rNormal = -transform.forward;
            spanNormal = -transform.right;

            cpAirVec = -airspeed[0] - Vector3.Cross(invRot * body.angularVelocity, controlPoint);

            for (int j = 0; j < affectWing.Length; j++)
            {
                Vni += Qij[j] * affectWing[j].Gamma;
            }
            cpAirVec += Vni;
            cpAirVec = Vector3.ProjectOnPlane(cpAirVec, spanNormal);

            alpha = Vector3.SignedAngle(cpAirVec, rNormal, spanNormal);
            cosAlpha = Mathf.Max(Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * alpha)), 0.1f);
            chordAirSpeed = cpAirVec.magnitude;
            float mach = chordAirSpeed / sonicSpeed[0];

            float C = 0.5f * chordAirSpeed;
            float Cl = Cl_Alpha.Evaluate(alpha) * Cl_Mach.Evaluate(mach);
            float nGamma = C * Cl * cpChordLength;
            C *= rho[0] * chordAirSpeed * cpChordLength * cpSpanLength;

            Lift = C * Cl * cosAlpha;
            Drag = C * Cd_Alpha.Evaluate(alpha) * Cd_Mach.Evaluate(mach) * cosAlpha;

            Vector3 DNormal = Vector3.Normalize(cpAirVec);
            Vector3 LNormal = Vector3.Cross(DNormal, spanNormal);
            Force = Lift * LNormal + Drag * DNormal + rho[0] * nGamma * cpSpanLength * Vni;
            // Force = Lift * LNormal + Drag * DNormal;
            // Moment = Vector3.Cross(controlPoint - body.centerOfMass, Force);
            Gamma = Mathf.Lerp(Gamma, nGamma, 0.1f);
            // body.AddRelativeForce(Force);
            // body.AddRelativeTorque(Moment);
            body.AddForceAtPosition(Force, body.position + controlPoint);
#if UNITY_EDITOR
            DebugForce();
#endif
        }

        void DebugForce()
        {
            Transform bt = body.transform;
            Vector3 worldcp = bt.TransformPoint(controlPoint);
            Vector3 DNormal = bt.TransformDirection(Vector3.Normalize(cpAirVec));
            Vector3 LNormal = bt.TransformDirection(Vector3.Cross(DNormal, spanNormal));
            Debug.DrawRay(worldcp, rNormal * 3, Color.cyan, 0f, false);
            Debug.DrawRay(worldcp, bt.TransformDirection(Vni / 10), Color.cyan, 0f, false);
            Debug.DrawRay(worldcp, bt.TransformDirection(cpAirVec / 10), Color.yellow, 0f, false);
            Debug.DrawRay(worldcp, Lift / 0.098f / body.mass * LNormal, Color.green, 0f, false);
            Debug.DrawRay(worldcp, Drag / 0.098f / body.mass * DNormal, Color.red, 0f, false);
            // Debug.DrawRay(worldcp, Vni, Color.yellow, 0f, false);
            // Debug.DrawRay(worldcp, transform.right * alpha[i], Color.magenta, 0f, false);
        }
    }
}
