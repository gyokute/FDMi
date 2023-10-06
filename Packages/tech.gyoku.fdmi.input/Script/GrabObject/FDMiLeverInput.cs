
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum LeverControlType { pull, rotate }
    public class FDMiLeverInput : FDMiInput
    {
        public FDMiSyncedFloat LeverOutput;
        public LeverControlType controlType;
        public LeverAxis leverAxis;
        public float initialValue;
        public float multiply, min, max;
        public float[] detents;

        public override void OnStartGrab()
        {
            base.OnStartGrab();
            initialValue = LeverOutput.data[0];
        }
        public override void OnDropGrab()
        {
            base.OnDropGrab();
            int detentIndex = -1;
            float minDetDiff = max;
            for (int i = 0; i < detents.Length; i++)
            {
                float detDiff = Mathf.Abs(detents[i] - LeverOutput.data[0]);
                if (detDiff < minDetDiff)
                {
                    detentIndex = i;
                    minDetDiff = detDiff;
                }
            }
            if (detentIndex >= 0) LeverOutput.set(detents[detentIndex]);
        }
        float rawInput;
        void Update()
        {
            if (!grabNow) return;
            if (controlType == LeverControlType.pull)
            {
                Vector3 p = (handPos - handStartPos);
                rawInput = p[(int)leverAxis];
            }
            if (controlType == LeverControlType.rotate)
            {
                Quaternion q = Quaternion.FromToRotation(handStartPos, handPos);
                rawInput = q.eulerAngles[(int)leverAxis];
                rawInput = rawInput - Mathf.Floor(rawInput / 180.1f) * 360;
            }
            LeverOutput.set(Mathf.Clamp(initialValue + multiply * rawInput, min, max));
        }

    }
}