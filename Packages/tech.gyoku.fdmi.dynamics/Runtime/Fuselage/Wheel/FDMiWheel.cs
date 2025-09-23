
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.dynamics
{
    public class FDMiWheel : FDMiAttribute
    {
        public WheelCollider wheel;
        public FDMiFloat TillerInput, TorqueInput, BrakeInput, Retract;
        public FDMiSByte ParkBrakeInput;
        public FDMiFloat SuspensionMove, WheelRotate;
        public FDMiBool IsGround;
        public float brakePressure = 3000f, parkBrakePressure = 3000f, absKi = 0.05f, absTgt = 0.35f;
        [SerializeField] LayerMask groundLayer;
        [SerializeField] private float preLoadTorque = 0.0001f;
        [SerializeField] private float rpm;
        [SerializeField] private float rotateAngle = 60f;
        // [SerializeField] private float retractThreshold = 0.5f;
        [SerializeField] private Vector3 retractDirection;
        [SerializeField] private AnimationCurve retractCurve;
        private Vector3 wheelOriginPos = Vector3.zero;
        private float[] tiller, torque, brake, susmove, wheelRot = { 0f };
        private sbyte[] parkbrake;
        private bool[] isground;
        private Ray groundRay;
        private float rayMaxLen, wheelCircumference;
        void Start()
        {
            tiller = TillerInput.data;
            brake = BrakeInput.data;
            parkbrake = ParkBrakeInput.data;
            if (TorqueInput) torque = TorqueInput.data;

            susmove = SuspensionMove.data;
            if (WheelRotate) wheelRot = WheelRotate.data;
            isground = IsGround.data;

            wheelOriginPos = wheel.transform.localPosition;
            if (Retract) Retract.subscribe(this, nameof(OnRetract));

            groundRay.direction = -Vector3.up;
            rayMaxLen = 1f / wheel.suspensionDistance; // targetPosition=0.5f fixed?
            wheelCircumference = 1f / (2 * Mathf.PI * wheel.radius);
        }

        public void OnRetract()
        {
            wheel.transform.localPosition = wheelOriginPos + retractCurve.Evaluate(Retract.Data) * retractDirection;
            // wheel.enabled = (Retract.Data < retractThreshold);
        }
        RaycastHit result;
        void LateUpdate()
        {
            // Ground Detection
            groundRay.origin = wheel.transform.position;
            isground[0] = Physics.Raycast(groundRay, out result, rayMaxLen, groundLayer);
            if (isground[0])
            {
                susmove[0] = (result.distance - wheel.radius) * rayMaxLen;
                wheelRot[0] = Time.deltaTime * Vector3.Dot(body.GetPointVelocity(wheel.transform.position), transform.forward) * wheelCircumference;
            }
            else
            {
                susmove[0] = 1f;
                wheelRot[0] = Mathf.MoveTowards(wheelRot[0], 0f, Time.deltaTime);
            }

            if (isOwner)
            {
                float parkBrakeFloat = parkbrake[0] / 127;
                float slipRate = Mathf.Abs(body.velocity.z);
                if (TorqueInput) wheel.motorTorque = Mathf.Clamp01(1f - parkBrakeFloat - parkBrakeFloat) * torque[0];
                else wheel.motorTorque = Mathf.Clamp01(0.1f - slipRate) * Mathf.Clamp01(1f - parkBrakeFloat - parkBrakeFloat) * preLoadTorque;
                wheel.steerAngle = tiller[0] * rotateAngle;
                wheel.brakeTorque = parkBrakeFloat * parkBrakePressure + brake[0] * brakePressure;
            }
        }
    }
}