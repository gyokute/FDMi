
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

        private sbyte[] pitch, roll, yaw, brake, trim;

        void Start()
        {
            pitch = PitchInput.data;
            roll = RollInput.data;
            yaw = YawInput.data;
            brake = BrakeInput.data;
            trim = TrimInput.data;
        }

        public override void whileGrab()
        {
            base.whileGrab();
            if (grabAxis == null) return;
            pitch[0] = yokeMove(pitchType, pitchMul, (int)pitchAxis);
            roll[0] = yokeMove(rollType, rollMul, (int)rollAxis);
            yaw[0] = yokeMove(yawType, yawMul, (int)yawAxis);
            brake[0] = AxisMove(grabAxis[(int)FingerInputType.Trigger], brakeMul);
            trim[0] = AxisMove(grabAxis[(int)FingerInputType.PadV], trimMul);
        }
        // TODO: fix when Released
        public override void OnRelease(FDMiFingerTracker finger)
        {
            base.OnRelease(finger);
            pitch[0] = 0;
            roll[0] = 0;
            yaw[0] = 0;
            brake[0] = 0;
            trim[0] = 0;
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