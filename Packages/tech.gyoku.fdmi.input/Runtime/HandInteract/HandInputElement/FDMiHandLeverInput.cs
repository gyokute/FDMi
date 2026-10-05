using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public abstract class FDMiHandLeverInput : FDMiHandInputElement
    {
        public string outputPath = "Value";

        [FDMiDataPath(nameof(outputPath))]
        public FDMiData output;

        [SerializeField]
        protected float multiplier = 1f;

        [SerializeField]
        protected Vector3 inputAxis = Vector3.right;

        [SerializeField]
        protected float min = 0f;

        [SerializeField]
        protected float max = 1f;

        [SerializeField, Min(0f)]
        protected float outputStep = 0f;

        [SerializeField]
        protected bool doRepeat;

        [SerializeField]
        protected bool preventSetWhileHold;

        [SerializeField]
        protected float[] detents = new float[0];

        [Header("Shift multiplier")]
        [SerializeField]
        protected bool useShiftMultiplier;

        [SerializeField]
        protected FDMiHandAxisType multiplierShiftAxis = FDMiHandAxisType.Trigger;

        [SerializeField]
        protected float multiplierShiftThreshold = 0.7f;

        [SerializeField, Min(0f)]
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
