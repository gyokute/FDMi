
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public enum TFAttribute
    {
        N1, N2, EGT, FF, force, input
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class FDMi_TurboFan_2A : FDMi_Attributes
    {
        public float[] val = { 0f, 0f, 0f, 0f, 0f, 0f };
        [UdonSynced(UdonSyncMode.Smooth)] public float N2;
        public FDMi_SyncObject fuelSW;
        public FDMi_SyncObject airSW;
        public FDMi_ActuatorControl throttle;
        public FDMi_SyncObject reverser;
        [SerializeField] private float N2min = 0.6f, N2max = 1.1f, kp, kd;
        private float dFF, pN2err, N2errD, theta;
        [SerializeField] private float FFidle = 0.47f, FFmax = 3.145f, G1 = 550f, G2 = 120f, maxAirN2 = 0.4f, airN2Coef = 0.1f;
        [SerializeField] private float ffDt = 1f, egtDt = 2f, N1Dt = 1f, N2Dt = 1f;
        public AudioSource[] sound;
        public float[] soundN1Pitch;
        public float[] soundN2Pitch;
        public float[] soundPitchOffset;
        public float[] soundN1Loud;
        public float[] soundN2Loud;
        public float[] soundLoudOffset;
        public float[] soundInsideLoud;
        public float[] soundOutsideLoud;

        Vector3 relativePos, thrustVec;

        public override void FDMi_Local_Start()
        {
            val[(int)TFAttribute.EGT] = airData[(int)AirData.temp];
            relativePos = VehicleRigidbody.transform.InverseTransformPoint(transform.position);
            thrustVec = VehicleRigidbody.transform.InverseTransformDirection(transform.forward);
        }
        public override void ResetStatus()
        {
            for (int vi = 0; vi < val.Length; vi++) val[vi] = 0f;
            for (int i = 0; i < sound.Length; i++) sound[i].volume = 0f;
            N2 = 0f;
        }

        void Update()
        {
            if (!sharedBool[0]) return;
            if (!sharedBool[(int)SharedBool.isPilot])
            {
                val[(int)TFAttribute.N2] = N2;
                if (sharedBool[(int)SharedBool.isPassenger]) Engine_PassengerUpdate();
            }
            for (int i = 0; i < sound.Length; i++)
            {
                sound[i].volume = soundN1Loud[i] * val[(int)TFAttribute.N1] + soundN2Loud[i] * val[(int)TFAttribute.N2] + soundLoudOffset[i];
                sound[i].pitch = soundN1Pitch[i] * val[(int)TFAttribute.N1];
                sound[i].pitch += soundN2Pitch[i] * val[(int)TFAttribute.N2] + soundPitchOffset[i];
                float inside = (sharedBool[(int)SharedBool.isPilot] || sharedBool[(int)SharedBool.isPassenger]) ? 1f : 0f;
                sound[i].volume *= Mathf.Lerp(soundOutsideLoud[i], soundInsideLoud[i], inside);
            }
            if (N2 >= N2min) airSW.Val = 0f;
        }
        void FixedUpdate()
        {
            if (!sharedBool[0]) return;
            if (sharedBool[(int)SharedBool.isPilot])
            {
                Engine_FixedUpdate();
                Engine_PilotFixedUpdate();
                N2 = val[(int)TFAttribute.N2];
            }
        }

        private void Engine_PassengerUpdate()
        {
            // Fuel flow(kg/s)
            val[(int)TFAttribute.input] = throttle.val;
            float dFF = fuelSW.val * Mathf.Lerp(FFidle, FFmax, val[(int)TFAttribute.input]) - val[(int)TFAttribute.FF];
            val[(int)TFAttribute.FF] = Mathf.MoveTowards(val[(int)TFAttribute.FF], val[(int)TFAttribute.FF] + dFF, ffDt * Time.deltaTime);
            //Easy Fuel-Turbine Energy equality
            float dT = 43000000f * val[(int)TFAttribute.FF];
            dT -= Mathf.Pow(val[(int)TFAttribute.N2], 2.18f) * (52230996f);
            dT -= airData[(int)AirData.machSpeed] * val[(int)TFAttribute.force];
            dT = dT * 0.000001f - 1f;
            val[(int)TFAttribute.EGT] += egtDt * dT * Time.deltaTime;
            val[(int)TFAttribute.EGT] = Mathf.Max(val[(int)TFAttribute.EGT], airData[(int)AirData.temp]);


            theta = Mathf.Sqrt((airData[(int)AirData.temp] + 273f) / 288f);
            float dN1 = val[(int)TFAttribute.N2] * 2f - 1f - val[(int)TFAttribute.N1];
            val[(int)TFAttribute.N1] = Mathf.MoveTowards(val[(int)TFAttribute.N1], val[(int)TFAttribute.N1] + dN1, N1Dt * theta * Time.deltaTime);
            val[(int)TFAttribute.N1] = Mathf.Max(val[(int)TFAttribute.N1], 0f);
            float N1Thrust = val[(int)TFAttribute.N1] * val[(int)TFAttribute.N1] * G1;
            float N2Thrust = val[(int)TFAttribute.N2] * val[(int)TFAttribute.N2] * G2;
            val[(int)TFAttribute.force] = airData[(int)AirData.machSpeed] * theta * airData[(int)AirData.pressure] / 101325.6178f;
            val[(int)TFAttribute.force] *= (N1Thrust + N2Thrust);
        }

        private void Engine_PilotFixedUpdate()
        {
            float N1Thrust = val[(int)TFAttribute.N1] * val[(int)TFAttribute.N1] * G1;
            float N2Thrust = val[(int)TFAttribute.N2] * val[(int)TFAttribute.N2] * G2;
            val[(int)TFAttribute.force] = airData[(int)AirData.machSpeed] * theta * airData[(int)AirData.pressure] / 101325.6178f;
            Vector3 localThrust = val[(int)TFAttribute.force] * (N1Thrust * (1f - 2f * reverser.val) + N2Thrust) * thrustVec;
            val[(int)TFAttribute.force] *= (N1Thrust + N2Thrust);
            if (localThrust.magnitude > 0.0001f)
            {
                VehicleRigidbody.AddRelativeForce(localThrust);
                VehicleRigidbody.AddRelativeTorque(Vector3.Cross(relativePos, localThrust));
            }
        }

        private void Engine_FixedUpdate()
        {
            // Fuel flow(kg/s)
            val[(int)TFAttribute.input] = throttle.val;
            // float N2err = Mathf.Lerp(N2min, N2max, throttle.val) - val[(int)TFAttribute.N2];
            float dFF = fuelSW.val * Mathf.Lerp(FFidle, FFmax, val[(int)TFAttribute.input]) - val[(int)TFAttribute.FF];
            // N2errD = (N2err - pN2err) / Time.fixedDeltaTime;
            // dFF = N2err * kp + N2errD * kd;
            // pN2err = N2err;
            val[(int)TFAttribute.FF] = Mathf.MoveTowards(val[(int)TFAttribute.FF], val[(int)TFAttribute.FF] + dFF, ffDt * Time.fixedDeltaTime);
            // val[(int)TFAttribute.FF] = Mathf.Clamp(val[(int)TFAttribute.FF] + dFF, FFidle, FFmax) * fuelSW.val;
            //Easy Fuel-Turbine Energy equality
            float dT = 43000000f * val[(int)TFAttribute.FF];
            dT -= Mathf.Pow(val[(int)TFAttribute.N2], 2.18f) * (52230996f);
            dT -= airData[(int)AirData.machSpeed] * val[(int)TFAttribute.force];
            dT = dT * 0.000001f - 1f;
            val[(int)TFAttribute.EGT] += egtDt * dT * Time.fixedDeltaTime;
            val[(int)TFAttribute.EGT] = Mathf.Max(val[(int)TFAttribute.EGT], airData[(int)AirData.temp]);

            theta = Mathf.Sqrt((airData[(int)AirData.temp] + 273f) / 288f);
            float dN2 = 0.55f * Mathf.Sqrt(Mathf.Max((val[(int)TFAttribute.EGT] + 273f) / 288f - 1f, 0f));
            dN2 *= Mathf.Clamp01((val[(int)TFAttribute.N2] - 0.2f) * 100f);
            dN2 = Mathf.Max(maxAirN2 * airSW.val, dN2) - val[(int)TFAttribute.N2];
            val[(int)TFAttribute.N2] += N2Dt * dN2 * Mathf.Lerp(airN2Coef, 1f, val[(int)TFAttribute.N2]) * Time.fixedDeltaTime * theta;
            val[(int)TFAttribute.N2] = Mathf.Max(val[(int)TFAttribute.N2], 0f);

            float dN1 = val[(int)TFAttribute.N2] * 2f - 1f - val[(int)TFAttribute.N1];
            val[(int)TFAttribute.N1] += N1Dt * dN1 * Time.fixedDeltaTime * theta;
            val[(int)TFAttribute.N1] = Mathf.Max(val[(int)TFAttribute.N1], 0f);
        }
    }
}
