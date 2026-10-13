using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.dynamics
{
    // 世界座標の風ベクトル [m/s] を出す。定常風 + Perlin 乱流。
    // 旧 aerodynamics からの移植 (SaccFlight 流儀の gust サンプリング)。
    // gust 座標は時間で流し、機体位置でスケールする。S=0 で定常風のみ。
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

        void Start()
        {
            _pos = Position.data;
            _const = WindConstant.data;
            _scale = WindTurbulenceScale.data;
            _strength = WindGustStrength.data;
            _wind = Wind.data;
        }

        void Update()
        {
            Vector3 g = Time.time * _const[0] + _scale[0] * _pos[0];
            Vector3 gust;
            gust.x = Mathf.PerlinNoise(g.x, 0f);
            gust.y = Mathf.PerlinNoise(g.y, 0.3f);
            gust.z = Mathf.PerlinNoise(g.z, 0.6f);
            gust -= 0.5f * Vector3.one;
            if (gust.sqrMagnitude > 1e-8f)
                gust = gust.normalized * _strength[0];
            else
                gust = Vector3.zero;
            _wind[0] = _const[0] + gust;
        }
    }
}
