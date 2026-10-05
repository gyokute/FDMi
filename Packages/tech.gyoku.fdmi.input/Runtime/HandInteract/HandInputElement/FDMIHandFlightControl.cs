using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public enum FDMiYokeControlType
    {
        Pull,
        Rotate,
        Twist,
    }

    public enum FDMiFlightAxis
    {
        X = 0,
        Y = 1,
        Z = 2,
    }

    public class FDMiHandFlightControl : FDMiHandInputElement
    {
        // NOTE: 基底の output / outputPath は使わない。3 出力のため下記を使う。
        public FDMiYokeControlType pitchType = FDMiYokeControlType.Pull;
        public FDMiFlightAxis pitchAxis = FDMiFlightAxis.X;
        public float pitchMultiplier = 1f;
        public string pitchPath = "PitchInput";

        [FDMiDataPath(nameof(pitchPath))]
        public FDMiData pitch;

        public FDMiYokeControlType rollType = FDMiYokeControlType.Rotate;
        public FDMiFlightAxis rollAxis = FDMiFlightAxis.X;
        public float rollMultiplier = 1f;
        public string rollPath = "RollInput";

        [FDMiDataPath(nameof(rollPath))]
        public FDMiData roll;

        public FDMiYokeControlType yawType = FDMiYokeControlType.Twist;
        public FDMiFlightAxis yawAxis = FDMiFlightAxis.Y;
        public float yawMultiplier = 1f;
        public string yawPath = "YawInput";

        [FDMiDataPath(nameof(yawPath))]
        public FDMiData yaw;

        private Vector3 startHandPos;
        private Quaternion startHandRot;

        public override void OnGrabStart(FDMiHandInputGroup group)
        {
            startHandPos = group.handPos;
            startHandRot = group.handRot;
        }

        public override void WhileGrab(FDMiHandInputGroup group)
        {
            if (!pitch || !roll || !yaw)
                return;
            pitch.Set(YokeMove(pitchType, pitchMultiplier, (int)pitchAxis, group.handPos, group.handRot));
            roll.Set(YokeMove(rollType, rollMultiplier, (int)rollAxis, group.handPos, group.handRot));
            yaw.Set(YokeMove(yawType, yawMultiplier, (int)yawAxis, group.handPos, group.handRot));
        }

        public override void OnGrabEnd()
        {
            if (pitch)
                pitch.Set(0f);
            if (roll)
                roll.Set(0f);
            if (yaw)
                yaw.Set(0f);
        }

        private float YokeMove(
            FDMiYokeControlType control,
            float multiplier,
            int axis,
            Vector3 handPos,
            Quaternion handRot
        )
        {
            float rawInput = 0f;
            if (control == FDMiYokeControlType.Pull)
            {
                Vector3 pull = handPos - startHandPos;
                rawInput = pull[axis];
            }
            else if (control == FDMiYokeControlType.Rotate)
            {
                Quaternion delta = Quaternion.FromToRotation(startHandPos, handPos);
                rawInput = SignedAngleAboutAxis(delta, AxisToVector(axis));
            }
            else if (control == FDMiYokeControlType.Twist)
            {
                Quaternion delta = Quaternion.Inverse(startHandRot) * handRot;
                rawInput = SignedAngleAboutAxis(delta, AxisToVector(axis));
            }
            return Mathf.Clamp(rawInput * multiplier, -1f, 1f);
        }

        private static Vector3 AxisToVector(int axis)
        {
            if (axis == 1) return Vector3.up;
            if (axis == 2) return Vector3.forward;
            return Vector3.right;
        }

        private static float SignedAngleAboutAxis(Quaternion delta, Vector3 axis)
        {
            float angle;
            Vector3 twistAxis;
            delta.ToAngleAxis(out angle, out twistAxis);
            if (angle > 180f) angle -= 360f;
            if (Vector3.Dot(twistAxis, axis) < 0f) angle = -angle;
            return angle;
        }
    }
}
