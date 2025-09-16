
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum YokeControlType { pull, rotate, twist }
    public class FDMiFlightControlInput : FDMiInputAddon
    {
        public YokeControlType pitchType, rollType, yawType;
        public LeverAxis pitchAxis, rollAxis, yawAxis;
        public FDMiSByte PitchInput, RollInput, YawInput, BrakeInput, TrimInput;
        [SerializeField] private float pitchMul, rollMul, yawMul, brakeMul, trimMul;

        public override void whileGrab()
        {
            base.whileGrab();
            if (grabAxis == null) return;
            PitchInput.Data = yokeMove(pitchType, pitchMul, (int)pitchAxis);
            RollInput.Data = yokeMove(rollType, rollMul, (int)rollAxis);
            YawInput.Data = yokeMove(yawType, yawMul, (int)yawAxis);
            BrakeInput.Data = AxisMove(grabAxis[(int)FingerInputType.Trigger], brakeMul);
            TrimInput.Data = AxisMove(grabAxis[(int)FingerInputType.PadV], trimMul);
        }
        // TODO: fix when Released
        public override void OnRelease(FDMiFingerTracker finger)
        {
            base.OnRelease(finger);
            PitchInput.Data = 0;
            RollInput.Data = 0;
            YawInput.Data = 0;
            BrakeInput.Data = 0;
            TrimInput.Data = 0;
        }

        private sbyte yokeMove(YokeControlType control, float inputMul, int axis)
        {
            float rawInput = 0f;
            if (control == YokeControlType.pull)
            {
                rawInput = handPos[axis] - handStartPos[axis];
            }
            if (control == YokeControlType.rotate)
            {
                Quaternion q = Quaternion.FromToRotation(handStartPos, handPos);
                rawInput = q.eulerAngles[axis];
                rawInput = rawInput - Mathf.Floor(rawInput / 180.1f) * 360;
            }
            if (control == YokeControlType.twist)
            {
                Vector3 eular = (Quaternion.Inverse(handStartAxis) * handAxis).eulerAngles;
                rawInput = eular[axis] - Mathf.Floor(eular[axis] / 180.1f) * 360;
            }
            return (sbyte)(Mathf.Clamp(rawInput * inputMul, -1, 1) * 127);
        }

        private static sbyte AxisMove(float axisValue, float inputMul)
        {
            return (sbyte)(Mathf.Clamp(axisValue * inputMul, -1, 1) * 127);
        }
    }
}