using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // 世界座標の風ベクトル [m/s] を出す。定常風 + Perlin 乱流。
    // 旧 aerodynamics からの移植 (SaccFlight 流儀の gust サンプリング)。
    // gust 座標は時間で流し、機体位置でスケールする。S=0 で定常風のみ。
    //
    // Udon 最適化:
    // - Time.time ではなく自前の時刻積算 (Time.time は Udon でやや高コスト、かつ長時間で精度低下)。
    // - gust が微小なら正規化をスキップ (normalized は Sqrt 1回。ゼロ除算ガード兼用)。
    // - 構造体コンストラクタ・Vector3.one 参照なし。すべて成分代入。
    public class FDMiWind : FDMiBehaviour
    {
        [Header("Wiring")]
        public FDMiVector3 Position; // 機体位置 (世界) [m]
        public FDMiVector3 WindConstant; // 定常風 (世界) [m/s]

        [Header("Turbulence")]
        public FDMiFloat WindTurbulenceScale; // 位置スケール [1/m]。大きいほど細かい乱れ
        public FDMiFloat WindGustStrength; // 乱流振幅 [m/s]

        [Header("Outputs")]
        public FDMiVector3 Wind;

        Vector3[] _pos, _const, _wind;
        float[] _scale, _strength;
        float _t;

        void Start()
        {
            _pos = Position.data;
            _const = WindConstant.data;
            _scale = WindTurbulenceScale.data;
            _strength = WindGustStrength.data;
            _wind = Wind.data;
            _t = 0f;
        }

        void FixedUpdate()
        {
            _t += Time.fixedDeltaTime;
            Vector3 c = _const[0];
            Vector3 p = _pos[0];
            float s = _scale[0];

            float gx = _t * c.x + s * p.x;
            float gy = _t * c.y + s * p.y;
            float gz = _t * c.z + s * p.z;

            float nx = Mathf.PerlinNoise(gx, 0f) - 0.5f;
            float ny = Mathf.PerlinNoise(gy, 0.3f) - 0.5f;
            float nz = Mathf.PerlinNoise(gz, 0.6f) - 0.5f;

            float amp = _strength[0];
            float len2 = nx * nx + ny * ny + nz * nz;
            Vector3 w;
            if (len2 > 1e-8f)
            {
                float inv = amp / Mathf.Sqrt(len2);
                w.x = c.x + nx * inv;
                w.y = c.y + ny * inv;
                w.z = c.z + nz * inv;
            }
            else
            {
                w = c;
            }
            _wind[0] = w;
        }
    }
}
