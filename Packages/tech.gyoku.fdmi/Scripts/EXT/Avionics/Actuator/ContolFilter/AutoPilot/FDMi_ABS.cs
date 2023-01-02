
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public enum ABS_Mode { RTO, OFF, ONE, TWO, THREE, MAX }

    public class FDMi_ABS : FDMi_ControlFilter
    {
        // public ABS_Mode mode;
        public FDMi_SyncObject ABSMode, throttle;
        public FDMi_LandingGear[] landingGear;
        public float kp, ki, brakeAcc = -2.19456f;
        private float output, tgtAcc, acc, prevAcc, err, pErr;
        private bool throttleLatch, isGround;
        public override void SFEXT_O_PilotEnter() => Networking.SetOwner(player, this.gameObject);
        public override float filter(float input)
        {
            if (!sharedBool[0]) return input;
            if (!sharedBool[(int)SharedBool.isPilot]) return input;
            tgtAcc = 0f;
            isGround = false;
            for (int i = 0; i < landingGear.Length; i++) isGround = (isGround || landingGear[i].isGround);
            if (!isGround) return 0f;
            switch ((ABS_Mode)Mathf.RoundToInt(ABSMode.val))
            {
                case ABS_Mode.OFF:
                    throttleLatch = false;
                    tgtAcc = 0f;
                    // tgtAcc += Mathf.Ceil(0.2f - input) * 100f;
                    break;
                case ABS_Mode.RTO:
                    tgtAcc = 0f;
                    if (throttle.val > 0.5f) throttleLatch = true;
                    if (throttle.val < 0.02f && throttleLatch) tgtAcc = -4.2672f; // 14ft/sec
                    break;
                case ABS_Mode.ONE:
                    throttleLatch = false;
                    tgtAcc = -1.2192f; // 4ft/sec
                    break;
                case ABS_Mode.TWO:
                    throttleLatch = false;
                    tgtAcc = -1.524f; // 5ft/sec
                    break;
                case ABS_Mode.THREE:
                    throttleLatch = false;
                    tgtAcc = -2.19456f; // 7.2ft/sec
                    break;
                case ABS_Mode.MAX:
                    throttleLatch = false;
                    tgtAcc = -4.2672f; // 14ft/sec
                    break;
            }
            err = Mathf.Min(tgtAcc, brakeAcc * input);
            if (err > -0.001f) output = 0f;
            err = (err - Mathf.Min(airData[(int)AirData.GSAcc], 0f));
            output = Mathf.Clamp01(output + ki * err * Time.deltaTime);
            return output;
        }
    }
}