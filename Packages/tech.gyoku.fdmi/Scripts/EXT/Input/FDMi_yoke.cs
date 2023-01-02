
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public enum YokeControlType { pull, rotate, twist }
    public enum YokeAxisType { pitch, roll, yaw, trigger, x, y }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMi_yoke : FDMi_InputObject
    {
        public YokeControlType pitchType, rollType, yawType;
        public LeverAxis pitchAxis, rollAxis, yawAxis;
        [UdonSynced(UdonSyncMode.None)] public float[] input = { 0, 0, 0, 0, 0, 0 };
        // [UdonSynced(UdonSyncMode.None)] public float YInput;
        [SerializeField] private float pitchMul, rollMul, yawMul;
        // public FDMi_SyncObject StickYObject;
        // [SerializeField] private float stickYmul = 1f, stickYthr = 0.7f;


        public override void whenRelease()
        {
            base.whenRelease();
            input[(int)YokeAxisType.pitch] = 0f;
            input[(int)YokeAxisType.roll] = 0f;
            input[(int)YokeAxisType.yaw] = 0f;
            input[(int)YokeAxisType.trigger] = 0f;
            input[(int)YokeAxisType.x] = 0f;
            input[(int)YokeAxisType.y] = 0f;
            // if (StickYObject != null)
            // {
            //     StickYObject.Val = 0f;
            //     StickYObject.RequestSerialization();
            // }
            RequestSerialization();
        }

        public override void InputUpdate()
        {
            base.InputUpdate();
            input[(int)YokeAxisType.pitch] = yokeMove(pitchType, pitchMul, (int)pitchAxis);
            input[(int)YokeAxisType.roll] = yokeMove(rollType, rollMul, (int)rollAxis);
            input[(int)YokeAxisType.yaw] = yokeMove(yawType, yawMul, (int)yawAxis);
            input[(int)YokeAxisType.trigger] = Trig[handType];
            input[(int)YokeAxisType.y] = StickY[handType];
            // Val =Trig[handType];
            // if (StickYObject != null)
            // {
            //     Networking.SetOwner(Networking.LocalPlayer, StickYObject.gameObject);
            //     StickYObject.Val = StickY[handType] * stickYmul;
            //     StickYObject.RequestSerialization();
            // }
            RequestSerialization();
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
