using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiObjectActivateByValue : FDMiBehaviour
    {
        public string ValuePath = "~/InZone";

        [FDMiDataPath(nameof(ValuePath)), FDMiRegisterCallback(nameof(OnChange))]
        public FDMiData Value;

        public float threshold = 0.5f;
        public bool onWhenDisable;

        public void OnChange()
        {
            gameObject.SetActive((Value.GetFloat() > threshold) ^ onWhenDisable);
        }
    }
}
