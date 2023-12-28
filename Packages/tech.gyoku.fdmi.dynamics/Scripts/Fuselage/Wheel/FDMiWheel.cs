
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
        public FDMiFloat TillerInput, BrakeInput, ParkBrakeInput, Retract;
        public FDMiFloat SuspensionMove, WheelRotate;
        public FDMiBool IsGround;
        public float brakePressure = 3000f, parkBrakePressure = 3000f, absKi = 0.05f, absTgt=0.35f;
        [SerializeField] LayerMask groundLayer;
        [SerializeField] private float preLoadTorque = 0.0001f;
        [SerializeField] private float rpm;
        [SerializeField] private float rotateAngle = 60f;
        // [SerializeField] private float retractThreshold = 0.5f;
        [SerializeField] private Vector3 retractDirection;
        [SerializeField] private AnimationCurve retractCurve;
        private Vector3 wheelOriginPos = Vector3.zero;
        private float[] tiller, brake, parkbrake, susmove, wheelRot = { 0f };
        private bool[] isground;
        private Ray groundRay;
        private float rayMaxLen, wheelCircumference;
        void Start()
        {
            tiller = TillerInput.data;
            brake = BrakeInput.data;
            parkbrake = ParkBrakeInput.data;

            susmove = SuspensionMove.data;
            if (WheelRotate) wheelRot = WheelRotate.data;
            isground = IsGround.data;

            wheelOriginPos = wheel.transform.localPosition;
            if (Retract) Retract.subscribe(this, nameof(OnRetract));

            groundRay.direction = -Vector3.up;
            rayMaxLen = (wheel.radius * 1.05f + wheel.suspensionDistance);
            wheelCircumference = 1f / (2 * Mathf.PI * wheel.radius);
        }

        public void OnRetract()
        {
            wheel.transform.localPosition = wheelOriginPos + retractCurve.Evaluate(Retract.Data) * retractDirection;
            // wheel.enabled = (Retract.Data < retractThreshold);
        }
        RaycastHit[] results = new RaycastHit[1];
        int resultLen;
        float slipRate, absBrakePress;
        void Update()
        {
            // Ground Detection
            groundRay.origin = wheel.transform.position;
            resultLen = Physics.RaycastNonAlloc(groundRay, results, rayMaxLen, groundLayer);
            if (resultLen > 0)
            {
                SuspensionMove.Data = results[0].distance / rayMaxLen;
                WheelRotate.Data = Time.deltaTime * Vector3.Dot(body.GetPointVelocity(wheel.transform.position), transform.forward) * wheelCircumference;
            }
            else
            {
                SuspensionMove.Data = 1f;
            }
            if (isOwner)
            {
                // IsGround.Data = (resultLen > 0);
                IsGround.Data = wheel.isGrounded;
                wheel.motorTorque = Mathf.Clamp01(1f - parkbrake[0] - parkbrake[0]) * preLoadTorque;
                wheel.steerAngle = tiller[0] * rotateAngle;
                slipRate = Mathf.Abs(body.velocity.z);
                if (slipRate < 1) absBrakePress = 1f;
                else
                {
                    slipRate = 1 - (wheel.rpm / (slipRate * wheelCircumference));
                    absBrakePress += absKi * (absTgt - Mathf.Clamp01(wheel.rpm)) * Time.deltaTime;
                    absBrakePress = Mathf.Clamp01(absBrakePress);
                }
                wheel.brakeTorque = parkbrake[0] * parkBrakePressure + absBrakePress * brake[0] * brakePressure;
            }
        }
    }
}