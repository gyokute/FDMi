using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public class FDMiInclinometer : FDMiBehaviour
    {
        public Rigidbody body;

        public string SlipPath = "Slip";

        [FDMiDataPath(nameof(SlipPath))]
        public FDMiFloat Slip;

        public string RotationPath = "Rotation";

        [FDMiDataPath(nameof(RotationPath))]
        public FDMiQuaternion Rotation;

        public float gravity = 9.78f;

        [SerializeField]
        float cutoffFrequency = 0.1f;

        void Start()
        {
            _rot = Rotation.data;
            // cutoffFrequency<=0 はゼロ除算になるため既定値 0.1 にフォールバック
            omega = 1f / (cutoffFrequency > 0f ? cutoffFrequency : 0.1f);
            // 初回フレームの速度差スパイクを防ぐため現在速度で初期化
            prevVelocity = body.velocity;
        }

        void Update()
        {
            // 速度差分をボディ系へ変換してから重力項と加算（フレーム混交の修正）
            Vector3 accelBody = Quaternion.Inverse(_rot[0]) * ((body.velocity - prevVelocity) / Time.deltaTime);
            centrifugalAcc = Vector3.Lerp(centrifugalAcc, accelBody, Time.deltaTime * omega);
            Vector3 a = Vector3.ProjectOnPlane(centrifugalAcc + gravity * (Quaternion.Inverse(_rot[0]) * Vector3.down), Vector3.forward);
            Slip.Data = Vector3.SignedAngle(Vector3.down, a, Vector3.forward);
            prevVelocity = body.velocity;
        }

        float omega;
        Vector3 centrifugalAcc;
        Vector3 prevVelocity;
        Quaternion[] _rot;
    }
}
