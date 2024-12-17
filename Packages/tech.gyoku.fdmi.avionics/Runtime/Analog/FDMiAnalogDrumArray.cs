
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;


namespace tech.gyoku.FDMi.avionics
{
    public class FDMiAnalogDrumArray : FDMiBehaviour
    {
        public FDMiFloat Value;
        public Transform[] rotateTransform;
        public AnalogDrumMode[] digitMode;
        public Vector3 rotateAxis;
        [SerializeField] float minDigit = 1f;
        [SerializeField] float multiplier = 1f;
        [SerializeField] float offset = 0f;
        [SerializeField] float minValue = 0f, maxValue = float.MaxValue;
        private float[] datam;
        private Vector3[] initial;
        void Start()
        {
            datam = Value.data;
            initial = new Vector3[rotateTransform.Length];
            for (int i = 0; i < initial.Length && i < rotateTransform.Length; i++)
                initial[i] = rotateTransform[i].localEulerAngles;
        }

        void Update()
        {
            float digit = minDigit;
            float val = Mathf.Clamp((datam[0] + offset) * multiplier, minValue, maxValue);
            for (int i = 0; i < rotateTransform.Length; i++)
            {
                float drumValue = getDrumDigit(val, digit, digitMode[i]);
                rotateTransform[i].localEulerAngles = initial[i] + drumValue * 36 * rotateAxis;
                digit *= 10;
            }

        }

        float getDrumDigit(float val, float digit, AnalogDrumMode mode)
        {
            float ret = 0f;
            if (mode == AnalogDrumMode.Linear)
            {
                ret = val / digit;
            }
            else if (mode == AnalogDrumMode.StepContinuous)
            {
                ret = Mathf.Floor(val / digit);
                ret += Mathf.Clamp01((val % digit - digit + minDigit) / minDigit);
            }
            else if (mode == AnalogDrumMode.StepImmediate)
            {
                ret = Mathf.Floor(val / digit);
            }
            return ret;
        }
    }
}