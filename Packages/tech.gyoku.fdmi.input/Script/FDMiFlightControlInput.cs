
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum YokeControlType { pull, rotate, twist }
    public class FDMiFlightControlInput : FDMiInput
    {
        public YokeControlType pitchType, rollType, yawType;
        public LeverAxis pitchAxis, rollAxis, yawAxis;

        public FDMiSyncedFloat Pitch, Roll, Yaw, Trim, Brake;
        [SerializeField] private float pitchMul, rollMul, yawMul;


        void LateUpdate()
        {
            if (!isGrab) return;
            Pitch.set(yokeMove(pitchType, pitchMul, (int)pitchAxis));
            Roll.set(yokeMove(rollType, rollMul, (int)rollAxis));
            Yaw.set(yokeMove(yawType, yawMul, (int)yawAxis));
            Brake.set(Trigger);
            Trim.set(stickY);
        }
        public override void OnDropGrab()
        {
            base.OnDropGrab();
            Pitch.set(0f);
            Roll.set(0f);
            Yaw.set(0f);
            Brake.set(0f);
            Trim.set(0f);
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