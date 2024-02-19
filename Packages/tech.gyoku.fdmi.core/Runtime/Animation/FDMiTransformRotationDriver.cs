
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiTransformRotationDriver : FDMiBehaviour
    {
        [SerializeField] FDMiFloat Value;
        [SerializeField] Transform rotateTransform;
        [SerializeField] Vector3 rotateAxis;
        [SerializeField] AnimationCurve multiplier;
        [SerializeField] private bool moveOnValueChange, moveOnUpdate;
        [SerializeField] private float repeatValue = 0f;
        private float[] datam;
        private Vector3 initial;
        void Start()
        {
            datam = Value.data;
            initial = rotateTransform.localEulerAngles;
            if (moveOnValueChange) Value.subscribe(this, "OnChange");
            gameObject.SetActive(moveOnUpdate);
        }
        void Update()
        {
            OnChange();
        }
        float dat;
        public void OnChange()
        {
            dat = Mathf.Approximately(repeatValue, 0) ? datam[0] : Mathf.Repeat(datam[0], repeatValue);
            rotateTransform.localEulerAngles = initial + multiplier.Evaluate(dat) * rotateAxis;
        }
    }

}