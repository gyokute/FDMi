using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // ISA 高度大気。位置 (機体 Y + km オフセット) から SAT・気圧・密度・音速を出す。
    // 旧 aerodynamics からの移植。地表基準の指数減衰をやめ、ISA 定義式にした。
    // 対流圏 (h <= 11000 m): T = T0 - L*h, p = p0*(T/T0)^5.25588
    // 成層圏下部 (h > 11000 m): T = 216.65 K、p11 からの指数減衰 (p11/p0 = 0.22336 定数)。
    // Rho は気圧・気温から状態方程式で解く (p = rho*R*T)。音速は sqrt(gamma*R*T)。
    //
    // Udon 最適化: 成層圏の p11 再計算を定数比に縮退 (Pow級の積×6 + 除算を削減)。
    // 5.25588 乗は tr^5 * sqrt(tr) で近似 (誤差 ~1e-4)。
    public class FDMiAtmosphere : FDMiBehaviour
    {
        [Header("Wiring")]
        [Tooltip("高度の解決パス")]
        public string AltitudePath = "~/Physics/Altitude";

        [FDMiDataPath(nameof(AltitudePath))]
        [Tooltip("機体位置 [m]。シーン原点基準")]
        public FDMiFloat Altitude; // 機体位置 (シーン原点基準) [m]

        [Header("Sea-level reference")]
        [Tooltip("海面気圧の解決パス")]
        public string GroundPressurePath = "~/Physics/GroundPressure";

        [FDMiDataPath(nameof(GroundPressurePath))]
        [Tooltip("海面気圧 [Pa]")]
        public FDMiFloat GroundPressure; // 海面気圧 [Pa]

        [Tooltip("海面気温の解決パス")]
        public string GroundTemperaturePath = "~/Physics/GroundTemperature";

        [FDMiDataPath(nameof(GroundTemperaturePath))]
        [Tooltip("海面気温 [K]")]
        public FDMiFloat GroundTemperature; // 海面気温 [K]

        [Header("Outputs (SI)")]
        [Tooltip("静気温の出力パス")]
        public string SATPath = "~/Physics/SAT";

        [FDMiDataPath(nameof(SATPath))]
        [Tooltip("静気温 [K]")]
        public FDMiFloat SAT; // 静気温 [K]

        [Tooltip("静圧の出力パス")]
        public string StaticPressure = "~/Physics/StaticPressure";

        [FDMiDataPath(nameof(StaticPressure))]
        [Tooltip("静圧 [Pa]")]
        public FDMiFloat Pressure; // 静圧 [Pa]

        [Tooltip("空気密度の出力パス")]
        public string RhoPath = "~/Physics/Rho";

        [FDMiDataPath(nameof(RhoPath))]
        [Tooltip("空気密度 [kg/m3]")]
        public FDMiFloat Rho; // 空気密度 [kg/m3]

        [Tooltip("音速の出力パス")]
        public string SonicSpeedPath = "~/Physics/SonicSpeed";

        [FDMiDataPath(nameof(SonicSpeedPath))]
        [Tooltip("音速 [m/s]")]
        public FDMiFloat SonicSpeed; // 音速 [m/s]

        float[] _alt;
        float[] _gndP;
        float[] _gndT;
        float[] _sat;
        float[] _p;
        float[] _rho;
        float[] _sonic;

        void Start()
        {
            _gndP = GroundPressure.data;
            _gndT = GroundTemperature.data;
            _alt = Altitude.data;
            _sat = SAT.data;
            _p = Pressure.data;
            _rho = Rho.data;
            _sonic = SonicSpeed.data;
        }

        void FixedUpdate()
        {
            float h = _alt[0];
            float t;
            float p;
            if (h <= 11000f)
            {
                t = _gndT[0] - 0.0065f * h;
                if (t < 216.65f)
                    t = 216.65f;
                float tr = t / _gndT[0];
                float tr2 = tr * tr;
                p = _gndP[0] * tr2 * tr2 * tr * Mathf.Sqrt(tr);
            }
            else
            {
                t = 216.65f;
                p = _gndP[0] * 0.22336f * Mathf.Exp(-(h - 11000f) * 0.000157696f);
            }
            _sat[0] = t;
            _p[0] = p;
            _rho[0] = p / (287.05f * t);
            _sonic[0] = Mathf.Sqrt(401.879f * t);
        }
    }
}
