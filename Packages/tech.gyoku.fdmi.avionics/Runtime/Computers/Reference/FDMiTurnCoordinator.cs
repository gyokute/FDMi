using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public class FDMiTurnCoordinator : FDMiBehaviour
    {
        public Rigidbody body;

        public string TurnRatePath = "TurnRate";

        [FDMiDataPath(nameof(TurnRatePath))]
        public FDMiFloat TurnRate;

        public string RotationPath = "Rotation";

        [FDMiDataPath(nameof(RotationPath))]
        public FDMiQuaternion Rotation;

        public float rollRateMultiplier = 0.1f;

        void Start()
        {
            _rot = Rotation.data;
        }

        void Update()
        {
            // yawRate と揃えるためボディ系角速度から取得（ロールは符号反転して使用）
            Vector3 angVel = Quaternion.Inverse(_rot[0]) * body.angularVelocity;
            float yawRate = angVel.y;
            float rollRate = -angVel.z;
            TurnRate.Data = Mathf.Rad2Deg * (rollRate * rollRateMultiplier + yawRate);
        }

        Quaternion[] _rot;
    }
}
