
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;


namespace tech.gyoku.FDMi.avionics
{
    public enum AnalogDrumMode { Linear, StepContinuous, StepImmediate }
    public class FDMiAnalogDrum : FDMiBehaviour
    {
        public FDMiFloat Value;
        public Transform rotateTransform;
        public AnalogDrumMode mode = AnalogDrumMode.StepImmediate;
        public Vector3 rotateAxis;
        [SerializeField] float multiplier = 1f;
        [SerializeField] float offset = 0f;
        [SerializeField] float minValue = 0f, maxValue = float.MaxValue;
        private float[] datam;
        private Vector3 initial;
        [SerializeField] private float digit, drumRotateSpeed = 10f;
        private float subDigit;
        void Start()
        {
            datam = Value.data;
            initial = rotateTransform.localEulerAngles;
            subDigit = digit * 10;
            Value.subscribe(this, "OnChange");
        }
        float drumValue;
        public void OnChange()
        {
            float val = Mathf.Clamp((datam[0] + offset) * multiplier, minValue, maxValue) / digit;
            if (mode == AnalogDrumMode.Linear)
                drumValue = val;
            if (mode == AnalogDrumMode.StepContinuous)
            {
                float targetValue = (float)(Mathf.FloorToInt(val));
                drumValue = Mathf.MoveTowards(drumValue, targetValue, drumRotateSpeed * Time.deltaTime);
            }
            if (mode == AnalogDrumMode.StepImmediate)
            {
                drumValue = (float)(Mathf.FloorToInt(val));
            }
            rotateTransform.localEulerAngles = initial + drumValue * rotateAxis;
        }
    }
}