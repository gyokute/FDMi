
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

        public FDMiSyncedFloat PitchInput, RollInput, YawInput;
        [SerializeField] private float pitchMul, rollMul, yawMul;


        public override void whileGrab()
        {
            base.whileGrab();
            if (!isActive) return;
            PitchInput.set(yokeMove(pitchType, pitchMul, (int)pitchAxis));
            RollInput.set(yokeMove(rollType, rollMul, (int)rollAxis));
            YawInput.set(yokeMove(yawType, yawMul, (int)yawAxis));
        }
        // TODO: fix when Released
        public override void OnRelease(FDMiFingerTracker finger)
        {
            base.OnRelease(finger);
            PitchInput.set(0f);
            RollInput.set(0f);
            YawInput.set(0f);
        }

        private float yokeMove(YokeControlType control, float inputMul, int axis)
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
            return Mathf.Clamp(rawInput * inputMul, -1, 1);
        }

    }
}