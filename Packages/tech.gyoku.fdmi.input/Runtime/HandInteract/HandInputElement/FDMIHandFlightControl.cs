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

    public class FDMiHandFlightControl : FDMiHandInputElement
    {
        // NOTE: 基底の output / outputPath は使わない。3 出力のため下記を使う。

        public string pitchPath = "PitchInput";

        [FDMiDataPath(nameof(pitchPath))]
        public FDMiData pitch;
        public FDMiYokeControlType pitchType = FDMiYokeControlType.Pull;
        public Vector3 pitchAxis = new Vector3(0f, 1f, 0f);
        public float pitchMultiplier = 1f;

        public string rollPath = "RollInput";

        [FDMiDataPath(nameof(rollPath))]
        public FDMiData roll;
        public FDMiYokeControlType rollType = FDMiYokeControlType.Rotate;
        public Vector3 rollAxis = new Vector3(1f, 0f, 0f);
        public float rollMultiplier = 1f;

        public string yawPath = "YawInput";

        [FDMiDataPath(nameof(yawPath))]
        public FDMiData yaw;
        public FDMiYokeControlType yawType = FDMiYokeControlType.Twist;
        public Vector3 yawAxis = new Vector3(0f, 1f, 0f);
        public float yawMultiplier = 1f;

        private Vector3 startHandPos;
        private Quaternion startHandRot;

        public override void OnGrabStart(FDMiHandInputGroup group)
        {
            startHandPos = group.handPos;
            startHandRot = group.handRot;
        }

        public override void WhileGrab(FDMiHandInputGroup group)
        {
            pitch.Set(YokeMove(pitchType, pitchMultiplier, pitchAxis, group.handPos, group.handRot));
            roll.Set(YokeMove(rollType, rollMultiplier, rollAxis, group.handPos, group.handRot));
            yaw.Set(YokeMove(yawType, yawMultiplier, yawAxis, group.handPos, group.handRot));
        }

        public override void OnGrabEnd()
        {
            pitch.Set(0f);
            roll.Set(0f);
            yaw.Set(0f);
        }

        private float YokeMove(
            FDMiYokeControlType control,
            float multiplier,
            Vector3 axis,
            Vector3 handPos,
            Quaternion handRot
        )
        {
            float rawInput = 0f;
            if (control == FDMiYokeControlType.Pull)
            {
                Vector3 pull = handPos - startHandPos;
                rawInput = Vector3.Dot(pull, axis);
            }
            else if (control == FDMiYokeControlType.Rotate)
            {
                Quaternion delta = Quaternion.FromToRotation(startHandPos, handPos);
                rawInput = SignedAngleAboutAxis(delta, axis);
            }
            else if (control == FDMiYokeControlType.Twist)
            {
                Quaternion delta = Quaternion.Inverse(startHandRot) * handRot;
                rawInput = SignedAngleAboutAxis(delta, axis);
            }
            return Mathf.Clamp(rawInput * multiplier, -1f, 1f);
        }

        private static float SignedAngleAboutAxis(Quaternion delta, Vector3 axis)
        {
            // swing-twist 分解の twist 側: 選択軸まわりの成分のみを厳密抽出。
            // 特異点（180°回転かつ軸が選択軸と垂直）では Atan2(0,0)=0 を返す。
            Vector3 n = axis.normalized;
            Vector3 v = new Vector3(delta.x, delta.y, delta.z);
            float angle = 2f * Mathf.Atan2(Vector3.Dot(v, n), delta.w) * Mathf.Rad2Deg;
            if (angle > 180f) angle -= 360f;
            else if (angle < -180f) angle += 360f;
            return angle;
        }
    }
}
