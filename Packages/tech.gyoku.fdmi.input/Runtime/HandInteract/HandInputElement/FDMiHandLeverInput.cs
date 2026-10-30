using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public abstract class FDMiHandLeverInput : FDMiHandInputElement
    {
        [Tooltip("出力データの解決パス。名前空間の区切りは /")]
        public string outputPath = "Value";

        [FDMiDataPath(nameof(outputPath))]
        [Tooltip("操作量を書き込む出力。outputPath から解決する")]
        public FDMiData output;

        [SerializeField]
        [Tooltip("手の移動量の倍率")]
        protected float multiplier = 1f;

        [SerializeField]
        [Tooltip("手の移動量を拾う軸。グループローカル")]
        protected Vector3 inputAxis = Vector3.right;

        [SerializeField]
        [Tooltip("出力の下限")]
        protected float min = 0f;

        [SerializeField]
        [Tooltip("出力の上限")]
        protected float max = 1f;

        [SerializeField, Min(0f)]
        [Tooltip("出力の刻み。0 で連続")]
        protected float outputStep = 0f;

        [SerializeField]
        [Tooltip("上限と下限を循環させる")]
        protected bool doRepeat;

        [SerializeField]
        [Tooltip("グラブ中は出力を書かない。離したときだけ書く")]
        protected bool preventSetWhileHold;

        [SerializeField]
        [Tooltip("離したときに寄せる位置。空なら outputStep に従う")]
        protected float[] detents = new float[0];

        [Header("Shift multiplier")]
        [SerializeField]
        [Tooltip("シフト軸で倍率を変える")]
        protected bool useShiftMultiplier;

        [SerializeField]
        [Tooltip("シフト判定に使う軸")]
        protected FDMiHandAxisType multiplierShiftAxis = FDMiHandAxisType.Trigger;

        [SerializeField]
        [Tooltip("シフトとみなす軸値のしきい値")]
        protected float multiplierShiftThreshold = 0.7f;

        [SerializeField, Min(0f)]
        [Tooltip("シフト中の倍率")]
        protected float shiftedMultiplier = 1f;

        protected float handMovement = 0f;
        private float innerValue;

        public override void OnGrabStart(FDMiHandInputGroup group)
        {
            innerValue = output.GetFloat();
        }

        public override void WhileGrab(FDMiHandInputGroup group)
        {
            if (useShiftMultiplier && group.axes[(int)multiplierShiftAxis] > multiplierShiftThreshold)
                innerValue += handMovement * shiftedMultiplier;
            else
                innerValue += handMovement * multiplier;

            if (doRepeat)
                innerValue = min + Mathf.Repeat(innerValue - min, max - min);
            else
                innerValue = Mathf.Clamp(innerValue, min, max);

            if (!preventSetWhileHold)
                output.Set(RoundToStep(innerValue, outputStep));
        }

        public override void OnGrabEnd()
        {
            float nearestDetent = float.MaxValue;
            float distance = float.MaxValue;
            foreach (float detent in detents)
            {
                float candidate = Mathf.Abs(detent - innerValue);
                if (candidate < distance)
                {
                    nearestDetent = detent;
                    distance = candidate;
                }
            }

            if (nearestDetent < float.MaxValue)
                output.Set(Mathf.Clamp(nearestDetent, min, max));
            else
                output.Set(RoundToStep(innerValue, outputStep));
        }

        private static float RoundToStep(float input, float step)
        {
            if (step <= 0f)
                return input;

            return Mathf.Round(input / step) * step;
        }
    }
}
