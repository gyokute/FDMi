
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.dynamics
{
    public class FDMiPistonEngine : FDMiBehaviour
    {
        public FDMiFloat Throttle, Mixture, Starter, Magneto;
        public FDMiFloat PowerTrainRPM, OutputTorque;
        public FDMiFloat IntakePressure, IntakeTemperature;
        [SerializeField] AnimationCurve torqueMap;
        [SerializeField] AnimationCurve mixtureMap;
        [SerializeField] AnimationCurve intakeDragCurve;
        [SerializeField] float loss, inertia, starterMaxRPM = 500f, idleThrottle = 0.01f, starterTorque = 50f, starterThreshold = 0.9f, magnetoThreshold = 0.05f;
        [Header("エンジン排気量[m^3]. Lを1000分の1にして代入")]
        [SerializeField] float displacement = 0.0054f;
        [Header("スロート断面積[m^2]")]
        [SerializeField] float throatArea = 0.001f;
        [Header("マニホールド圧[Pa]")]
        public FDMiFloat ManifoldPressure;
        [Header("海上におけるミクスチャ最大のときの空燃比.リッチ(1/10)程度に設定する事！")]
        [SerializeField] float maxMixtureAFR = 0.1f;
        [Header("現在の燃料流量[kg/s]")]
        [SerializeField] float currentFuelFlow;
        [Header("混合比(空気/燃料)")]
        [SerializeField] float mixtureRatio;


        float[] throttle, mixture, rpm, output, starter, mag, map;
        float[] p0, tmp;

        void Start()
        {
            throttle = Throttle.data;
            mixture = Mixture.data;
            rpm = PowerTrainRPM.data;
            output = OutputTorque.data;
            starter = Starter.data;
            mag = Magneto.data;
            p0 = IntakePressure.data;
            map = ManifoldPressure.data;
            tmp = IntakeTemperature.data;
            ManifoldPressure.Data = p0[0];
        }
        [SerializeField] float engineTorque, pureEngineTorque;
        float currentThrottle, throatAirSpeed;
        float R = 287.4f; // 気体定数[m^2/s^2/K]=[J/kg/K]
        float kappa = 1.4f; // cp/cr
        float m_th = 0.000001f; // スロットル吸気流量[m^3/s]
        public float V_m = 0.001f;
        public float volumetricEfficiency = 1f;//排気効率
        float dpdt;
        void FixedUpdate()
        {
            currentThrottle = Mathf.Lerp(idleThrottle, 1f, throttle[0]);
            // 秒間吸気容積[m^3/s] 単気筒 4st 想定
            float V_li = displacement * rpm[0] * volumetricEfficiency / 120;
            dpdt = (m_th * R * tmp[0] - 0.5f * V_li * map[0]) / V_m;
            if (float.IsNaN(dpdt)) dpdt = 0f;
            ManifoldPressure.Data = Mathf.Clamp(map[0] + dpdt * Time.fixedDeltaTime, 1f, p0[0] * 100);
            // スロットル吸気流量[kg/s]
            m_th = intakeDragCurve.Evaluate(rpm[0]) * currentThrottle * throatArea * psi_flow(map[0], p0[0], tmp[0]);
            if (float.IsNaN(m_th)) m_th = 0f;

            currentFuelFlow = mixture[0] * m_th * maxMixtureAFR;
            mixtureRatio = currentFuelFlow / Mathf.Max(m_th, 0.0000001f);
            currentFuelFlow *= 7936.641f; //debug

            pureEngineTorque = torqueMap.Evaluate(rpm[0]) * mixtureMap.Evaluate(mixtureRatio) * currentThrottle * ((mag[0] - magnetoThreshold) > 0 ? 1 : 0) - loss * rpm[0];
            engineTorque = pureEngineTorque + output[0];
            engineTorque += starterTorque * ((starter[0] - starterThreshold) > 0 ? (1 - rpm[0] / starterMaxRPM) : 0);
            PowerTrainRPM.Data = rpm[0] + engineTorque / inertia;
        }

        float psi_flow(float p, float p0, float T)
        {
            float p_div_p0 = p / p0;
            // if (p_div_p0 < 0.528f) return p0 * 1.87890310f / Mathf.Sqrt(R * T);
            return p0 * Mathf.Pow(p_div_p0, 0.714285714f) * Mathf.Sqrt(7.0f * (1 - Mathf.Pow(p_div_p0, 0.285714f))) / Mathf.Sqrt(R * T);
        }
    }
}