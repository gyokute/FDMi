
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_LandingGear : FDMi_Attributes
    {
        // Override Nvidia-WheelCollider to ideal brake simulation....
        // val: gear situation (1:up 0:down)
        public float val;
        public WheelCollider wheel;
        public FDMi_SyncObject gearLever, parkBrakeInput;
        public FDMi_ActuatorControl tillerInput, brakeInput;
        [SerializeField] Transform[] WheelBone;
        public float moveTime;
        public bool isGround;
        // psi = 6894.76Pa
        public float brakePressure = 3000f;

        [SerializeField] private float parkBrakePressure = 0f;
        // Torque= μ(Ap)r[N]
        // 0.4* (p*0.15*7*6)*0.8
        public float frictionTorque = 10;
        public float airDrag = 2;
        public FDMi_GroundSpoiler GS;
        [SerializeField] private float preLoadTorque = 0.0001f;
        private Vector3 dragForce;
        [SerializeField] private float rpm;
        [SerializeField] private float boneRotCoef = 60f;
        [SerializeField] private FDMi_Annunciator[] annunciators;


        void Update()
        {
            // gear up/down move
            val = Mathf.MoveTowards(val, gearLever.val, Time.deltaTime / moveTime);
            foreach (Transform wb in WheelBone) wb.Rotate(0, wheel.rpm * Time.deltaTime * boneRotCoef, 0);
            foreach (FDMi_Annunciator ind in annunciators) ind.whenChange(Mathf.RoundToInt(val * 2));

            if (!sharedBool[0]) return;
            if (!sharedBool[(int)SharedBool.isPilot] && !sharedBool[(int)SharedBool.isPassenger]) return;
            rpm = wheel.rpm;
            // Ground Detection
            isGround = wheel.isGrounded;
            if (GS != null && isGround) GS.whenLand();
            wheel.brakeTorque = brakeInput.val * brakePressure + parkBrakeInput.val * parkBrakePressure;
            wheel.motorTorque = Mathf.Clamp01(wheel.rpm + 0.01f) * Mathf.Clamp01(1f - parkBrakeInput.val - brakeInput.val) * preLoadTorque;
            if (tillerInput != null) wheel.steerAngle = tillerInput.val;
        }

        void FixedUpdate()
        {
            if (!sharedBool[(int)SharedBool.initialized]) return;
            if (!sharedBool[(int)SharedBool.isPilot]) return;
            dragForce = -Vector3.forward * (1 - val) * airDrag * airData[(int)AirData.rho] * airData[(int)AirData.IAS] * airData[(int)AirData.IAS];
            VehicleRigidbody.AddRelativeForce(dragForce);
        }

    }
}