using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public enum AnalogDrumMode { Linear, StepContinuous, StepImmediate }

    public class FDMiAnalogDrum : FDMiBehaviour
    {
        public string ValuePath = "Value";

        [FDMiDataPath(nameof(ValuePath))]
        [FDMiRegisterCallback(nameof(OnChange))]
        public FDMiFloat Value;
        public Transform rotateTransform;
        public AnalogDrumMode mode = AnalogDrumMode.StepImmediate;
        public Vector3 rotateAxis;
        [SerializeField] float multiplier = 1f;
        [SerializeField] float offset = 0f;
        [SerializeField] float minValue = 0f, maxValue = float.MaxValue;
        [SerializeField] private float digit, drumRotateSpeed = 10f;

        Vector3 initial;
        float drumValue;
        float targetValue;

        void Start()
        {
            datam = Value.data;
            initial = rotateTransform.localEulerAngles;
        }

        public void OnChange()
        {
            float d = digit > 0f ? digit : 1f; // ゼロ除算ガード
            float val = Mathf.Clamp((datam[0] + offset) * multiplier, minValue, maxValue) / d;
            if (mode == AnalogDrumMode.StepContinuous)
            {
                targetValue = (float)Mathf.FloorToInt(val); // 目標値のみ更新し、移動はUpdateに委譲
                return;
            }
            drumValue = mode == AnalogDrumMode.Linear ? val : (float)Mathf.FloorToInt(val);
            rotateTransform.localEulerAngles = initial + drumValue * rotateAxis;
        }

        void Update()
        {
            if (mode != AnalogDrumMode.StepContinuous) return;
            drumValue = Mathf.MoveTowards(drumValue, targetValue, drumRotateSpeed * Time.deltaTime);
            rotateTransform.localEulerAngles = initial + drumValue * rotateAxis;
        }

        float[] datam;
    }
}
