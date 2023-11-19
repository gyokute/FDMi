
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
            return omega / (omega + Time.fixedDeltaTime);
        }
        protected float LPF(float value, float prev, float tau)
        {
            return Mathf.Lerp(prev, value, tau);
        }

        protected float PControl(float err, float kp)
        {
            return err * kp;
        }
        protected float IControl(float err, float prevOut, float ki)
        {
            return prevOut + ki * err * Time.fixedDeltaTime;
        }
        protected float PIControl(float err, float prevErr, float prevOut, float kp, float ki)
        {
            float output = kp / Time.fixedDeltaTime * (err - prevErr) + ki * err;
            return prevOut + output * Time.fixedDeltaTime;
        }
    }
}