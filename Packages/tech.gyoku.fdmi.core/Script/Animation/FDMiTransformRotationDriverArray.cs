
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiTransformRotationDriverArray : FDMiDriverArray
    {
        [SerializeField] FDMiFloat[] Input;
        [SerializeField] Transform[] rotateTransform;
        [SerializeField] Vector3[] rotateAxis;
        [SerializeField] AnimationCurve[] multiplier;
        [SerializeField] private float[] repeatValue;
        [SerializeField] private DriverUpdateMode updateMode;
        private Vector3[] initial;
        void Start()
        {
            initial = new Vector3[Input.Length];
            for (int i = 0; i < Mathf.Min(Input.Length, 128); i++)
            {
                initial[i] = rotateTransform[i].localEulerAngles;
                if (updateMode == DriverUpdateMode.OnValueChange)
                    Input[i].subscribe(this, "OnChange" + i.ToString());
            }
            gameObject.SetActive(updateMode == DriverUpdateMode.OnUpdate);
        }

        void Update()
        {
            if (updateMode != DriverUpdateMode.OnUpdate) return;
            for (int i = 0; i < Mathf.Min(Input.Length, 128); i++)
            {
                OnChange(i);
            }
        }

        float dat;
        public override void OnChange(int i)
        {
            dat = Mathf.Approximately(repeatValue[i], 0) ? Input[i].Data : Mathf.Repeat(Input[i].Data, repeatValue[i]);
            rotateTransform[i].localEulerAngles = initial[i] + multiplier[i].Evaluate(dat) * rotateAxis[i];
        }
    }
}