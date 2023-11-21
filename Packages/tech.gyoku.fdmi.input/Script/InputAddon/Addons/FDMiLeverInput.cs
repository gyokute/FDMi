
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum LeverControlType { pull, rotate, twist }
    public enum LeverAxis { x, y, z }
    public class FDMiLeverInput : FDMiInputAddon
    {
        public FDMiFloat LeverOutput, Multiplier;
        public LeverControlType controlType;
        public LeverAxis leverAxis;
        public float angleDiv = 1f, multiplier, min, max;
        [SerializeField] bool doRepeat, doRound, preventSetWhileHold;
        public float[] detents;
        float[] mul = { 0f };
        float initial, rawInput, ret, rotateAcc;
        Vector3 prevPos;
        Quaternion prevAxis;

        protected override void Start()
        {
            base.Start();
            mul[0] = multiplier;
            if (Multiplier) mul = Multiplier.data;

        }
        public override void OnCalled(KeyCode callKey, VRCPlayerApi.TrackingDataType trackType)
        {
            base.OnCalled(callKey, trackType);
            prevPos = handStartPos;
            prevAxis = handStartAxis;
            initial = LeverOutput.Data;
            rotateAcc = 0f;
        }
        public override void OnReleased()
        {
            base.OnReleased();
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
            if (detentIndex < 0 && preventSetWhileHold) LeverOutput.set(Mathf.Clamp(ret, min, max));
            if (detentIndex >= 0) LeverOutput.set(detents[detentIndex]);
        }
        protected override void Update()
        {
            base.Update();
            if (!isActive || !isInit) return;
            if (controlType == LeverControlType.pull)
            {
                Vector3 p = (handPos - handStartPos);
                rawInput = p[(int)leverAxis];
            }
            if (controlType == LeverControlType.rotate)
            {
                Quaternion q = Quaternion.FromToRotation(prevPos, handPos);
                rotateAcc += Mathf.Repeat(q.eulerAngles[(int)leverAxis] + 180, 360) - 180;
                rawInput = Mathf.Sign(rotateAcc) * Mathf.Floor(Mathf.Abs(rotateAcc) / angleDiv) * angleDiv;
            }
            if (controlType == LeverControlType.twist)
            {
                Vector3 eular = (Quaternion.Inverse(prevAxis) * handAxis).eulerAngles;
                rotateAcc += Mathf.Repeat(eular[(int)leverAxis] + 180, 360) - 180;
                rawInput = Mathf.Sign(rotateAcc) * Mathf.Floor(Mathf.Abs(rotateAcc) / angleDiv) * angleDiv;
            }
            ret = initial + mul[0] * rawInput;
            if (doRepeat) ret = Mathf.Repeat(ret, max);
            if (doRound) ret = Mathf.Round(ret);
            if (!preventSetWhileHold) LeverOutput.set(Mathf.Clamp(ret, min, max));
            if (!Input.GetKey(triggeredKey)) OnReleased();
            prevPos = handPos;
            prevAxis = handAxis;
        }

    }
}