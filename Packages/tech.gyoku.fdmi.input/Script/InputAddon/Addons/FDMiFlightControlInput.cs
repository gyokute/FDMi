
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

        public FDMiSyncedFloat Pitch, Roll, Yaw;
        [SerializeField] private float pitchMul, rollMul, yawMul;


        public override void Update()
        {
            base.Update();
            Pitch.set(yokeMove(pitchType, pitchMul, (int)pitchAxis));
            Roll.set(yokeMove(rollType, rollMul, (int)rollAxis));
            Yaw.set(yokeMove(yawType, yawMul, (int)yawAxis));
            if (!Input.GetKey(triggeredKey)) OnReleased();
        }
        public override void OnReleased()
        {
            base.OnReleased();
            Pitch.set(0f);
            Roll.set(0f);
            Yaw.set(0f);
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