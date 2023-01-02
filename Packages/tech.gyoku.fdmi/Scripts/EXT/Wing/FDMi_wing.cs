using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public enum CSAttribute
    {
        alpha, cl, cd, cm, area, Length
    }
    public class FDMi_wing : FDMi_Attributes
    {

        public AnimationCurve[] Cl_Alpha;
        public AnimationCurve[] Cd_Alpha;
        public AnimationCurve[] Cl_Mach;
        public AnimationCurve[] Cd_Mach;
        public AnimationCurve[] Cm_alpha;

        //        
        //      /----/wingTip[i], wingToe[i]
        //     /    / 
        //    /-*->/ CoordPos[i], cooordNormal[i]
        //   /    /
        //  /----/wingTip[i+1], wingToe[i+1]
        public Vector3[] wingTip;
        public Vector3[] wingToe;
        public Vector3[] CoordPos;
        public Vector3[] SpanNormal;
        public Vector3[] CoordNormal;
        public Vector3[] PlanformNormal;
        public Vector3[] rNormal;
        public float[] coord, alpha;

        public FDMi_ControlSurface[] controlSurface;
        private float[] totCSEffect = { 0f, 0f, 0f, 0f, 1f };
        private float[][] csAlpha, csCl, csCd, csCm, csArea;
        private Vector3 thisSpanPos, prevSpanPos, areaPos;
        private Vector3 airspeed, windSpeed, airAngularVelocity, coordAirSpeed, CdNormal, ClNormal, Cforce, Cn;
        public float C, M, wingAreaCoef = 1f;
        public Vector3 CoGOffset, force, totalForce, totalMoment;
        private Quaternion invRot;
        [SerializeField] private float[] wingArea;

        public override void FDMi_Local_Start()
        {
            gameObject.SetActive(false);
            csAlpha = new float[controlSurface.Length][];
            csCl = new float[controlSurface.Length][];
            csCd = new float[controlSurface.Length][];
            csCm = new float[controlSurface.Length][];
            csArea = new float[controlSurface.Length][];
            for (int i = 0; i < controlSurface.Length; i++)
            {
                csAlpha[i] = controlSurface[i].alpha;
                csCl[i] = controlSurface[i].cl;
                csCd[i] = controlSurface[i].cd;
                csCm[i] = controlSurface[i].cm;
                csArea[i] = controlSurface[i].area;
            }
        }
        public override void SFEXT_O_PilotEnter()
        {
            base.SFEXT_O_PilotEnter();
            gameObject.SetActive(true);
        }
        public override void SFEXT_O_PilotExit() => gameObject.SetActive(false);
        public override void SFEXT_P_PassengerEnter() => gameObject.SetActive(true);
        public override void SFEXT_P_PassengerExit() => gameObject.SetActive(false);
        private void FixedUpdate()
        {
            if (!sharedBool[0]) return;
            if (!sharedBool[(int)SharedBool.isPilot]) return;

            windSpeed[0] = airData[(int)AirData.windX];
            windSpeed[1] = airData[(int)AirData.windY];
            windSpeed[2] = airData[(int)AirData.windZ];

            totalForce = Vector3.zero;
            totalMoment = Vector3.zero;
            invRot = Quaternion.Inverse(VehicleRigidbody.rotation);
            airspeed = invRot * (windSpeed - VehicleRigidbody.velocity);
            airAngularVelocity = invRot * VehicleRigidbody.angularVelocity;
            Vector3 CoGOffset = VehicleRigidbody.centerOfMass;
            for (int i = 0; i < CoordPos.Length; i++)
            {
                totCSEffect[(int)CSAttribute.alpha] = 0f;
                totCSEffect[(int)CSAttribute.cl] = 0f;
                totCSEffect[(int)CSAttribute.cd] = 0f;
                totCSEffect[(int)CSAttribute.cm] = 0f;
                totCSEffect[(int)CSAttribute.area] = 1f;
                for (int csi = 0; csi < controlSurface.Length; csi++)
                {
                    totCSEffect[(int)CSAttribute.alpha] += csAlpha[csi][i];
                    totCSEffect[(int)CSAttribute.cl] += csCl[csi][i];
                    totCSEffect[(int)CSAttribute.cd] += csCd[csi][i];
                    totCSEffect[(int)CSAttribute.cm] += csCm[csi][i];
                    totCSEffect[(int)CSAttribute.area] *= csArea[csi][i];
                }
                coordAirSpeed = airspeed - Vector3.Cross(airAngularVelocity, CoordPos[i]);
                // coordAirSpeed = invRot * (windSpeed - VehicleRigidbody.GetRelativePointVelocity(CoordPos[i]));
                coordAirSpeed = Vector3.ProjectOnPlane(coordAirSpeed, SpanNormal[i]);
                alpha[i] = Vector3.SignedAngle(CoordNormal[i], coordAirSpeed, SpanNormal[i]) + totCSEffect[(int)CSAttribute.alpha];
                C = 0.5f * airData[(int)AirData.rho] * coordAirSpeed.sqrMagnitude * wingArea[i] * Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * alpha[i]));
                C *= totCSEffect[(int)CSAttribute.area] * wingAreaCoef;
                M = coordAirSpeed.magnitude / airData[(int)AirData.machSpeed];
                CdNormal = Vector3.Normalize(coordAirSpeed);
                ClNormal = Vector3.Cross(SpanNormal[i], CdNormal);

                Cforce = (Cl_Alpha[i].Evaluate(alpha[i]) + totCSEffect[(int)CSAttribute.cl]) * Cl_Mach[i].Evaluate(M) * ClNormal;
                Cforce += (Cd_Alpha[i].Evaluate(alpha[i]) + totCSEffect[(int)CSAttribute.cd]) * Cd_Mach[i].Evaluate(M) * CdNormal;
                totalForce += C * Cforce;
                totalMoment += C * (-coord[i] * ((Cm_alpha[i].Evaluate(alpha[i]) + totCSEffect[(int)CSAttribute.cm]) * SpanNormal[i]) + Vector3.Cross(CoordPos[i] - CoGOffset, Cforce));

                // Debug.DrawRay(transform.TransformPoint(CoordPos[i]), transform.TransformDirection(coordAirSpeed / 50), Color.yellow, 0f, false);
                // Debug.DrawRay(transform.TransformPoint(CoordPos[i]), transform.TransformDirection(Vector3.Project(coordAirSpeed, Vector3.up)), Color.cyan, 0f, false);
                // Debug.DrawRay(transform.TransformPoint(CoordPos[i]), transform.right * alpha[i], Color.magenta, 0f, false);
                // Debug.DrawRay(transform.TransformPoint(CoordPos[i]), transform.TransformDirection(Vector3.Project(3 * Cforce, ClNormal)), Color.blue, 0f, false);
                // Debug.DrawRay(transform.TransformPoint(CoordPos[i]), transform.TransformDirection(Vector3.Project(30 * Cforce, CdNormal)), Color.red, 0f, false);
                // Debug.DrawRay(transform.TransformPoint(CoordPos[i]), transform.TransformDirection(coord[i] * ((Cm_alpha[i].Evaluate(alpha) * SpanNormal[i]))), Color.yellow, 0f, false);
            }
            if (float.IsNaN(totalForce.x) || float.IsNaN(totalForce.y) || float.IsNaN(totalForce.z)) return;
            VehicleRigidbody.AddRelativeForce(totalForce);
            VehicleRigidbody.AddRelativeTorque(totalMoment);
            // Debug.Log(totCSEffect[(int)CSAttribute.area]);
            // Debug.DrawRay(transform.position, transform.right * transform.InverseTransformDirection(VehicleRigidbody.velocity).x, Color.cyan, 0f, false);
            // Debug.DrawRay(transform.position, VehicleRigidbody.velocity, Color.blue, 0f, false);
        }
    }
}
