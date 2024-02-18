
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.dynamics
{
    public class FDMiPistonEngine : FDMiBehaviour
    {
        [SerializeField] FDMiFloat Throttle, Mixture, Starter, Magneto;
        [SerializeField] FDMiFloat PowerTrainRPM, OutputTorque;
        [SerializeField] FDMiFloat Rho, IntakePressure;
        [SerializeField] AnimationCurve torqueMap;
        [SerializeField] AnimationCurve mixtureMap;
        [SerializeField] float loss, inertia, starterMaxRPM = 500f, idleThrottle = 0.01f, starterTorque = 50f, starterThreshold = 0.9f, magnetoThreshold = 0.05f;
        [Header("エンジン排気量[m^3]. Lを1000分の1にして代入")]
        [SerializeField] float displacement = 0.0054f;
        [Header("スロート断面積[m^2]")]
        [SerializeField] float throatArea = 0.001f;
        [Header("エンジン空気流量[m^3].")]
        [SerializeField] float Q;
        [Header("マニホールド圧[Pa]")]
        [SerializeField] float manPressure;
        [Header("海上におけるミクスチャ最大のときの空燃比.リッチ(1/10)程度に設定する事！")]
        [SerializeField] float maxMixtureAFR = 0.1f;
        [Header("現在の燃料流量[kg/s]")]
        [SerializeField] float currentFuelFlow;
        [Header("混合比(空気/燃料)")]
        [SerializeField] float mixtureRatio;


        float[] throttle, mixture, rpm, output, starter, mag;
        float[] rho, p0;

        void Start()
        {
            throttle = Throttle.data;
            mixture = Mixture.data;
            rpm = PowerTrainRPM.data;
            output = OutputTorque.data;
            starter = Starter.data;
            mag = Magneto.data;
            rho = Rho.data;
            p0 = IntakePressure.data;
        }
        [SerializeField] float engineTorque, pureEngineTorque;
        float currentThrottle, throatAirSpeed;
        void FixedUpdate()
        {
            currentThrottle = Mathf.Lerp(idleThrottle, 1f, throttle[0]);

            Q = displacement * rpm[0] / 120 * currentThrottle;
            throatAirSpeed = Mathf.Lerp(Q / throatArea, throatAirSpeed, 0.5f);
            manPressure = p0[0] + rho[0] * 0.5f * (1 - 1 / (currentThrottle * currentThrottle)) * throatAirSpeed * throatAirSpeed;
            manPressure *= 0.0002953f; //debug

            currentFuelFlow = mixture[0] * Q * maxMixtureAFR;
            mixtureRatio = currentFuelFlow / Mathf.Max(rho[0] * Q, 0.0000001f);
            currentFuelFlow *= 7936.641f; //debug

            pureEngineTorque = torqueMap.Evaluate(rpm[0]) * mixtureMap.Evaluate(mixtureRatio) * currentThrottle * ((mag[0] - magnetoThreshold) > 0 ? 1 : 0) - loss * rpm[0];
            engineTorque = pureEngineTorque + output[0];
            engineTorque += starterTorque * ((starter[0] - starterThreshold) > 0 ? (1 - rpm[0] / starterMaxRPM) : 0);
            PowerTrainRPM.Data = rpm[0] + engineTorque / inertia;
        }
    }
}