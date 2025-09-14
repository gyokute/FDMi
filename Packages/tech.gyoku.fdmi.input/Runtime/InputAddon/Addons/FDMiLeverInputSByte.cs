
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiLeverInputSByte : FDMiInputAddon
    {
        public FDMiSByte LeverOutput;
        public FDMiFloat Multiplier;
        public float multiplier;
        public LeverControlType controlType;
        public LeverAxis leverAxis;
        public float angleDiv = 1f;
        public sbyte min = -127, max = 127;
        [SerializeField] bool doRepeat, preventSetWhileHold;
        public sbyte[] detents;
        float initial, rawInput, ret, rotateAcc;
        float[] mul;

        void Start()
        {
            if (Multiplier) mul = Multiplier.data;
            else mul = new float[] { multiplier };
        }
        public override void whileGrab()
        {
            base.whileGrab();
            if (grabAxis == null) return;

            if (controlType == LeverControlType.pull)
            {
                Vector3 p = (handPos - handStartPos);
                rawInput = p[(int)leverAxis];
                ret = initial + mul[0] * rawInput;
            }
            if (controlType == LeverControlType.rotate)
            {
                Quaternion q = Quaternion.FromToRotation(handPrevPos, handPos);
                rotateAcc += Mathf.Repeat(q.eulerAngles[(int)leverAxis] + 180, 360) - 180;
                rawInput = Mathf.Sign(rotateAcc) * Mathf.Floor(Mathf.Abs(rotateAcc) / angleDiv);
                ret += mul[0] * rawInput;
                if (rawInput != 0) rotateAcc = 0;
            }
            if (controlType == LeverControlType.twist)
            {
                Vector3 eular = (Quaternion.Inverse(handPrevAxis) * handAxis).eulerAngles;
                rotateAcc += Mathf.Repeat(eular[(int)leverAxis] + 180, 360) - 180;
                rawInput = Mathf.Sign(rotateAcc) * Mathf.Floor(Mathf.Abs(rotateAcc) / angleDiv);
                ret += mul[0] * rawInput;
                if (rawInput != 0) rotateAcc = 0;
            }
            if (doRepeat) ret = Mathf.Repeat(ret, max);
            ret = Mathf.Clamp(ret, min, max);
            if (!preventSetWhileHold) LeverOutput.Data = (sbyte)Mathf.Round(ret);
        }

        public override void OnGrab(FDMiFingerTracker finger)
        {
            base.OnGrab(finger);
            initial = LeverOutput.Data;
            ret = LeverOutput.Data;
            rotateAcc = 0;
        }
        public override void OnRelease(FDMiFingerTracker finger)
        {
            base.OnRelease(finger);
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
            if (detentIndex < 0 && preventSetWhileHold) LeverOutput.Data = (sbyte)Mathf.Clamp(ret, min, max);
            if (detentIndex >= 0) LeverOutput.Data = detents[detentIndex];
        }
    }
}