using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // 機体固定座標系 (+Z = 前方) の対気速度と大気データを出す。空力・エンジン計算の上流。
    // AirSpeed = 機体座標系で見た対気速度ベクトル (機体速度 - 風)。FDMiWing は wind と body.velocity から
    // 逆向き (風 - 機体速度) を自分で作る。翼が AirSpeed を読まないのは、角速度の機体座標変換に結局
    // invRot が要り、節約がベクトル減算1回ぶんに留まるため。
    // 旧 aerodynamics からの移植。TASAcc の pIAS 誤参照・風の座標系混在を修正し、
    // TAS は |v|、IAS 基準密度は 1.225 kg/m3 (ISA 海面) にした。
    // OAT / DynamicPressure は JetEngine が要る全温・全圧 (等エントロピー関係で Mach から解く)。
    //
    // Udon 最適化:
    // - 迎角・横滑り角は Atan2 直算 (旧 SignedAngle + ProjectOnPlane は正規化×4相当を含む)。
    //   Alpha = -Atan2(y, z)、Beta = +Atan2(x, z)。旧式と等価 (符号は旧式の minus 込みで合わせた)。
    // - body.velocity / wind はローカルに1回読み。割り算は invDt にまとめた。
    public class FDMiAirData : FDMiBehaviour
    {
        [Header("Wiring")]
        public Rigidbody body;
        public string WindPath = "~/Physics/Wind";

        [FDMiDataPath(nameof(WindPath))]
        public FDMiVector3 Wind; // 世界座標の風ベクトル [m/s]
        public string SATPath = "~/Physics/SAT";

        [FDMiDataPath(nameof(SATPath))]
        public FDMiFloat SAT; // 静気温 [K]
        public string StaticPressurePath = "~/Physics/StaticPressure";

        [FDMiDataPath(nameof(StaticPressurePath))]
        public FDMiFloat StaticPressure; // 静圧 [Pa]
        public string RhoPath = "~/Physics/Rho";

        [FDMiDataPath(nameof(RhoPath))]
        public FDMiFloat Rho; // 空気密度 [kg/m3]
        public string SonicSpeedPath = "~/Physics/SonicSpeed";

        [FDMiDataPath(nameof(SonicSpeedPath))]
        public FDMiFloat SonicSpeed; // 音速 [m/s]

        [Header("Outputs")]
        public string AirSpeedPath = "~/Physics/AirSpeed";

        [FDMiDataPath(nameof(AirSpeedPath))]
        public FDMiVector3 AirSpeed;
        public string AlphaPath = "~/Physics/Alpha";

        [FDMiDataPath(nameof(AlphaPath))]
        public FDMiFloat Alpha; // 迎角 [deg]
        public string BetaPath = "~/Physics/Beta";

        [FDMiDataPath(nameof(BetaPath))]
        public FDMiFloat Beta; // 横滑り角 [deg]
        public string IASPath = "~/Physics/IAS";

        [FDMiDataPath(nameof(IASPath))]
        public FDMiFloat IAS; // 指示対気速度 [m/s]
        public string TASPath = "~/Physics/TAS";

        [FDMiDataPath(nameof(TASPath))]
        public FDMiFloat TAS; // 真対気速度 [m/s]
        public string MachPath = "~/Physics/Mach";

        [FDMiDataPath(nameof(MachPath))]
        public FDMiFloat Mach;
        public string OATPath = "~/Physics/OAT";

        [FDMiDataPath(nameof(OATPath))]
        public FDMiFloat OAT; // 全温 Tt0 [K] (JetEngine の OAT 入力)
        public string DynamicPressurePath = "~/Physics/DynamicPressure";

        [FDMiDataPath(nameof(DynamicPressurePath))]
        public FDMiFloat DynamicPressure; // 全圧 Pt0 [Pa] (JetEngine の DynamicPressure 入力)
        public string VerticalSpeedPath = "~/Physics/VS";

        [FDMiDataPath(nameof(VerticalSpeedPath))]
        public FDMiFloat VerticalSpeed; // 世界 Y 速度 [m/s]
        public string IASAccPath = "~/Physics/IASAcc";

        [FDMiDataPath(nameof(IASAccPath))]
        public FDMiFloat IASAcc; // 指示対気速度の時間微分 [m/s2]
        public string TASAccPath = "~/Physics/TASAcc";

        [FDMiDataPath(nameof(TASAccPath))]
        public FDMiFloat TASAcc; // 真対気速度の時間微分 [m/s2]

        [Header("Smoothing")]
        public float tauAcc = 0.5f; // 加速度の一次遅れ [s] (微分のノイズ吸収。定常値は変えない)

        Vector3[] _wind;
        float[] _sat,
            _ps,
            _rho,
            _sonic;
        Vector3[] _air;
        float[] _alpha,
            _beta,
            _ias,
            _tas,
            _mach,
            _oat,
            _pt,
            _vs,
            _iasAcc,
            _tasAcc;

        void Start()
        {
            _wind = Wind.data;
            _sat = SAT.data;
            _ps = StaticPressure.data;
            _rho = Rho.data;
            _sonic = SonicSpeed.data;
            _air = AirSpeed.data;
            _alpha = Alpha.data;
            _beta = Beta.data;
            _ias = IAS.data;
            _tas = TAS.data;
            _mach = Mach.data;
            _oat = OAT.data;
            _pt = DynamicPressure.data;
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

            Vector3 vel = body.velocity;
            Vector3 local = Quaternion.Inverse(body.rotation) * (vel - _wind[0]);
            _air[0] = local;

            _alpha[0] = -Mathf.Atan2(local.y, local.z) * Mathf.Rad2Deg;
            _beta[0] = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;

            float pIAS = _ias[0];
            float pTAS = _tas[0];
            float tas = local.magnitude;
            float ias = tas * Mathf.Sqrt(_rho[0] / 1.225f);
            _tas[0] = tas;
            _ias[0] = ias;

            float mach = tas / Mathf.Max(_sonic[0], 0.1f);
            _mach[0] = mach;

            // 等エントロピー関係: Tt0/T0 = 1 + 0.2*M^2、Pt0/Ps0 = (1 + 0.2*M^2)^3.5
            float f = 1f + 0.2f * mach * mach;
            _oat[0] = _sat[0] * f;
            _pt[0] = _ps[0] * f * f * f * Mathf.Sqrt(f);

            _vs[0] = vel.y;

            float invDt = 1f / dt;
            float k = dt / (tauAcc + dt);
            _iasAcc[0] += ((ias - pIAS) * invDt - _iasAcc[0]) * k;
            _tasAcc[0] += ((tas - pTAS) * invDt - _tasAcc[0]) * k;
        }
    }
}
