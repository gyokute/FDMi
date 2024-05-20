
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiTransformDriverArray : FDMiDriverArray
    {
        public FDMiFloat[] Input;
        public Transform[] targetTransform;
        [SerializeField] Vector3[] zeroPosition, onePosition;
        [SerializeField] AnimationCurve[] multiplier;
        [SerializeField] private DriverUpdateMode updateMode;
        void Start()
        {
            for (int i = 0; i < Mathf.Min(Input.Length, 128); i++)
            {
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
            targetTransform[i].localPosition = Vector3.LerpUnclamped(zeroPosition[i], onePosition[i], multiplier[i].Evaluate(Input[i].Data));
        }
    }
}