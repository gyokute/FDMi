
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public enum FDMiFloatDifferenceType { PLUS, MINUS }
    public class FDMiFloatDifference : FDMiBehaviour
    {
        [SerializeField] FDMiFloat output;
        [SerializeField] FDMiFloat[] input;
        [SerializeField] FDMiFloatDifferenceType[] inputType;
        [SerializeField] private bool useUpdate = false, useOnChange = true;
        void Start()
        {
            if (useOnChange) foreach (FDMiFloat d in input) d.subscribe(this, "OnChange");
            if (input.Length != inputType.Length) inputType = new FDMiFloatDifferenceType[input.Length];
            gameObject.SetActive(useUpdate);
        }
        void Update()
        {
            OnChange();
        }
        float o;
        public void OnChange()
        {
            o = 0;
            for (int i = 0; i < input.Length; i++)
            {
                o += (inputType[i] == FDMiFloatDifferenceType.PLUS ? 1f : -1f) * input[i].data[0];
            }
            output.Data = o;
        }
    }
}
