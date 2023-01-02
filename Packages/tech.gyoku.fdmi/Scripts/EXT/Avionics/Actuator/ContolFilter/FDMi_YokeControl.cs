
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_YokeControl : FDMi_ControlFilter
    {
        public FDMi_yoke[] yoke;
        public YokeAxisType yokeType;
        public float yokeMul = 1f;
        // public float minIAS = 0f, maxIAS = 420f;
        public KeyCode DesktopUp, DesktopDown;
        public float KeyboardMul = 1f, keyDelay = 0.02f;
        private float val, keyInput, keyVal = 0f;
        private int i;

        public override float filter(float input)
        {
            if (sharedBool[0])
            {
                if (sharedBool[(int)SharedBool.isPilot])
                {
                    keyInput = Input.GetKey(DesktopUp) ? KeyboardMul : 0;
                    keyInput -= Input.GetKey(DesktopDown) ? KeyboardMul : 0;
                    keyVal = Mathf.MoveTowards(keyVal, keyInput, keyDelay / Time.deltaTime);
                }
            }
            val = keyVal;
            for (i = 0; i < yoke.Length; i++)
                val += yoke[i].input[(int)yokeType];
            val = Mathf.Clamp(val, -1, 1) * yokeMul;
            // val *= yokeMul * Mathf.Clamp01(maxIAS - airData[(int)AirData.IAS]) * Mathf.Clamp01(airData[(int)AirData.IAS] - minIAS);
            return val;
        }
    }
}