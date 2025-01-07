
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public class FDMiAutoPilot : FDMiAttribute
    {
        protected float LPFTau(float T)
        {
            float omega = 2 * Mathf.PI / T;
            return omega / (omega + Time.deltaTime);
        }
        protected float LPF(float value, float prev, float tau)
        {
            return Mathf.Lerp(prev, value, tau);
        }
        protected float HPFTau(float T)
        {
            float omega = 2 * Mathf.PI / T;
            return 1 / (1 + omega * Time.deltaTime);
        }
        protected float HPF(float pOutput, float input, float pInput, float tau)
        {
            return tau * (pOutput + input - pInput);
        }

        protected float PControl(float err, float kp)
        {
            return err * kp;
        }

        protected float IControl(float err, float prevOut, float ki)
        {
            return prevOut + ki * err * Time.deltaTime;
        }
        protected float IControl(float err, float prevErr, float prevOut, float ki)
        {
            return ki * (prevOut + (err + prevErr) * Time.deltaTime * 0.5f);
        }
        protected float DControl(float err, float prevErr, float kd)
        {
            return kd / Time.deltaTime * (err - prevErr);
        }
        protected float PIControl(float err, float prevErr, float prevOut, float kp, float ki)
        {
            float output = kp * (err - prevErr) + ki * err * Time.deltaTime;
            return prevOut + output;
        }
        protected float PIDControl(float err, float pErr, float ppErr, float prevOut, float kp, float ki, float kd)
        {
            float errDiff = err - pErr;
            float pErrDiff = pErr - ppErr;
            float output = kp * errDiff + ki * err * Time.deltaTime + kd * (errDiff - pErrDiff);
            return prevOut + output;
        }
    }
}