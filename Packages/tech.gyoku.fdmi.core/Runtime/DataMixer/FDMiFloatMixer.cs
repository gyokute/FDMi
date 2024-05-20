
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiFloatMixer : FDMiBehaviour
    {
        public FDMiFloat output;
        public FDMiFloat[] data;
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
            foreach (FDMiFloat d in data) outTarget += d.data[0];
            outTarget = outputCurve.Evaluate(outTarget);
            output.Data = Mathf.MoveTowards(output.data[0], outputCurve.Evaluate(outTarget), Time.deltaTime * t);
            if (!useUpdate && Mathf.Approximately(output.data[0], outTarget)) gameObject.SetActive(false);
        }
        public void OnChange()
        {
            gameObject.SetActive(true);
        }
    }
}
