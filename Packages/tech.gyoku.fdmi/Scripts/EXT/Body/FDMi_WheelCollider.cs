
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace SaccFlight_FDMi
{
    public class FDMi_WheelCollider : FDMi_Attributes
    {
        public FDMi_SyncObject gearLever, parkBrakeInput;
        public FDMi_ActuatorControl tillerInput, brakeInput;
        public float radius = 0.34f;
        public float suspensionTravel = 0.2f;
        public float susPressure = 10000000f;
        public float susArea = 0.025f;
        public float damping = 5000;
        public LayerMask layer;
        public float inertia = 2.2f;
        public float grip = 1.0f, sideGrip = 10.0f;
        public float brakeFrictionTorque = 27580000;
        public float parkBrakeTorqueRatio = 0.01f;
        public float frictionTorque = 10;
        public float airDrag = 10;
        public float maxSteerAngle = 0f;
        public Transform steerBone;
        public Transform[] wheelModel;
        public float massFraction = 0.25f;
        public float[] a = { 1.0f, -60f, 1688f, 4140f, 6.026f, 0f, -0.3589f, 1f, 0f, -6.111f / 1000f, -3.244f / 100f, 0f, 0f, 0f, 0f };
        public float[] b = { 1.0f, -60f, 1588f, 0f, 229f, 0f, 0f, 0f, -10f, 0f, 0f };
        public float gearMoveTime = 3f;
        // brake input
        private float brake, steerAngle, gearMoveSpeed = 0f;
        public float val;
        // output
        public bool isGrounded;
        public float angularVelocity, slipRatio, slipAngle, prevVal;
        public float compression;
        [SerializeField] private float curSusTravel;
        [SerializeField] private FDMi_Annunciator annunciator;

        // state
        Vector3 wheelVel, localVel, suspensionForce, roadForce, groundNormal, up, right, forward;
        Quaternion localRotation = Quaternion.identity;
        Quaternion inverseLocalRotation = Quaternion.identity;
        float normalForce, maxSlip, maxAngle, oldAngle;
        RaycastHit hit;

        public override void FDMi_Local_Start()
        {
            InitSlipMaxima();
            gearMoveSpeed = 1 / gearMoveTime;
        }
        public override void SFEXT_O_PilotEnter()
        {
            base.SFEXT_O_PilotEnter();
        }

        public override void ResetStatus()
        {
            val = 0f;
        }

        void Update()
        {
            val = Mathf.MoveTowards(val, gearLever.val, Time.deltaTime * gearMoveSpeed);
            if (annunciator) annunciator.whenChange(Mathf.CeilToInt(val * 1.1f - 0.005f));
        }
        void FixedUpdate()
        {
            if (!sharedBool[(int)SharedBool.initialized]) return;
            if (!Networking.IsOwner(gameObject) || VehicleRigidbody.isKinematic) return;
            if (val > 0.5f || Mathf.Approximately(maxAngle, 0)) return;
            Vector3 pos = transform.position;
            up = transform.up;
            float currentMaxSus = suspensionTravel * (1f - val);
            brake = brakeInput.val + parkBrakeInput.val * parkBrakeTorqueRatio;
            steerAngle = tillerInput.val * maxSteerAngle;
            Vector3 dragForce = -transform.forward * (1 - val) * airDrag * airData[(int)AirData.rho] * airData[(int)AirData.TAS] * airData[(int)AirData.TAS];
            isGrounded = Physics.SphereCast(pos, radius, -up, out hit, currentMaxSus, layer, QueryTriggerInteraction.Ignore);
            if (isGrounded)
            {
                groundNormal = transform.InverseTransformDirection(inverseLocalRotation * hit.normal);
                curSusTravel = suspensionTravel - hit.distance + radius;
                compression = (curSusTravel / suspensionTravel);
                wheelVel = VehicleRigidbody.GetPointVelocity(pos);
                localVel = transform.InverseTransformDirection(inverseLocalRotation * wheelVel);
                VehicleRigidbody.AddForceAtPosition(SuspensionForce() + RoadForce() + dragForce, pos);
            }
            else
            {
                curSusTravel = 0f;
                compression = 0.0f;
                suspensionForce = Vector3.zero;
                roadForce = Vector3.zero;
                float totalFrictionTorque = brakeFrictionTorque * brake + frictionTorque;
                float frictionAngularDelta = totalFrictionTorque * Time.fixedDeltaTime / inertia;
                if (Mathf.Abs(angularVelocity) > frictionAngularDelta)
                    angularVelocity -= frictionAngularDelta * Mathf.Sign(angularVelocity);
                else
                    angularVelocity = 0;
                slipRatio = 0;
                VehicleRigidbody.AddForceAtPosition(dragForce, pos);
            }
            compression = Mathf.Clamp01(compression);
            foreach (Transform model in wheelModel)
            {
                // model.transform.localPosition = Vector3.up * (compression - 1.0f) * suspensionTravel;
                model.Rotate(0, angularVelocity, 0);
            }
            if (steerBone != null)
            {
                steerBone.localEulerAngles = Vector3.up * -steerAngle;
            }
        }
        #region pacejka
        float CalcLongitudinalForce(float Fz, float slip)
        {
            Fz *= 0.001f;//convert to kN
            slip *= 100f; //covert to %
            float uP = b[1] * Fz + b[2];
            float D = uP * Fz;
            float B = ((b[3] * Fz + b[4]) * Mathf.Exp(-b[5] * Fz)) / (b[0] * uP);
            float S = slip + b[9] * Fz + b[10];
            float E = b[6] * Fz * Fz + b[7] * Fz + b[8];
            float Fx = D * Mathf.Sin(b[0] * Mathf.Atan(S * B + E * (Mathf.Atan(S * B) - S * B)));
            return Fx;
        }
        float CalcLateralForce(float Fz, float slipAngle)
        {
            Fz *= 0.001f;//convert to kN
            slipAngle *= Mathf.Rad2Deg; //convert angle to deg
            float uP = a[1] * Fz + a[2];
            float D = uP * Fz;
            float B = (a[3] * Mathf.Sin(2 * Mathf.Atan(Fz / a[4]))) / (a[0] * uP * Fz);
            float S = slipAngle + a[9] * Fz + a[10];
            float E = a[6] * Fz + a[7];
            float Sv = a[12] * Fz + a[13];
            float Fy = D * Mathf.Sin(a[0] * Mathf.Atan(S * B + E * (Mathf.Atan(S * B) - S * B))) + Sv;
            return Fy;
        }
        Vector3 CombinedForce(float Fz, float slip, float slipAngle)
        {
            float unitSlip = slip / maxSlip;
            float unitAngle = slipAngle / maxAngle;
            float p = Mathf.Sqrt(unitSlip * unitSlip + unitAngle * unitAngle);
            if (p > Mathf.Epsilon)
            {
                if (Mathf.Abs(slip) < 0.8f)
                    return -localVel.normalized * (Mathf.Abs(unitAngle / p * CalcLateralForce(Fz, p * maxAngle)) * sideGrip + Mathf.Abs(unitSlip / p * CalcLongitudinalForce(Fz, p * maxSlip)));
                else
                {
                    Vector3 forward = new Vector3(0, -groundNormal.z, groundNormal.y);
                    return Vector3.right * unitAngle / p * CalcLateralForce(Fz, p * maxAngle) * sideGrip + forward * unitSlip / p * CalcLongitudinalForce(Fz, p * maxSlip);
                }
            }
            else
                return Vector3.zero;
        }
        void InitSlipMaxima()
        {
            const float stepSize = 0.001f;
            const float testNormalForce = 4000f;
            float force = 0;
            for (float slip = stepSize; ; slip += stepSize)
            {
                float newForce = CalcLongitudinalForce(testNormalForce, slip);
                if (force < newForce)
                    force = newForce;
                else
                {
                    maxSlip = slip - stepSize;
                    break;
                }
            }
            force = 0;
            for (float slipAngle = stepSize; ; slipAngle += stepSize)
            {
                float newForce = CalcLateralForce(testNormalForce, slipAngle);
                if (force < newForce)
                    force = newForce;
                else
                {
                    maxAngle = slipAngle - stepSize;
                    break;
                }
            }
        }
        #endregion
        Vector3 SuspensionForce()
        {
            float susRate = susPressure * susArea * curSusTravel;
            float springForce = susRate * compression * suspensionTravel;
            float damperForce = Vector3.Dot(localVel, groundNormal) * damping;
            normalForce = springForce;
            return (springForce - damperForce) * up;
        }

        float SlipRatio()
        {
            const float fullSlipVel = 4.0f;

            float wheelRoadVel = Vector3.Dot(wheelVel, forward);
            if (wheelRoadVel == 0)
                return 0;
            float absRoadVel = Mathf.Abs(wheelRoadVel);
            float damping = Mathf.Clamp01(absRoadVel / fullSlipVel);

            float wheelTireVel = angularVelocity * radius;
            return (wheelTireVel - wheelRoadVel) / absRoadVel * damping;
        }

        float SlipAngle()
        {
            const float fullAngleVel = 2.0f;

            Vector3 wheelMotionDirection = localVel;
            wheelMotionDirection.y = 0;

            if (wheelMotionDirection.sqrMagnitude < Mathf.Epsilon)
                return 0;

            float sinSlipAngle = wheelMotionDirection.normalized.x;
            sinSlipAngle = Mathf.Clamp(sinSlipAngle, -1, 1); // To avoid precision errors.
            float damping = Mathf.Clamp01(localVel.magnitude / fullAngleVel);
            return -Mathf.Asin(sinSlipAngle) * damping * damping;
        }

        Vector3 RoadForce()
        {
            int slipRes = (int)((100.0f - Mathf.Abs(angularVelocity)) / (10.0f));
            if (slipRes < 1)
                slipRes = 1;
            float invSlipRes = (1.0f / (float)slipRes);

            float totalFrictionTorque = brakeFrictionTorque * brake + frictionTorque;
            float frictionAngularDelta = totalFrictionTorque * Time.fixedDeltaTime * invSlipRes / inertia;

            Vector3 totalForce = Vector3.zero;
            float newAngle = steerAngle;
            for (int i = 0; i < slipRes; i++)
            {
                float f = i * 1.0f / (float)slipRes;
                localRotation = Quaternion.Euler(0, oldAngle + (newAngle - oldAngle) * f, 0);
                inverseLocalRotation = Quaternion.Inverse(localRotation);
                forward = transform.TransformDirection(localRotation * Vector3.forward);
                right = transform.TransformDirection(localRotation * Vector3.right);

                slipRatio = Mathf.Clamp(SlipRatio(), -1, 1);
                slipAngle = SlipAngle();
                Vector3 force = invSlipRes * grip * CombinedForce(normalForce, slipRatio, slipAngle);
                Vector3 worldForce = transform.TransformDirection(localRotation * force);
                angularVelocity -= (force.z * radius * Time.fixedDeltaTime) / inertia;
                if (Mathf.Abs(angularVelocity) > frictionAngularDelta)
                    angularVelocity -= frictionAngularDelta * Mathf.Sign(angularVelocity);
                else
                    angularVelocity = 0;
                wheelVel += worldForce * (1 / VehicleRigidbody.mass) * Time.fixedDeltaTime * invSlipRes;
                totalForce += worldForce;
            }
            oldAngle = newAngle;
            return totalForce;
        }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            // Physics.Raycast(pos, -up, out hit, suspensionTravel + radius, layer, QueryTriggerInteraction.Ignore);
            Gizmos.DrawLine(
                transform.position,
                transform.position + (-transform.up * (suspensionTravel))
            );
            Gizmos.color = Color.green;
            // Physics.Raycast(pos, -up, out hit, suspensionTravel + radius, layer, QueryTriggerInteraction.Ignore);
            Gizmos.DrawLine(
                transform.position + (-transform.up * (suspensionTravel)),
                transform.position + (-transform.up * (suspensionTravel + radius))
            );
            //Draw the wheel
            // Vector3 point1;
            // Vector3 point0 = transform.TransformPoint(m_wheelRadius * new Vector3(0, Mathf.Sin(0), Mathf.Cos(0)));
            // for (int i = 1; i <= 20; ++i)
            // {
            //     point1 = transform.TransformPoint(m_wheelRadius * new Vector3(0, Mathf.Sin(i / 20.0f * Mathf.PI * 2.0f), Mathf.Cos(i / 20.0f * Mathf.PI * 2.0f)));
            //     Gizmos.DrawLine(point0, point1);
            //     point0 = point1;

            // }
            Gizmos.color = Color.white;
        }
#endif
    }
}