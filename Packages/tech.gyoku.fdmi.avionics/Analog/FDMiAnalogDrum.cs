
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;


namespace tech.gyoku.FDMi.avionics
{
    public class FDMiAnalogDrum : UdonSharpBehaviour
    {
        public FDMiFloat Value;
        public Transform rotateTransform;
        public Vector3 rotateAxis;
        public float multiplier;
        public int rotateDigit;
        public bool rotateStep, rotateSmooth;
        private float[] datam;
        private Vector3 initial;
        public float digitValue, digit, subDigit;
        void Start()
        {
            datam = Value.data;
            initial = rotateTransform.localEulerAngles;
            subDigit = digit * 0.1f;
        }
        void Update()
        {
            subDigit = digit * 0.1f;
            float dat = datam[0] * multiplier;
            digitValue = dat / digit;
            if (!rotateSmooth && !rotateStep)
            {
                digitValue = Mathf.Floor(digitValue);
            }
            if (rotateStep)
            {
                digitValue = Mathf.Floor(digitValue);
                digitValue += Mathf.Clamp01((Mathf.Floor((dat * subDigit) % 10) - 9f) * 0.1f);
            }
            rotateTransform.localEulerAngles = initial + digitValue * rotateAxis;
        }
    }
}