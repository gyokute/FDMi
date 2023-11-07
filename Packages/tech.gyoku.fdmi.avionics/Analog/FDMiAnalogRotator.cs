
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;


namespace tech.gyoku.FDMi.avionics
{
    public class FDMiAnalogRotator : UdonSharpBehaviour
    {
        public FDMiFloat Value;
        public Transform rotateTransform;
        public Vector3 rotateAxis;
        public AnimationCurve multiplier;
        private float[] datam;
        private Vector3 initial;
        void Start()
        {
            datam = Value.data;
            initial = rotateTransform.localEulerAngles;
            Value.subscribe(this, "OnChange");
        }
        public void OnChange()
        {
            rotateTransform.localEulerAngles = initial + multiplier.Evaluate(datam[0]) * rotateAxis;
        }
    }

}