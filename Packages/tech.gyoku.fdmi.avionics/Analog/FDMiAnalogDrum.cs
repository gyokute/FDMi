
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;


namespace tech.gyoku.FDMi.avionics
{
    public enum AnalogDrumMode { Linear, Step }
    public class FDMiAnalogDrum : UdonSharpBehaviour
    {
        public FDMiFloat Value;
        public Transform rotateTransform;
        public AnalogDrumMode mode = AnalogDrumMode.Step;
        public Vector3 rotateAxis;
        public float multiplier = 1f;
        public float offset = 0f;
        private float[] datam;
        private Vector3 initial;
        [SerializeField] private float digit, drumRotateSpeed = 10f;
        private float subDigit;
        void Start()
        {
            datam = Value.data;
            initial = rotateTransform.localEulerAngles;
            subDigit = digit * 10;
        }
        float drumValue;
        void Update()
        {
            if (mode == AnalogDrumMode.Linear)
                drumValue = (datam[0] + offset) * multiplier / digit;
            if (mode == AnalogDrumMode.Step)
            {
                float targetValue = (float)(Mathf.FloorToInt((datam[0] + offset) * multiplier / digit) % 10);
                drumValue = Mathf.MoveTowards(drumValue, targetValue, drumRotateSpeed * Time.deltaTime);
            }
            rotateTransform.localEulerAngles = initial + drumValue * rotateAxis;
        }
    }
}