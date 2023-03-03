
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.v2.core;

namespace tech.gyoku.FDMi.v2.aerodynamics
{
    public class AirFoil : VorticityField
    { 
        public Planform lPlanf, rPlanf;
        public float chordLength;
        public VorticityField[] affectedVorticity;

        public AnimationCurve Cl_Alpha, Cd_Alpha, Cm_alpha;
        public AnimationCurve Cl_Mach, Cd_Mach;

        private Vector3 windSpeed;
        private Vector3 CoGOffset, chordPos, chordNormal;
        void Start()
        {
            SetChord();
        }
        void FixedUpdate()
        {
            Vector3 invRot = Quaternion.Inverse(vehicle.rotation);
            Vector3 airspeed = invRot * (windSpeed - vehicle.velocity);
            Vector3 airAngularVelocity = invRot * vehicle.angularVelocity;
            CoGOffset = vehicle.centerOfMass;

            Vector3 coordAirSpeed = airspeed - Vector3.Cross(airAngularVelocity, CoordPos);
            coordAirSpeed = Vector3.ProjectOnPlane(coordAirSpeed, SpanNormal[i]);
            alpha[i] = Vector3.SignedAngle(CoordNormal[i], coordAirSpeed, SpanNormal[i]) + totCSEffect[(int)CSAttribute.alpha];
            q = 0.5f * airData[(int)AirData.rho] * coordAirSpeed.sqrMagnitude * wingArea[i] * Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * alpha[i]));
            C *= totCSEffect[(int)CSAttribute.area] * wingAreaCoef;
            M = coordAirSpeed.magnitude / airData[(int)AirData.machSpeed];
            CdNormal = Vector3.Normalize(coordAirSpeed);
            ClNormal = Vector3.Cross(SpanNormal[i], CdNormal);

            Cforce = (Cl_Alpha[i].Evaluate(alpha[i]) + totCSEffect[(int)CSAttribute.cl]) * Cl_Mach[i].Evaluate(M) * ClNormal;
            Cforce += (Cd_Alpha[i].Evaluate(alpha[i]) + totCSEffect[(int)CSAttribute.cd]) * Cd_Mach[i].Evaluate(M) * CdNormal;
        }

        public void SetChord()
        {
            chordPos = vehicle.transform.InverseTransformPoint(transform.position);
            chordNormal = vehicle.transform.InverseTransformDirection(transform.forward);
        }

    }
}