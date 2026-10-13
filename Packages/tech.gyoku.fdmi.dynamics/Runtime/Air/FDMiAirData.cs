using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // 機体固定座標系 (+Z = 前方) の対気速度と大気データを出す。空力計算の上流。
    // AirSpeed = 機体座標系で見た対気速度ベクトル (機体の動きを空気基準で表したもの)。
    // Wing は -AirSpeed を相対風として使う。
    // 旧 aerodynamics からの移植。TASAcc の pIAS 誤参照・風の座標系混在を修正し、
    // TAS は |v|、IAS 基準密度は 1.225 kg/m3 (ISA 海面) にした。
    public class FDMiAirData : FDMiBehaviour
    {
        [Header("Wiring")]
        public Rigidbody body;
        public FDMiVector3 Wind; // 世界座標の風ベクトル [m/s]
        public FDMiFloat Rho; // 空気密度 [kg/m3]
        public FDMiFloat SonicSpeed; // 音速 [m/s]

        [Header("Outputs")]
        public FDMiVector3 AirSpeed;
        public FDMiFloat Alpha; // 迎角 [deg]
        public FDMiFloat Beta; // 横滑り角 [deg]
        public FDMiFloat IAS; // 指示対気速度 [m/s]
        public FDMiFloat TAS; // 真対気速度 [m/s]
        public FDMiFloat Mach;
        public FDMiFloat VerticalSpeed; // 世界 Y 速度 [m/s]
        public FDMiFloat IASAcc; // 指示対気速度の時間微分 [m/s2]
        public FDMiFloat TASAcc; // 真対気速度の時間微分 [m/s2]

        [Header("Smoothing")]
        public float tauAcc = 0.5f; // 加速度の一次遅れ [s] (微分のノイズ吸収。定常値は変えない)

        Vector3[] _wind;
        float[] _rho, _sonic;
        Vector3[] _air;
        float[] _alpha, _beta, _ias, _tas, _mach, _vs, _iasAcc, _tasAcc;

        void Start()
        {
            _wind = Wind.data;
            _rho = Rho.data;
            _sonic = SonicSpeed.data;
            _air = AirSpeed.data;
            _alpha = Alpha.data;
            _beta = Beta.data;
            _ias = IAS.data;
            _tas = TAS.data;
            _mach = Mach.data;
            _vs = VerticalSpeed.data;
            _iasAcc = IASAcc.data;
            _tasAcc = TASAcc.data;
        }

        void FixedUpdate()
        {
            if (body == null)
                return;
            float dt = Time.fixedDeltaTime;
            if (dt <= 0f)
                return;

            Vector3 local = Quaternion.Inverse(body.rotation) * (body.velocity - _wind[0]);
            _air[0] = local;

            _alpha[0] = -Vector3.SignedAngle(Vector3.ProjectOnPlane(local, Vector3.right), Vector3.forward, Vector3.right);
            _beta[0] = Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(local, Vector3.up), Vector3.up);

            float pIAS = _ias[0];
            float pTAS = _tas[0];
            float tas = local.magnitude;
            float ias = tas * Mathf.Sqrt(_rho[0] / 1.225f);
            _tas[0] = tas;
            _ias[0] = ias;
            _mach[0] = tas / Mathf.Max(_sonic[0], 0.1f);
            _vs[0] = body.velocity.y;

            float k = dt / (tauAcc + dt);
            _iasAcc[0] += ((ias - pIAS) / dt - _iasAcc[0]) * k;
            _tasAcc[0] += ((tas - pTAS) / dt - _tasAcc[0]) * k;
        }
    }
}
