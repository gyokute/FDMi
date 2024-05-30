
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiFingerGestureInput : FDMiInputAddon
    {
        public FDMiFloat GestureOutput;
        public FDMiFloat Multiplier;
        public float multiplier;
        public LeverControlType controlType;
        public LeverAxis leverAxis;
        public float angleDiv = 1f, min, max;
        [SerializeField] bool doRepeat, doRound, preventSetWhileHold;
        public float[] detents;
        float initial, rawInput, ret, rotateAcc;
        float[] mul;

        void Start()
        {
            if (Multiplier) mul = Multiplier.data;
            else mul = new float[] { multiplier };
        }
        public override void whileTouch()
        {
            base.whileTouch();
            if (touchAxis == null) return;

            if (controlType == LeverControlType.pull)
            {
                Vector3 p = (fingerPos - fingerStartPos);
                rawInput = p[(int)leverAxis];
                ret = initial + mul[0] * rawInput;
            }
            if (controlType == LeverControlType.rotate)
            {
                Quaternion q = Quaternion.FromToRotation(fingerPrevPos, fingerPos);
                rotateAcc += Mathf.Repeat(q.eulerAngles[(int)leverAxis] + 180, 360) - 180;
                rawInput = Mathf.Sign(rotateAcc) * Mathf.Floor(Mathf.Abs(rotateAcc) / angleDiv);
                ret += mul[0] * rawInput;
                if (rawInput != 0) rotateAcc = 0;
            }
            if (controlType == LeverControlType.twist)
            {
                Vector3 eular = (Quaternion.Inverse(fingerPrevAxis) * fingerAxis).eulerAngles;
                rotateAcc += Mathf.Repeat(eular[(int)leverAxis] + 180, 360) - 180;
                rawInput = Mathf.Sign(rotateAcc) * Mathf.Floor(Mathf.Abs(rotateAcc) / angleDiv);
                ret += mul[0] * rawInput;
                if (rawInput != 0) rotateAcc = 0;
            }
            if (doRepeat) ret = Mathf.Repeat(ret, max);
            if (doRound) ret = Mathf.Round(ret);
            ret = Mathf.Clamp(ret, min, max);
            if (!preventSetWhileHold) GestureOutput.Data = ret;
        }

        public override void OnFingerEnter(FDMiFingerTracker finger)
        {
            base.OnFingerEnter(finger);
            initial = GestureOutput.Data;
            ret = GestureOutput.Data;
            rotateAcc = 0;
        }
        public override void OnFingerLeave(FDMiFingerTracker finger)
        {
            base.OnFingerLeave(finger);
            int detentIndex = -1;
            rotateAcc = 0f;
            float minDetDiff = float.MaxValue;
            for (int i = 0; i < detents.Length; i++)
            {
                float detDiff = Mathf.Abs(detents[i] - ret);
                if (detDiff < minDetDiff)
                {
                    detentIndex = i;
                    minDetDiff = detDiff;
                }
            }
            if (detentIndex < 0 && preventSetWhileHold) GestureOutput.Data = Mathf.Clamp(ret, min, max);
            if (detentIndex >= 0) GestureOutput.Data = detents[detentIndex];
        }
    }
}