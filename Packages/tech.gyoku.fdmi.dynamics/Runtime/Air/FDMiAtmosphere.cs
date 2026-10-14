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
        public FDMiVector3 Position; // 機体位置 (シーン原点基準) [m]
        public FDMiVector3 KmPosition; // シーン原点の標高オフセット [km]

        [Header("Sea-level reference")]
        public FDMiFloat GroundPressure; // 海面気圧 [Pa]
        public FDMiFloat GroundTemperature; // 海面気温 [K]

        [Header("Outputs (SI)")]
        public FDMiFloat Altitude; // 標高 [m]
        public FDMiFloat SAT; // 静気温 [K]
        public FDMiFloat Pressure; // 静圧 [Pa]
        public FDMiFloat Rho; // 空気密度 [kg/m3]
        public FDMiFloat SonicSpeed; // 音速 [m/s]

        Vector3[] _pos, _km;
        float[] _gndP, _gndT;
        float[] _alt, _sat, _p, _rho, _sonic;

        void Start()
        {
            _pos = Position.data;
            _km = KmPosition.data;
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
            float h = _pos[0].y + _km[0].y * 1000f;
            if (h < 0f)
                h = 0f;
            _alt[0] = h;

            float t, p;
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
