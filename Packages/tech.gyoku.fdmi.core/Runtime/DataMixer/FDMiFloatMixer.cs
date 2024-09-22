
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public enum FloatMixType { add, mul }
    public class FDMiFloatMixer : FDMiBehaviour
    {
        public FDMiFloat output;
        public FDMiFloat[] data;
        public FloatMixType mixType = FloatMixType.add;
        [SerializeField] float t = 1f;
        [SerializeField] private AnimationCurve outputCurve;
        [SerializeField] private bool useUpdate = false, useOnChange = true;
        void Start()
        {
            if (useOnChange) foreach (FDMiFloat d in data) d.subscribe(this, "OnChange");
            gameObject.SetActive(useUpdate);
        }
        void Update()
        {
            float outTarget = 0f;
            if (mixType == FloatMixType.add)
            {
                outTarget = 0f;
                foreach (FDMiFloat d in data) outTarget += d.data[0];
            }
            else if (mixType == FloatMixType.mul)
            {
                outTarget = 1f;
                foreach (FDMiFloat d in data) outTarget *= d.data[0];
            }
            outTarget = outputCurve.Evaluate(outTarget);
            output.Data = Mathf.MoveTowards(output.data[0], outTarget, Time.deltaTime * t);
            if (!useUpdate && Mathf.Approximately(output.data[0], outTarget)) gameObject.SetActive(false);
        }
        public void OnChange()
        {
            gameObject.SetActive(true);
        }
    }
}
