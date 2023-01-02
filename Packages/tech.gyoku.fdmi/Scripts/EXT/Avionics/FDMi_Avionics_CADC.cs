
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public enum AirData
    {
        rho, temp, pressure, machSpeed,
        ALT, VS, VSRate, RadioALT,
        windX, windY, windZ, windDeg, windSpeed,
        AirSpeedX, AirSpeedY, AirSpeedZ,
        IAS, IASAcc, TAS, TASAcc, GS, Mach, TRK, alpha,
        pitch, roll, HDG, pitchRate, rollRate, yawRate,
        pitchAcc, rollAcc, yawAcc,
        G, Slip, GSAcc,
        Length

    }
    public class FDMi_Avionics_CADC : FDMi_Attributes
    {
        [System.NonSerializedAttribute] public float[] Data;
        public bool SW = true;
        [SerializeField] private AnimationCurve alt_rho;
        [SerializeField] private AnimationCurve alt_temp;
        [SerializeField] private AnimationCurve alt_mach;
        [SerializeField] private AnimationCurve alt_pressure;
        public float heightOffset, HDGOffset, fc = 1f;
        private Vector3 prevPos, groundSpeedVec, prevGSVec, GRate, airSpeedVec, windVec;
        private Vector3 rot, rotRate, prevRotRate, rotAcc;
        private Quaternion rotation, prevRotation;
        private float dT, rho0ft, prevIAS, prevTAS, prevVS, relax;

        public override void FDMi_InitData()
        {
            Data = new float[(int)AirData.Length];
            Param.airData = Data;
            rho0ft = alt_rho.Evaluate(0f);
            gameObject.SetActive(false);
        }
        public override void SFEXT_O_PilotEnter() => gameObject.SetActive(true);
        public override void SFEXT_O_PilotExit() => gameObject.SetActive(false);
        public override void SFEXT_P_PassengerEnter() => gameObject.SetActive(true);
        public override void SFEXT_P_PassengerExit() => gameObject.SetActive(false);
        private void LateUpdate()
        {
            if (!sharedBool[0]) return;
            if (!sharedBool[(int)SharedBool.isPassenger] || !SW) return;
            dT = 1f / Time.deltaTime;
            relax = 2 * Mathf.PI * Time.deltaTime * fc;
            relax = relax / (relax + 1f);
            // groundSpeedVec = (transform.position - prevPos) * dT;
            groundSpeedVec = Vector3.Lerp(groundSpeedVec, (transform.position - prevPos) * dT, relax);
            airSpeedVec = transform.InverseTransformVector(groundSpeedVec - windVec);
            prevPos = transform.position;
            rotation = VehicleRigidbody.rotation;
            rot = rotation.eulerAngles;
            rotRate = (Quaternion.Inverse(prevRotation) * rotation).eulerAngles;
            rotRate.x -= Mathf.Floor(rotRate.x / 180.001f) * 360f;
            rotRate.y -= Mathf.Floor(rotRate.y / 180.001f) * 360f;
            rotRate.z -= Mathf.Floor(rotRate.z / 180.001f) * 360f;
            rotRate *= dT * Mathf.Deg2Rad;
            rotRate = Vector3.Lerp(prevRotRate, rotRate, relax);
            prevRotation = rotation;
            AirData_Update();
        }
        private void FixedUpdate()
        {
            if (!sharedBool[0]) return;
            if (!sharedBool[(int)SharedBool.isPilot] || !SW) return;
            dT = 1f / Time.fixedDeltaTime;
            relax = 2 * Mathf.PI * Time.fixedDeltaTime * fc;
            relax = relax / (relax + 1f);

            groundSpeedVec = VehicleRigidbody.velocity;
            rotation = VehicleRigidbody.rotation;
            airSpeedVec = Quaternion.Inverse(rotation)*(groundSpeedVec - windVec);
            rot = rotation.eulerAngles;
            rotRate = Quaternion.Inverse(rotation) * VehicleRigidbody.angularVelocity;
            AirData_Update();
        }

        private void AirData_Update()
        {

            windVec.x = Data[(int)AirData.windX];
            windVec.y = Data[(int)AirData.windY];
            windVec.z = Data[(int)AirData.windZ];

            Data[(int)AirData.AirSpeedX] = airSpeedVec.x;
            Data[(int)AirData.AirSpeedY] = airSpeedVec.y;
            Data[(int)AirData.AirSpeedZ] = airSpeedVec.z;
            Data[(int)AirData.alpha] = -Vector3.SignedAngle(Vector3.ProjectOnPlane(airSpeedVec, Vector3.right), Vector3.forward, Vector3.right);

            Data[(int)AirData.ALT] = transform.position.y + heightOffset;
            Data[(int)AirData.rho] = alt_rho.Evaluate(Data[(int)AirData.ALT]);
            Data[(int)AirData.temp] = alt_temp.Evaluate(Data[(int)AirData.ALT]);
            Data[(int)AirData.machSpeed] = alt_mach.Evaluate(Data[(int)AirData.ALT]);
            Data[(int)AirData.pressure] = alt_pressure.Evaluate(Data[(int)AirData.ALT]);

            Data[(int)AirData.TAS] = airSpeedVec.z;
            Data[(int)AirData.IAS] = Data[(int)AirData.TAS] * Mathf.Sqrt(Data[(int)AirData.rho] / rho0ft);
            Data[(int)AirData.Mach] = Data[(int)AirData.TAS] / Data[(int)AirData.machSpeed];


            Data[(int)AirData.TASAcc] = Mathf.Lerp(Data[(int)AirData.TASAcc], (Data[(int)AirData.TAS] - prevTAS) * dT, relax);
            Data[(int)AirData.IASAcc] = Mathf.Lerp(Data[(int)AirData.IASAcc], (Data[(int)AirData.IAS] - prevIAS) * dT, relax);
            prevTAS = Data[(int)AirData.TAS];
            prevIAS = Data[(int)AirData.IAS];
            Data[(int)AirData.GS] = transform.InverseTransformVector(groundSpeedVec).z;

            Data[(int)AirData.VS] = groundSpeedVec.y;
            Data[(int)AirData.VSRate] = (Data[(int)AirData.VS] - prevVS) * dT;
            prevVS = Data[(int)AirData.VS];

            Data[(int)AirData.TRK] = Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(groundSpeedVec, Vector3.up), Vector3.up);
            Data[(int)AirData.TRK] += Mathf.Ceil(-Data[(int)AirData.TRK] / 180.1f) * 360f;

            Data[(int)AirData.pitch] = rot.x - Mathf.Ceil(Mathf.Clamp01(rot.x - 180f)) * 360f;
            Data[(int)AirData.roll] = rot.z - Mathf.Ceil(Mathf.Clamp01(rot.z - 180f)) * 360f;
            Data[(int)AirData.HDG] = rot.y;
            Data[(int)AirData.pitchRate] = rotRate.x;
            Data[(int)AirData.rollRate] = rotRate.z;
            Data[(int)AirData.yawRate] = rotRate.y;
            rotAcc = Vector3.Lerp(rotAcc, (rotRate - prevRotRate) * dT, relax);
            prevRotRate = rotRate;
            Data[(int)AirData.pitchAcc] = rotAcc.x;
            Data[(int)AirData.rollAcc] = rotAcc.z;
            Data[(int)AirData.yawAcc] = rotAcc.y;

            GRate = Vector3.Lerp(GRate, transform.InverseTransformVector(dT * (prevGSVec - groundSpeedVec) - 9.80665f * Vector3.up), relax);
            prevGSVec = groundSpeedVec;
            Data[(int)AirData.G] = GRate.y * 0.10197162129f;
            // Data[(int)AirData.Slip] = Vector3.SignedAngle(-Vector3.up, Vector3.ProjectOnPlane(GRate, Vector3.forward), Vector3.forward);
            Data[(int)AirData.Slip] = GRate.x * 0.10197162129f;
            Data[(int)AirData.GSAcc] = -GRate.z;
        }
    }
}
