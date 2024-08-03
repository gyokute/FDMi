using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public class FDMiTurnCordinator : FDMiAttribute
    {
        public FDMiFloat TurnRate;
        public FDMiQuaternion Rotation;
        public float rollRateMultiplier = 0.1f;
        Quaternion[] rot;
        void Start()
        {
            rot = Rotation.data;
        }

        Vector3 centrifugalAcc, prevVelocity;
        void Update()
        {
            // Turn Cordinator
            float yawRate = (Quaternion.Inverse(rot[0]) * body.angularVelocity).y;
            float rollRate = -body.angularVelocity.z;

            TurnRate.Data = Mathf.Rad2Deg * (rollRate * rollRateMultiplier + yawRate);
        }
    }
}
