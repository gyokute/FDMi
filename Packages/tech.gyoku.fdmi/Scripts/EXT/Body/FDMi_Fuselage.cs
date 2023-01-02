using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMi_Fuselage : FDMi_Attributes
    {
        float tim = 0f;
        [SerializeField] private bool overrideInertia, overrideInertiaRotation, overrideCog;
        // weight and COG settings
        public Vector3 fullLoadCoG, minLoadCoG;
        // Max fuel (in kg). 1gal=3.78541L Jet-A ~0.775kg/L
        public float OEW, maxFuel, fuel, payload;

        // Airplane is (mostly)synmetrical == inertia tensor rotates only in X-Axis(in unity)
        public float inertiaRotation;
        [SerializeField] private Vector3 inertiaTensor;
        private Quaternion inertiaTensorRotation;
        [UdonSynced, FieldChangeCallback(nameof(KinematicMode))] private bool _kinematicMode;
        public bool KinematicMode
        {
            get => _kinematicMode;
            set
            {
                _kinematicMode = value;
                VehicleRigidbody.isKinematic = _kinematicMode;
            }
        }

        public override void FDMi_Local_Start()
        {
            if (!overrideInertia) inertiaTensor = VehicleRigidbody.inertiaTensor;
            if (!overrideInertiaRotation) inertiaRotation = VehicleRigidbody.inertiaTensorRotation.eulerAngles.x;
            inertiaTensorRotation = Quaternion.AngleAxis(inertiaRotation, Vector3.right);
            // sharedFloat[(int)SharedFloat.fuel] = fuel;
            // sharedFloat[(int)SharedFloat.maxFuel] = maxFuel;
            if (overrideCog) setMass();
            setInertia();
            if (!overrideInertia) VehicleRigidbody.ResetInertiaTensor();
            gameObject.SetActive(false);
        }

        public override void SFEXT_O_PilotEnter() => gameObject.SetActive(true);
        public override void SFEXT_O_PilotExit() => gameObject.SetActive(false);

        private void FixedUpdate()
        {
            if (!sharedBool[0]) return;
            if (!sharedBool[(int)SharedBool.isPilot]) return;

            airDrag();
            if (overrideCog) setMass();
            if (overrideInertia) setInertia();
            fixPlane();
        }

        private void setInertia()
        {
            VehicleRigidbody.inertiaTensor = inertiaTensor;
            VehicleRigidbody.inertiaTensorRotation = inertiaTensorRotation;
            // if (!overrideInertia) VehicleRigidbody.ResetInertiaTensor();
        }
        private void setMass()
        {
            VehicleRigidbody.mass = OEW + payload + fuel;
            VehicleRigidbody.centerOfMass = Vector3.Lerp(fullLoadCoG, minLoadCoG, fuel / maxFuel);
        }

        public AnimationCurve Cd_Alpha;
        public float bodyArea;
        private Vector3 airspeed, airSpeedVec;
        private float alpha, C;
        private void airDrag()
        {
            airspeed.x = airData[(int)AirData.AirSpeedX];
            airspeed.y = airData[(int)AirData.AirSpeedY];
            airspeed.z = airData[(int)AirData.AirSpeedZ];
            airSpeedVec = Vector3.Normalize(airspeed);
            alpha = Vector3.SignedAngle(transform.forward, airspeed, transform.right);
            C = 0.5f * airData[(int)AirData.rho] * airspeed.sqrMagnitude * bodyArea * Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * alpha));
            VehicleRigidbody.AddRelativeForce(-C * Cd_Alpha.Evaluate(alpha) * airSpeedVec);
        }
        #region stopper
        public FDMi_InputObject parkBrake;
        private void fixPlane()
        {
            if (parkBrake == null) return;
            if (Mathf.Abs(airData[(int)AirData.GS]) < 0.05f && parkBrake.val > 0.5f) KinematicMode = true;
            if (parkBrake.val < 0.5f) KinematicMode = false;
        }
        #endregion
    }
}