using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public class FDMiAnalogRotator : FDMiBehaviour
    {
        public string ValuePath = "Value";

        [FDMiDataPath(nameof(ValuePath))]
        [FDMiRegisterCallback(nameof(OnChange))]
        public FDMiFloat Value;
        public Transform rotateTransform;
        public Vector3 rotateAxis;
        public AnimationCurve multiplier;

        Vector3 initial;

        void Start()
        {
            datam = Value.data;
            initial = rotateTransform.localEulerAngles;
        }

        public void OnChange()
        {
            rotateTransform.localEulerAngles = initial + multiplier.Evaluate(datam[0]) * rotateAxis;
        }

        float[] datam;
    }
}
